using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.IntegrationTests;

public sealed class ScanOrchestratorIntegrationTests
{
    [Fact]
    public async Task PreparationFailureStillRunsTrivyAndPersistsPartialResult()
    {
        var root = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var projectFile = Path.Combine(root, "App.csproj");
        await File.WriteAllTextAsync(projectFile, "<Project />");
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={Path.Combine(root, "data.db")}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var project = new Project { Name = "App", Path = root, Technology = ProjectTechnology.DotNet, PackageManager = PackageManagerType.DotNetCli, AutoDetectPreparation = true, IsPreparationTrusted = true };
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        var detection = new ProjectDetectionResult(root, [ProjectTechnology.DotNet], [PackageManagerType.DotNetCli], ProjectTechnology.DotNet, PackageManagerType.DotNetCli,
            [new DetectedProjectTarget("dotnet:App.csproj", ProjectTechnology.DotNet, PackageManagerType.DotNetCli, root, projectFile, ["dotnet"], [root])], []);
        var trivy = new FakeTrivyService();
        var storage = new TestStoragePathService(root);
        var orchestrator = new ScanOrchestrator(
            dbContext,
            new FailingProcessRunner(),
            trivy,
            new EmptyReportParser(),
            new TrivyReportRedactionService(new SecretMaskingService()),
            new ScanComparisonService(),
            new NoOpDependencyAnalysisService(),
            new NoOpEnrichmentService(),
            new TestSettingsService(),
            storage,
            new NoOpRetentionService(),
            new FixedDetectionService(detection),
            new CommandProfileService(),
            new SecurityExceptionApplicator(dbContext),
            NullLogger<ScanOrchestrator>.Instance);

        var result = await orchestrator.RunAsync(project.Id, ScanMode.Full);

        Assert.True(trivy.WasCalled);
        Assert.Equal(ScanStatus.SucceededWithWarnings, result.Scan.Status);
        Assert.Contains("falhou", result.Scan.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        var commands = await dbContext.ProjectCommands.OrderBy(command => command.ExecutionOrder).ToListAsync();
        Assert.Equal("restore \"App.csproj\"", commands[0].Arguments);
        Assert.Equal(root, commands[0].WorkingDirectory);
    }

    [Fact]
    public async Task FailedNpmInstallSkipsDependentBuildAndStillRunsTrivy()
    {
        var root = Path.Combine(Path.GetTempPath(), "tpm-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var packageJson = Path.Combine(root, "package.json");
        await File.WriteAllTextAsync(packageJson, "{\"scripts\":{\"build\":\"vite build\"}}");
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite($"Data Source={Path.Combine(root, "data.db")}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var project = new Project { Name = "NodeApp", Path = root, Technology = ProjectTechnology.Node, PackageManager = PackageManagerType.Npm, AutoDetectPreparation = true, IsPreparationTrusted = true };
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        var detection = new ProjectDetectionResult(root, [ProjectTechnology.Node], [PackageManagerType.Npm], ProjectTechnology.Node, PackageManagerType.Npm,
            [new DetectedProjectTarget("node:package.json", ProjectTechnology.Node, PackageManagerType.Npm, root, packageJson, ["npm"], [root])], []);
        var trivy = new FakeTrivyService();
        var processRunner = new FailingProcessRunner();
        var orchestrator = new ScanOrchestrator(
            dbContext,
            processRunner,
            trivy,
            new EmptyReportParser(),
            new TrivyReportRedactionService(new SecretMaskingService()),
            new ScanComparisonService(),
            new NoOpDependencyAnalysisService(),
            new NoOpEnrichmentService(),
            new TestSettingsService(),
            new TestStoragePathService(root),
            new NoOpRetentionService(),
            new FixedDetectionService(detection),
            new CommandProfileService(),
            new SecurityExceptionApplicator(dbContext),
            NullLogger<ScanOrchestrator>.Instance);

        var result = await orchestrator.RunAsync(project.Id, ScanMode.Full);

        Assert.True(trivy.WasCalled);
        Assert.Equal(ScanStatus.SucceededWithWarnings, result.Scan.Status);
        Assert.Equal(1, processRunner.CallCount);
        var commands = await dbContext.ProjectCommands.OrderBy(command => command.ExecutionOrder).ToListAsync();
        Assert.Collection(
            commands,
            install => Assert.Equal("install", install.Arguments),
            build => Assert.Equal("run build", build.Arguments));
        Assert.Contains(result.Logs, line => line.Message.Contains("ignorado", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FailingProcessRunner : IProcessRunner
    {
        public int CallCount { get; private set; }

        public Task<ProcessResult> RunAsync(ProcessRequest request, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new ProcessResult(request.FileName, request.Arguments, now, now, 1, CommandExecutionStatus.Failed, string.Empty, "failed"));
        }
    }

    private sealed class FakeTrivyService : ITrivyService
    {
        public bool WasCalled { get; private set; }
        public string? LocateExecutable(string? configuredPath = null) => "trivy";
        public Task<bool> IsInstalledAsync(string? configuredPath = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<string?> GetVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default) => Task.FromResult<string?>("Trivy 1.0");
        public async Task<ProcessResult> ScanFileSystemAsync(string projectPath, string outputJsonPath, TrivyOptions options, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            await File.WriteAllTextAsync(outputJsonPath, "{\"Results\":[]}", cancellationToken);
            var now = DateTimeOffset.UtcNow;
            return new ProcessResult("trivy", [], now, now, 0, CommandExecutionStatus.Succeeded, string.Empty, string.Empty);
        }
    }

    private sealed class EmptyReportParser : ITrivyReportParser
    {
        public Task<IReadOnlyList<Finding>> ParseAsync(string reportPath, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Finding>>([]);
    }

    private sealed class FixedDetectionService(ProjectDetectionResult detection) : IProjectDetectionService
    {
        public Task<ProjectDetectionResult> DetectAsync(string projectPath, CancellationToken cancellationToken = default) => Task.FromResult(detection);
    }

    private sealed class TestSettingsService : IAppSettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppSettings());
        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestStoragePathService(string root) : IStoragePathService
    {
        public string GetDatabasePath() => Path.Combine(root, "data.db");
        public string GetSettingsPath() => Path.Combine(root, "settings.json");
        public string GetManagedTrivyExecutablePath() => Path.Combine(root, "trivy.exe");
        public string GetReportDirectory(Project project) => Path.Combine(root, "reports");
        public string GetLogDirectory(Project project) => Path.Combine(root, "logs");
        public string GetSbomDirectory(Project project) => Path.Combine(root, "sbom");
        public string GetReportPath(Project project, Guid scanId) => Path.Combine(GetReportDirectory(project), $"{scanId}.json");
        public string GetLogPath(Project project, Guid scanId) => Path.Combine(GetLogDirectory(project), $"{scanId}.log");
    }

    private sealed class NoOpRetentionService : IRetentionService
    {
        public Task ApplyAsync(Guid projectId, int maxHistory, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpDependencyAnalysisService : IDependencyAnalysisService
    {
        public Task AnalyzeAsync(Project project, IReadOnlyCollection<Finding> findings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpEnrichmentService : IVulnerabilityEnrichmentService
    {
        public Task<VulnerabilityEnrichmentResult?> TryEnrichAsync(string vulnerabilityId, CancellationToken cancellationToken = default) => Task.FromResult<VulnerabilityEnrichmentResult?>(null);
    }
}
