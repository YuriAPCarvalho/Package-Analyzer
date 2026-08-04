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
                .GroupBy(o => $"{o.Target}|{o.FilePath}|{o.ProjectName}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        return [.. unique.Values.OrderByDescending(f => f.Severity).ThenBy(f => f.PackageName).ThenBy(f => f.VulnerabilityId)];
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
            ProjectName = target
        });
    }
}
