using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;
using TrivyProjectManager.Infrastructure.Services;
using SecurityExceptionEntity = TrivyProjectManager.Domain.Entities.SecurityException;

namespace TrivyProjectManager.IntegrationTests;

public sealed class SecurityExceptionIntegrationTests
{
    [Fact]
    public async Task AppliesOnlyActiveAndUnexpiredExceptionsWithSqlite()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"), "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var project = new Project { Name = "Project", Path = Path.GetDirectoryName(dbPath)!, Technology = ProjectTechnology.DotNet, PackageManager = PackageManagerType.DotNetCli };
        dbContext.Projects.Add(project);
        dbContext.SecurityExceptions.AddRange(
            Exception(project.Id, "ACTIVE", null, true),
            Exception(project.Id, "FUTURE", DateTimeOffset.UtcNow.AddDays(1), true),
            Exception(project.Id, "EXPIRED", DateTimeOffset.UtcNow.AddDays(-1), true),
            Exception(project.Id, "INACTIVE", null, false));
        await dbContext.SaveChangesAsync();
        var findings = new[] { Finding("ACTIVE"), Finding("FUTURE"), Finding("EXPIRED"), Finding("INACTIVE") };

        await new SecurityExceptionApplicator(dbContext).ApplyAsync(project.Id, findings);

        Assert.Equal(FindingLifecycleStatus.Ignored, findings[0].Status);
        Assert.Equal(FindingLifecycleStatus.Ignored, findings[1].Status);
        Assert.NotEqual(FindingLifecycleStatus.Ignored, findings[2].Status);
        Assert.NotEqual(FindingLifecycleStatus.Ignored, findings[3].Status);
    }

    private static SecurityExceptionEntity Exception(Guid projectId, string vulnerability, DateTimeOffset? expiresAt, bool active) => new()
    {
        ProjectId = projectId,
        VulnerabilityId = vulnerability,
        IsActive = active,
        ExpiresAt = expiresAt
    };

    private static Finding Finding(string vulnerability) => new()
    {
        FindingKey = vulnerability,
        FindingType = FindingType.Vulnerability,
        VulnerabilityId = vulnerability,
        PackageName = "Package",
        InstalledVersion = "1.0.0"
    };
}
