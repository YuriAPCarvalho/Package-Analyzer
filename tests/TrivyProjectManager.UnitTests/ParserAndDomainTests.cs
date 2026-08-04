using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Domain.ValueObjects;

namespace TrivyProjectManager.UnitTests;

public sealed class ParserAndDomainTests
{
    [Fact]
    public async Task ParserReadsVulnerabilityAndReferences()
    {
        var findings = await ParseSampleAsync("dotnet-vulnerability.json");

        var finding = Assert.Single(findings);
        Assert.Equal("CVE-2025-27598", finding.VulnerabilityId);
        Assert.Equal("SixLabors.ImageSharp", finding.PackageName);
        Assert.Equal(FindingSeverity.High, finding.Severity);
        Assert.NotEmpty(finding.References);
    }

    [Fact]
    public async Task ParserDeduplicatesSameCveAcrossTargets()
    {
        var findings = await ParseSampleAsync("duplicate-cve-targets.json");

        var finding = Assert.Single(findings);
        Assert.Equal(2, finding.Occurrences.Count);
    }

    [Fact]
    public async Task ParserMasksSecrets()
    {
        var findings = await ParseSampleAsync("secret.json");

        var finding = Assert.Single(findings);
        Assert.Equal(FindingType.Secret, finding.FindingType);
        Assert.DoesNotContain("ABCDEFGHIJKLMNOPQRSTUVXYZ", finding.MaskedCodeSnippet);
        Assert.Contains("***", finding.MaskedCodeSnippet);
    }

    [Fact]
    public async Task ParserToleratesMissingFields()
    {
        var findings = await ParseSampleAsync("missing-fields.json");

        var finding = Assert.Single(findings);
        Assert.Equal("CVE-0000-0001", finding.VulnerabilityId);
        Assert.Equal(FindingSeverity.Unknown, finding.Severity);
    }

    [Fact]
    public void CounterDoesNotDuplicateUniqueVulnerabilities()
    {
        var findings = new[]
        {
            new Finding
            {
                FindingType = FindingType.Vulnerability,
                Severity = FindingSeverity.High,
                Occurrences = [new FindingOccurrence(), new FindingOccurrence()]
            }
        };

        var counters = FindingCounterService.Calculate(findings);

        Assert.Equal(1, counters.UniqueVulnerabilities);
        Assert.Equal(2, counters.TotalOccurrences);
    }

    [Fact]
    public void FindingKeyUsesLogicalVulnerabilityIdentity()
    {
        var key = FindingKey.Create(FindingType.Vulnerability, "cve-1", "Package", "1.0.0");

        Assert.Equal("VULNERABILITY|CVE-1|PACKAGE|1.0.0", key);
    }

    [Fact]
    public void CommandValidationRejectsWholeCommandLine()
    {
        var command = new ProjectCommand { Command = "dotnet restore", Arguments = "" };

        var errors = CommandValidationService.Validate(command);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ScanComparisonClassifiesNewExistingRegressionAndResolved()
    {
        var service = new ScanComparisonService();
        var previous = new[] { Finding("A"), Finding("B") };
        var current = new[] { Finding("A"), Finding("C"), Finding("D") };
        var resolvedHistory = new[] { Finding("D") };

        service.Classify(current, previous, resolvedHistory);

        Assert.Equal(FindingLifecycleStatus.Existing, current[0].Status);
        Assert.Equal(FindingLifecycleStatus.New, current[1].Status);
        Assert.Equal(FindingLifecycleStatus.Regression, current[2].Status);
        Assert.Equal(FindingLifecycleStatus.Resolved, previous[1].Status);
    }

    [Fact]
    public void DisplayTextTranslatesUiEnumsToPortuguese()
    {
        Assert.Equal("Crítica", DisplayTextService.Severity(FindingSeverity.Critical));
        Assert.Equal("Alta", DisplayTextService.Severity(FindingSeverity.High));
        Assert.Equal("Desconhecida", DisplayTextService.Severity(FindingSeverity.Unknown));
        Assert.Equal("Nova", DisplayTextService.LifecycleStatus(FindingLifecycleStatus.New));
        Assert.Equal("Regressão", DisplayTextService.LifecycleStatus(FindingLifecycleStatus.Regression));
        Assert.Equal("Concluído", DisplayTextService.ScanStatus(ScanStatus.Succeeded));
        Assert.Equal("Configuração incorreta", DisplayTextService.FindingType(FindingType.Misconfiguration));
        Assert.Equal("Segredo", DisplayTextService.FindingType(FindingType.Secret));
    }

    private static async Task<IReadOnlyList<Finding>> ParseSampleAsync(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "trivy-reports", fileName));
        var parser = new TrivyReportParser(new SecretMaskingService(), new FindingDeduplicationService());
        return await parser.ParseAsync(path);
    }

    private static Finding Finding(string key) => new() { FindingKey = key };
}
