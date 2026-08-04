using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.IntegrationTests;

public sealed class RetentionIntegrationTests
{
    [Fact]
    public async Task MigrationCreatesDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"), "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.MigrateAsync();

        Assert.True(File.Exists(dbPath));
        Assert.True(await dbContext.Projects.CountAsync() == 0);
    }

    [Fact]
    public async Task RetentionDeletesOldScansAndFiles()
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
        var oldReport = Path.Combine(Path.GetDirectoryName(dbPath)!, "old.json");
        await File.WriteAllTextAsync(oldReport, "{}");
        dbContext.Scans.Add(new Scan { ProjectId = project.Id, StartedAt = DateTimeOffset.UtcNow.AddDays(-2), Status = ScanStatus.Succeeded, RawReportPath = oldReport });
        dbContext.Scans.Add(new Scan { ProjectId = project.Id, StartedAt = DateTimeOffset.UtcNow, Status = ScanStatus.Succeeded });
        await dbContext.SaveChangesAsync();

        await new RetentionService(dbContext).ApplyAsync(project.Id, maxHistory: 1);

        Assert.Single(await dbContext.Scans.ToListAsync());
        Assert.False(File.Exists(oldReport));
    }
}
