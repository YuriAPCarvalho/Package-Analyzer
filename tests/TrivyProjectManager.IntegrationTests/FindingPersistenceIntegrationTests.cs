using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.IntegrationTests;

public sealed class FindingPersistenceIntegrationTests
{
    [Fact]
    public async Task SavesFindingReferencesAndOccurrencesAsNewRows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"), "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var project = new Project
        {
            Name = "Project",
            Path = Path.GetDirectoryName(dbPath)!,
            Technology = ProjectTechnology.DotNet,
            PackageManager = PackageManagerType.DotNetCli
        };
        var scan = new Scan
        {
            ProjectId = project.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ScanStatus.Running
        };
        var finding = new Finding
        {
            ScanId = scan.Id,
            FindingKey = "VULNERABILITY|CVE-1|PACKAGE|1.0.0",
            FindingType = FindingType.Vulnerability,
            VulnerabilityId = "CVE-1",
            PackageName = "Package",
            InstalledVersion = "1.0.0",
            Severity = FindingSeverity.High,
            References = [new FindingReference { Url = "https://example.test/CVE-1" }],
            Occurrences = [new FindingOccurrence { Target = "target.deps.json", FilePath = "target.deps.json" }]
        };

        dbContext.Projects.Add(project);
        dbContext.Scans.Add(scan);
        dbContext.Findings.Add(finding);
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.Findings.CountAsync());
        Assert.Equal(1, await dbContext.FindingReferences.CountAsync());
        Assert.Equal(1, await dbContext.FindingOccurrences.CountAsync());
    }
}
