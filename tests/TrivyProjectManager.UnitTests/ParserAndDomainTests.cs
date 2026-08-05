using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Domain.ValueObjects;
using TrivyProjectManager.Infrastructure.Services;

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
    public async Task ParserReadsTechnicalDetailsAndFriendlyReferences()
    {
        var findings = await ParseSampleAsync("vulnerability-details.json");

        var finding = Assert.Single(findings);
        Assert.Equal("5.0.3", finding.RecommendedFixedVersion);
        Assert.Equal("4.7.2", finding.OtherFixedVersions);
        Assert.Equal(FixAvailability.Available, finding.FixAvailability);
        Assert.Equal("nuget", finding.Ecosystem);
        Assert.Equal("nvd", finding.SeveritySource);
        Assert.Equal(9.8m, finding.CvssScore);
        Assert.Equal("CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H", finding.CvssVector);
        Assert.Equal("CWE-120", finding.CweIds);
        Assert.DoesNotContain(finding.References, reference => reference.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(finding.References, reference => reference.DisplayName == "GitHub Advisory");
    }

    [Fact]
    public void FixedVersionRecommendationDoesNotSuggestDowngrade()
    {
        var service = new FixedVersionRecommendationService();

        var recommendation = service.Recommend("5.0.0", "4.7.2, 5.0.3");

        Assert.Equal("5.0.3", recommendation.RecommendedVersion);
        Assert.Contains("4.7.2", recommendation.OtherVersions);
    }

    [Fact]
    public void FixedVersionRecommendationLeavesDowngradeOnlyVersionsAsAlternatives()
    {
        var service = new FixedVersionRecommendationService();

        var recommendation = service.Recommend("5.0.0", "4.7.2");

        Assert.Null(recommendation.RecommendedVersion);
        Assert.Equal(["4.7.2"], recommendation.OtherVersions);
    }

    [Fact]
    public void ReferenceDisplayNamesAndPrioritizesPrimaryAdvisory()
    {
        var service = new ReferenceDisplayService();

        var references = service.Build([
            "https://www.cve.org/CVERecord?id=CVE-1",
            "https://github.com/advisories/GHSA-xxxx-yyyy-zzzz",
            "https://nvd.nist.gov/vuln/detail/CVE-1"
        ]);

        Assert.Equal("GitHub Advisory", references.First().DisplayName);
        Assert.True(references.First().IsPrimary);
    }

    [Fact]
    public void FindingTextDoesNotDuplicateEquivalentDescription()
    {
        var service = new FindingTextService();

        var description = service.Description("dotnet: Remote Code Execution Vulnerability", "dotnet Remote Code Execution Vulnerability");

        Assert.Equal("Descrição detalhada não fornecida pelo Trivy.", description);
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
        Assert.Equal("Atualização disponível", DisplayTextService.ApplicationUpdateStatus(ApplicationUpdateStatus.UpdateAvailable));
    }

    [Fact]
    public async Task DependencyAnalysisMarksDirectDotNetDependencyAndBuildsCommand()
    {
        using var temp = TempDirectory.Create();
        var csproj = Path.Combine(temp.Path, "App.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="System.Drawing.Common" Version="5.0.0" />
              </ItemGroup>
            </Project>
            """);
        var project = new Project
        {
            Name = "App",
            Path = temp.Path,
            PackageManager = PackageManagerType.DotNetCli,
            Technology = ProjectTechnology.DotNet
        };
        var finding = new Finding
        {
            FindingType = FindingType.Vulnerability,
            PackageName = "System.Drawing.Common",
            RecommendedFixedVersion = "5.0.3",
            Occurrences = [new FindingOccurrence { Target = "bin/Debug/net9.0/App.deps.json", FilePath = "bin/Debug/net9.0/App.deps.json" }]
        };

        await new DependencyAnalysisService().AnalyzeAsync(project, [finding]);
        var command = new UpdateCommandService().Build(project, finding);

        Assert.Equal(DependencyRelation.Direct, finding.DependencyRelation);
        Assert.Contains("dotnet add", command);
        Assert.Contains("System.Drawing.Common", command);
        Assert.Contains("5.0.3", command);
    }

    [Fact]
    public async Task ExternalLinkServiceRejectsNonHttpsUrls()
    {
        var service = new ExternalLinkService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync("http://example.test"));
    }

    private static async Task<IReadOnlyList<Finding>> ParseSampleAsync(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "trivy-reports", fileName));
        var parser = new TrivyReportParser(
            new SecretMaskingService(),
            new FindingDeduplicationService(),
            new FixedVersionRecommendationService(),
            new ReferenceDisplayService());
        return await parser.ParseAsync(path);
    }

    private static Finding Finding(string key) => new() { FindingKey = key };
}
