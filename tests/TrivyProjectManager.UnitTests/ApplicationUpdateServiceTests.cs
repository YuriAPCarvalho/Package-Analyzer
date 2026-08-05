using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;

namespace TrivyProjectManager.UnitTests;

public sealed class ApplicationUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdatesReturnsUpToDateWhenNoPackageExists()
    {
        var settings = new AppSettings();
        var settingsService = new RecordingSettingsService();
        var updateService = new ApplicationUpdateService(new FakeUpdateClient(null), settingsService);

        var result = await updateService.CheckForUpdatesAsync(settings);

        Assert.Equal(ApplicationUpdateStatus.UpToDate, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.Equal("UpToDate", settings.LastApplicationUpdateStatus);
        Assert.NotNull(settings.LastApplicationUpdateCheckUtc);
        Assert.Contains(ApplicationUpdateStatus.Checking, settingsService.SavedStatuses);
        Assert.Contains(ApplicationUpdateStatus.UpToDate, settingsService.SavedStatuses);
    }

    [Fact]
    public async Task CheckForUpdatesReturnsRequiredUpdateWhenPackageExists()
    {
        var package = new ApplicationUpdatePackage("pkg", "0.1.1", "Correções importantes", null);
        var updateService = new ApplicationUpdateService(new FakeUpdateClient(package), new RecordingSettingsService());

        var result = await updateService.CheckForUpdatesAsync(new AppSettings());

        Assert.Equal(ApplicationUpdateStatus.UpdateAvailable, result.Status);
        Assert.Equal("0.1.1", result.AvailableVersion);
        Assert.Equal("Correções importantes", result.ReleaseNotes);
        Assert.Same(package, result.Package);
    }

    [Fact]
    public async Task CheckForUpdatesMapsNotInstalled()
    {
        var updateService = new ApplicationUpdateService(
            new FakeUpdateClient(null) { CheckException = new ApplicationUpdateNotInstalledException("Fora do Velopack") },
            new RecordingSettingsService());
        var settings = new AppSettings();

        var result = await updateService.CheckForUpdatesAsync(settings);

        Assert.Equal(ApplicationUpdateStatus.NotInstalled, result.Status);
        Assert.Equal("NotInstalled", settings.LastApplicationUpdateStatus);
        Assert.Contains("Fora do Velopack", result.Message);
    }

    [Fact]
    public async Task CheckForUpdatesMapsFailure()
    {
        var updateService = new ApplicationUpdateService(
            new FakeUpdateClient(null) { CheckException = new InvalidOperationException("GitHub indisponível") },
            new RecordingSettingsService());

        var result = await updateService.CheckForUpdatesAsync(new AppSettings());

        Assert.Equal(ApplicationUpdateStatus.Failed, result.Status);
        Assert.Contains("GitHub indisponível", result.Message);
    }

    [Fact]
    public async Task DownloadAndApplyDownloadsAndRequestsRestart()
    {
        var package = new ApplicationUpdatePackage("pkg", "0.1.1", null, null);
        var client = new FakeUpdateClient(package);
        var settings = new AppSettings();
        var settingsService = new RecordingSettingsService();
        var updateService = new ApplicationUpdateService(client, settingsService);
        var update = new ApplicationUpdateResult("0.1.0", "0.1.1", null, ApplicationUpdateStatus.UpdateAvailable, "Atualização disponível.", DateTimeOffset.UtcNow, package);

        var result = await updateService.DownloadAndApplyAsync(settings, update);

        Assert.Equal(ApplicationUpdateStatus.Applying, result.Status);
        Assert.True(client.Downloaded);
        Assert.True(client.Applied);
        Assert.Equal("Applying", settings.LastApplicationUpdateStatus);
        Assert.Contains(ApplicationUpdateStatus.Downloading, settingsService.SavedStatuses);
        Assert.Contains(ApplicationUpdateStatus.Applying, settingsService.SavedStatuses);
    }

    private sealed class RecordingSettingsService : IAppSettingsService
    {
        public List<ApplicationUpdateStatus> SavedStatuses { get; } = [];

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AppSettings());
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            if (Enum.TryParse<ApplicationUpdateStatus>(settings.LastApplicationUpdateStatus, out var status))
            {
                SavedStatuses.Add(status);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUpdateClient(ApplicationUpdatePackage? package) : IApplicationUpdateClient
    {
        public Exception? CheckException { get; set; }
        public bool Downloaded { get; private set; }
        public bool Applied { get; private set; }
        public string InstalledVersion => "0.1.0";

        public Task<ApplicationUpdatePackage?> CheckForUpdatesAsync(string channel, CancellationToken cancellationToken = default)
        {
            if (CheckException is not null)
            {
                throw CheckException;
            }

            return Task.FromResult(package);
        }

        public Task DownloadUpdateAsync(ApplicationUpdatePackage updatePackage, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            Downloaded = true;
            progress?.Report(100);
            return Task.CompletedTask;
        }

        public void ApplyUpdateAndRestart(ApplicationUpdatePackage updatePackage)
        {
            Applied = true;
        }
    }
}
