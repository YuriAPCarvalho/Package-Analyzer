using System.Text.Json;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Domain.ValueObjects;

namespace TrivyProjectManager.Application.Services;

public sealed class TrivyReportParser(
    ISecretMaskingService secretMaskingService,
    IFindingDeduplicationService deduplicationService,
    FixedVersionRecommendationService fixedVersionRecommendationService,
    ReferenceDisplayService referenceDisplayService) : ITrivyReportParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<IReadOnlyList<Finding>> ParseAsync(string reportPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("Trivy report was not found.", reportPath);
        }

        await using var stream = File.OpenRead(reportPath);
        var report = await JsonSerializer.DeserializeAsync<TrivyReportDto>(stream, JsonOptions, cancellationToken)
            ?? new TrivyReportDto();

        var findings = new List<Finding>();
        foreach (var result in report.Results ?? [])
        {
            AddVulnerabilities(findings, result);
            AddMisconfigurations(findings, result);
            AddSecrets(findings, result);
        }

        return deduplicationService.Deduplicate(findings);
    }

    private void AddVulnerabilities(List<Finding> findings, TrivyResultDto result)
    {
        foreach (var vulnerability in result.Vulnerabilities ?? [])
        {
            var recommendation = fixedVersionRecommendationService.Recommend(vulnerability.InstalledVersion, vulnerability.FixedVersion);
            var references = referenceDisplayService.Build(vulnerability.References ?? [], vulnerability.PrimaryUrl);
            var cvss = SelectCvss(vulnerability);
            var finding = new Finding
            {
                FindingType = FindingType.Vulnerability,
                Target = result.Target,
                VulnerabilityId = vulnerability.VulnerabilityId,
                PackageName = vulnerability.PackageName,
                PackagePath = vulnerability.PackagePath,
                Ecosystem = result.Type,
                InstalledVersion = vulnerability.InstalledVersion,
                FixedVersion = vulnerability.FixedVersion,
                RecommendedFixedVersion = recommendation.RecommendedVersion,
                OtherFixedVersions = recommendation.OtherVersions.Count == 0 ? null : string.Join(", ", recommendation.OtherVersions),
                Severity = ParseSeverity(vulnerability.Severity),
                SeveritySource = vulnerability.SeveritySource,
                FixAvailability = ClassifyFixAvailability(vulnerability.FixedVersion, vulnerability.Status),
                Title = vulnerability.Title,
                Description = vulnerability.Description,
                PrimaryUrl = referenceDisplayService.SelectPrimaryUrl(references),
                CvssScore = cvss.Score,
                CvssVector = cvss.Vector,
                CvssSource = cvss.Source,
                CweIds = vulnerability.CweIds is null || vulnerability.CweIds.Count == 0 ? null : string.Join(", ", vulnerability.CweIds.Distinct(StringComparer.OrdinalIgnoreCase)),
                PublishedDate = vulnerability.PublishedDate,
                LastModifiedDate = vulnerability.LastModifiedDate
            };
            finding.FindingKey = FindingKey.Create(finding.FindingType, finding.VulnerabilityId, finding.PackageName, finding.InstalledVersion, result.Target, finding.Title);
            finding.References.AddRange(references);
            finding.Occurrences.Add(new FindingOccurrence
            {
                Target = result.Target,
                FilePath = result.Target,
                RelativePath = result.Target,
                ProjectName = result.Target
            });
            findings.Add(finding);
        }
    }

    private void AddMisconfigurations(List<Finding> findings, TrivyResultDto result)
    {
        foreach (var misconfiguration in result.Misconfigurations ?? [])
        {
            var id = misconfiguration.AvdId ?? misconfiguration.Id;
            var snippet = misconfiguration.CauseMetadata?.Code?.Lines?.FirstOrDefault(line => line.IsCause == true)?.Content
                ?? misconfiguration.CauseMetadata?.Code?.Lines?.FirstOrDefault()?.Content;
            var finding = new Finding
            {
                FindingType = FindingType.Misconfiguration,
                Target = result.Target,
                VulnerabilityId = id,
                PackageName = misconfiguration.Type,
                InstalledVersion = misconfiguration.CauseMetadata?.Resource,
                Severity = ParseSeverity(misconfiguration.Severity),
                Title = misconfiguration.Title ?? misconfiguration.Message,
                Description = misconfiguration.Description,
                FilePath = result.Target,
                StartLine = misconfiguration.CauseMetadata?.StartLine,
                MaskedCodeSnippet = snippet
            };
            finding.FindingKey = FindingKey.Create(finding.FindingType, finding.VulnerabilityId, finding.PackageName, finding.InstalledVersion, result.Target, finding.Title);
            var references = referenceDisplayService.Build(misconfiguration.References ?? [], misconfiguration.PrimaryUrl);
            finding.PrimaryUrl = referenceDisplayService.SelectPrimaryUrl(references);
            finding.References.AddRange(references);
            finding.Occurrences.Add(new FindingOccurrence { Target = result.Target, FilePath = result.Target, RelativePath = result.Target, ProjectName = result.Target });
            findings.Add(finding);
        }
    }

    private void AddSecrets(List<Finding> findings, TrivyResultDto result)
    {
        foreach (var secret in result.Secrets ?? [])
        {
            var snippet = secret.Code?.Lines?.FirstOrDefault(line => line.IsCause == true)?.Content ?? secret.Match ?? secret.Title;
            var finding = new Finding
            {
                FindingType = FindingType.Secret,
                Target = result.Target,
                VulnerabilityId = secret.RuleId,
                PackageName = secret.Category,
                Severity = ParseSeverity(secret.Severity),
                Title = secret.Title ?? secret.RuleId,
                FilePath = result.Target,
                StartLine = secret.StartLine,
                MaskedCodeSnippet = secretMaskingService.Mask(snippet)
            };
            finding.FindingKey = FindingKey.Create(finding.FindingType, finding.VulnerabilityId, finding.PackageName, null, result.Target, finding.Title);
            finding.Occurrences.Add(new FindingOccurrence { Target = result.Target, FilePath = result.Target, RelativePath = result.Target, ProjectName = result.Target });
            findings.Add(finding);
        }
    }

    private static FixAvailability ClassifyFixAvailability(string? fixedVersion, string? status)
    {
        if (!string.IsNullOrWhiteSpace(fixedVersion))
        {
            return FixAvailability.Available;
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return FixAvailability.NotInformed;
        }

        return status.Contains("will_not_fix", StringComparison.OrdinalIgnoreCase)
            || status.Contains("not fixed", StringComparison.OrdinalIgnoreCase)
            || status.Contains("unfixed", StringComparison.OrdinalIgnoreCase)
            ? FixAvailability.Unavailable
            : FixAvailability.NotInformed;
    }

    private static (decimal? Score, string? Vector, string? Source) SelectCvss(TrivyVulnerabilityDto vulnerability)
    {
        if (vulnerability.Cvss is null || vulnerability.Cvss.Count == 0)
        {
            return (null, null, null);
        }

        var preferred = vulnerability.Cvss
            .OrderByDescending(pair => pair.Value.V40Score.HasValue)
            .ThenByDescending(pair => pair.Value.V3Score.HasValue)
            .ThenByDescending(pair => pair.Value.V2Score.HasValue)
            .ThenByDescending(pair => pair.Value.V40Score ?? pair.Value.V3Score ?? pair.Value.V2Score ?? 0)
            .First();

        var value = preferred.Value;
        return value.V40Score.HasValue
            ? (value.V40Score, value.V40Vector, preferred.Key)
            : value.V3Score.HasValue
                ? (value.V3Score, value.V3Vector, preferred.Key)
                : (value.V2Score, value.V2Vector, preferred.Key);
    }

    private static FindingSeverity ParseSeverity(string? severity)
    {
        return Enum.TryParse<FindingSeverity>(severity, ignoreCase: true, out var parsed)
            ? parsed
            : FindingSeverity.Unknown;
    }

}
