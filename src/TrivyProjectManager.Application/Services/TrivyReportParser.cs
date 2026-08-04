using System.Text.Json;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Domain.ValueObjects;

namespace TrivyProjectManager.Application.Services;

public sealed class TrivyReportParser(ISecretMaskingService secretMaskingService, IFindingDeduplicationService deduplicationService) : ITrivyReportParser
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

    private static void AddVulnerabilities(List<Finding> findings, TrivyResultDto result)
    {
        foreach (var vulnerability in result.Vulnerabilities ?? [])
        {
            var finding = new Finding
            {
                FindingType = FindingType.Vulnerability,
                Target = result.Target,
                VulnerabilityId = vulnerability.VulnerabilityId,
                PackageName = vulnerability.PackageName,
                PackagePath = vulnerability.PackagePath,
                InstalledVersion = vulnerability.InstalledVersion,
                FixedVersion = vulnerability.FixedVersion,
                Severity = ParseSeverity(vulnerability.Severity),
                Title = vulnerability.Title,
                Description = vulnerability.Description,
                PrimaryUrl = vulnerability.PrimaryUrl ?? vulnerability.References?.FirstOrDefault(),
                PublishedDate = vulnerability.PublishedDate,
                LastModifiedDate = vulnerability.LastModifiedDate
            };
            finding.FindingKey = FindingKey.Create(finding.FindingType, finding.VulnerabilityId, finding.PackageName, finding.InstalledVersion, result.Target, finding.Title);
            finding.References.AddRange((vulnerability.References ?? []).Where(IsHttpUrl).Distinct(StringComparer.OrdinalIgnoreCase).Select(url => new FindingReference { Url = url }));
            finding.Occurrences.Add(new FindingOccurrence { Target = result.Target, FilePath = vulnerability.PackagePath, ProjectName = result.Target });
            findings.Add(finding);
        }
    }

    private static void AddMisconfigurations(List<Finding> findings, TrivyResultDto result)
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
                PrimaryUrl = misconfiguration.PrimaryUrl ?? misconfiguration.References?.FirstOrDefault(),
                FilePath = result.Target,
                StartLine = misconfiguration.CauseMetadata?.StartLine,
                MaskedCodeSnippet = snippet
            };
            finding.FindingKey = FindingKey.Create(finding.FindingType, finding.VulnerabilityId, finding.PackageName, finding.InstalledVersion, result.Target, finding.Title);
            finding.References.AddRange((misconfiguration.References ?? []).Where(IsHttpUrl).Distinct(StringComparer.OrdinalIgnoreCase).Select(url => new FindingReference { Url = url }));
            finding.Occurrences.Add(new FindingOccurrence { Target = result.Target, FilePath = result.Target, ProjectName = result.Target });
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
            finding.Occurrences.Add(new FindingOccurrence { Target = result.Target, FilePath = result.Target, ProjectName = result.Target });
            findings.Add(finding);
        }
    }

    private static FindingSeverity ParseSeverity(string? severity)
    {
        return Enum.TryParse<FindingSeverity>(severity, ignoreCase: true, out var parsed)
            ? parsed
            : FindingSeverity.Unknown;
    }

    private static bool IsHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
