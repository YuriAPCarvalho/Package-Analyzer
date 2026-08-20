using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrivyProjectManager.App.DTOs;
using TrivyProjectManager.App.Services;
using TrivyProjectManager.App.ViewModels;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.UnitTests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task AddProjectSelectsExistingProjectWhenFolderIsAlreadyRegistered()
    {
        using var directory = TempDirectory.Create();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TrivyProjectManagerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new TrivyProjectManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var existingProject = new Project
        {
            Name = "Existing",
            Path = directory.Path,
            Technology = ProjectTechnology.DotNet,
            PackageManager = PackageManagerType.DotNetCli
        };
        dbContext.Projects.Add(existingProject);
        await dbContext.SaveChangesAsync();

        var dialog = new RecordingDialogService
        {
            Folder = directory.Path + Path.DirectorySeparatorChar
        };
        var detection = new RecordingProjectDetectionService();
        var viewModel = CreateViewModel(dbContext, dialog, detection);

        await viewModel.AddProjectCommand.ExecuteAsync(null);
        await Task.Delay(50);

        Assert.Equal(existingProject.Id, viewModel.SelectedProject?.Id);
        Assert.Equal(1, await dbContext.Projects.CountAsync());
        Assert.Equal(0, detection.CallCount);
        var message = Assert.Single(dialog.Messages);
        Assert.Equal("Projeto já cadastrado", message.Title);
    }

    private static MainWindowViewModel CreateViewModel(
        TrivyProjectManagerDbContext dbContext,
        IDialogService dialogService,
        IProjectDetectionService detectionService)
    {
        return new MainWindowViewModel(
            dbContext,
            detectionService,
            new CommandProfileService(),
            new NoOpScanOrchestrator(),
            new NoOpSettingsService(),
            new NoOpTrivyBootstrapService(),
            new NoOpExternalLinkService(),
            new NoOpApplicationUpdateService(),
            dialogService,
            new FindingTextService(),
            new UpdateCommandService());
    }

    private sealed class RecordingProjectDetectionService : IProjectDetectionService
    {
        public int CallCount { get; private set; }

        public Task<ProjectDetectionResult> DetectAsync(string projectPath, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectDetectionResult(
                projectPath,
                [ProjectTechnology.Unknown],
                [PackageManagerType.Unknown],
                ProjectTechnology.Unknown,
                PackageManagerType.Unknown,
                [],
                []));
        }
    }

    private sealed class RecordingDialogService : IDialogService
    {
        public string? Folder { get; init; }
        public List<(string Title, string Message)> Messages { get; } = [];

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult(Folder);
        public Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            Messages.Add((title, message));
            return Task.CompletedTask;
        }

        public Task<bool> ShowMandatoryUpdateAsync(ApplicationUpdateResult update, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task CopyToClipboardAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveTextFileAsync(string suggestedFileName, string content, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void CloseApplication()
        {
        }

        public Task<SecurityExceptionDialogResult?> ShowSecurityExceptionDialogAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SecurityExceptionDialogResult?>(null);
        }
    }

    private sealed class NoOpSettingsService : IAppSettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppSettings());
        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpTrivyBootstrapService : ITrivyBootstrapService
    {
        public Task<TrivyBootstrapResult> EnsureAvailableAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TrivyBootstrapResult(null, null, false, "Skipped"));
        }
    }

    private sealed class NoOpApplicationUpdateService : IApplicationUpdateService
    {
        public string InstalledVersion => "0.0.0";

        public Task<ApplicationUpdateResult> CheckForUpdatesAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ApplicationUpdateResult(
                InstalledVersion,
                null,
                null,
                ApplicationUpdateStatus.UpToDate,
                "Up to date.",
                DateTimeOffset.UtcNow));
        }

        public Task<ApplicationUpdateResult> DownloadAndApplyAsync(
            AppSettings settings,
            ApplicationUpdateResult update,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(update);
        }
    }

    private sealed class NoOpExternalLinkService : IExternalLinkService
    {
        public Task OpenAsync(string url, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpScanOrchestrator : IScanOrchestrator
    {
        public Task<ScanExecutionResult> RunAsync(
            Guid projectId,
            ScanMode mode,
            IProgress<ScanProgress>? progress = null,
            IProgress<ProcessLogLine>? logs = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
