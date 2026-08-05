using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Services;

public sealed class FindingDeduplicationService : IFindingDeduplicationService
{
    public IReadOnlyList<Finding> Deduplicate(IEnumerable<Finding> findings)
    {
        var unique = new Dictionary<string, Finding>(StringComparer.OrdinalIgnoreCase);

        foreach (var finding in findings)
        {
            if (!unique.TryGetValue(finding.FindingKey, out var existing))
            {
                unique[finding.FindingKey] = finding;
                EnsureOccurrence(finding, finding.Target, finding.FilePath);
                continue;
            }

            existing.Occurrences.AddRange(finding.Occurrences);
            MergeReferences(existing, finding);
            MergeMetadata(existing, finding);
            if (finding.Occurrences.Count == 0)
            {
                EnsureOccurrence(existing, finding.Target, finding.FilePath);
            }

            if (finding.Severity > existing.Severity)
            {
                existing.Severity = finding.Severity;
            }
        }

        foreach (var finding in unique.Values)
        {
            finding.Occurrences = finding.Occurrences
                .GroupBy(o => $"{o.Target}|{o.FilePath}|{o.ProjectName}|{o.RelativePath}|{o.AbsolutePath}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            finding.References = finding.References
                .GroupBy(reference => reference.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var selected = group.OrderByDescending(reference => reference.IsPrimary).First();
                    selected.IsPrimary = group.Any(reference => reference.IsPrimary);
                    return selected;
                })
                .OrderByDescending(reference => reference.IsPrimary)
                .ThenBy(reference => reference.DisplayName)
                .ToList();
        }

        return [.. unique.Values.OrderByDescending(f => f.Severity).ThenBy(f => f.PackageName).ThenBy(f => f.VulnerabilityId)];
    }

    private static void MergeReferences(Finding existing, Finding incoming)
    {
        foreach (var reference in incoming.References)
        {
            if (existing.References.All(current => !current.Url.Equals(reference.Url, StringComparison.OrdinalIgnoreCase)))
            {
                existing.References.Add(reference);
            }
        }

        if (string.IsNullOrWhiteSpace(existing.PrimaryUrl))
        {
            existing.PrimaryUrl = incoming.PrimaryUrl;
        }
    }

    private static void MergeMetadata(Finding existing, Finding incoming)
    {
        existing.Ecosystem ??= incoming.Ecosystem;
        existing.RecommendedFixedVersion ??= incoming.RecommendedFixedVersion;
        existing.OtherFixedVersions ??= incoming.OtherFixedVersions;
        existing.SeveritySource ??= incoming.SeveritySource;
        existing.CvssScore ??= incoming.CvssScore;
        existing.CvssVector ??= incoming.CvssVector;
        existing.CvssSource ??= incoming.CvssSource;
        existing.CweIds ??= incoming.CweIds;
        existing.EnrichmentSource ??= incoming.EnrichmentSource;
        existing.EnrichedAt ??= incoming.EnrichedAt;
        existing.RuntimeSupportAlert ??= incoming.RuntimeSupportAlert;
        if (existing.FixAvailability == Domain.Enums.FixAvailability.Unknown)
        {
            existing.FixAvailability = incoming.FixAvailability;
        }

        if (existing.DependencyRelation == Domain.Enums.DependencyRelation.Unknown)
        {
            existing.DependencyRelation = incoming.DependencyRelation;
        }
    }

    private static void EnsureOccurrence(Finding finding, string? target, string? filePath)
    {
        if (finding.Occurrences.Count > 0)
        {
            return;
        }

        finding.Occurrences.Add(new FindingOccurrence
        {
            Target = target,
            FilePath = filePath,
            RelativePath = filePath,
            ProjectName = target
        });
    }
}
