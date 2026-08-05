using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.IntegrationTests;

public sealed class AutomaticPreparationMigrationTests
{
    [Fact]
    public async Task EnablesAutomaticModeOnlyForLegacyGeneratedCommands()
    {
        var root = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={Path.Combine(root, "data.db")}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var dbContext = new TrivyProjectManagerDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync("20260805090000_VulnerabilityDetailsEnhancements");
        var generatedId = Guid.NewGuid();
        var customId = Guid.NewGuid();
        await dbContext.Database.ExecuteSqlRawAsync(ProjectSql(generatedId, "Generated", Path.Combine(root, "generated")));
        await dbContext.Database.ExecuteSqlRawAsync(ProjectSql(customId, "Custom", Path.Combine(root, "custom")));
        await dbContext.Database.ExecuteSqlRawAsync(CommandSql(Guid.NewGuid(), generatedId, "Restore", "dotnet", "restore"));
        await dbContext.Database.ExecuteSqlRawAsync(CommandSql(Guid.NewGuid(), customId, "Prepare", "custom-tool", "prepare"));

        await migrator.MigrateAsync();
        dbContext.ChangeTracker.Clear();
        var projects = await dbContext.Projects.ToListAsync();
        var generated = Assert.Single(projects, project => project.Name == "Generated");
        var custom = Assert.Single(projects, project => project.Name == "Custom");

        Assert.True(generated.AutoDetectPreparation);
        Assert.False(custom.AutoDetectPreparation);
        Assert.False(generated.IsPreparationTrusted);
        Assert.False(custom.IsPreparationTrusted);
    }

    private static string ProjectSql(Guid id, string name, string path) => $"""
        INSERT INTO Projects (Id, Name, Path, Technology, PackageManager, StorageMode, CreatedAt, UpdatedAt, LastScanAt, IsActive)
        VALUES ('{id}', '{name}', '{path.Replace("'", "''")}', 1, 1, 0, '2026-08-05T00:00:00+00:00', '2026-08-05T00:00:00+00:00', NULL, 1);
        """;

    private static string CommandSql(Guid id, Guid projectId, string name, string command, string arguments) => $"""
        INSERT INTO ProjectCommands (Id, ProjectId, Name, Command, Arguments, ExecutionOrder, IsEnabled, ContinueOnError, WorkingDirectory)
        VALUES ('{id}', '{projectId}', '{name}', '{command}', '{arguments}', 1, 1, 0, NULL);
        """;
}
