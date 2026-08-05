using System.IO.Compression;
using System.Security.Cryptography;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.UnitTests;

public sealed class TrivyBootstrapServiceTests
{
    [Fact]
    public async Task CurrentManagedVersionChecksReleaseWithoutDownloading()
    {
        using var directory = TempDirectory.Create();
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(managedPath, "current");
        var trivy = new FakeTrivyService(managedPath, new Dictionary<string, string> { [managedPath] = "Version: 0.73.0" });
        var release = FakeReleaseClient.Create("0.73.0");
        var settings = new AppSettings { TrivyPath = managedPath };
        var service = CreateService(directory.Path, trivy, release, out var settingsService);

        var result = await service.EnsureAvailableAsync(settings);

        Assert.False(result.InstalledOrUpdated);
        Assert.False(release.Downloaded);
        Assert.True(settingsService.Saved);
        Assert.Contains("atualizado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalVersionMigratesToManagedCopyWithoutChangingExternalFile()
    {
        using var directory = TempDirectory.Create();
        var externalPath = Path.Combine(directory.Path, "external", "trivy.exe");
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(externalPath, "external-original");
        var versions = new Dictionary<string, string> { [externalPath] = "Version: 0.73.0" };
        var trivy = new FakeTrivyService(externalPath, versions, path => path.Contains("extract-", StringComparison.OrdinalIgnoreCase) || path == managedPath ? "Version: 0.73.0" : null);
        var release = FakeReleaseClient.Create("0.73.0");
        var settings = new AppSettings { TrivyPath = externalPath };
        var service = CreateService(directory.Path, trivy, release, out var settingsService);

        var result = await service.EnsureAvailableAsync(settings);

        Assert.True(result.InstalledOrUpdated);
        Assert.True(release.Downloaded);
        Assert.True(settingsService.Saved);
        Assert.Equal(managedPath, settings.TrivyPath);
        Assert.Equal("external-original", await File.ReadAllTextAsync(externalPath));
        Assert.True(File.Exists(managedPath));
    }

    [Fact]
    public async Task OlderManagedVersionIsReplacedAfterPackageValidation()
    {
        using var directory = TempDirectory.Create();
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(managedPath, "old-version");
        var versions = new Dictionary<string, string> { [managedPath] = "Version: 0.72.0" };
        var trivy = new FakeTrivyService(managedPath, versions, path =>
        {
            if (path.Contains("extract-", StringComparison.OrdinalIgnoreCase))
            {
                return "Version: 0.73.0";
            }

            return path == managedPath && File.Exists(path) && File.ReadAllText(path) == "test executable"
                ? "Version: 0.73.0"
                : null;
        });
        var release = FakeReleaseClient.Create("0.73.0");
        var service = CreateService(directory.Path, trivy, release, out var settingsService);

        var result = await service.EnsureAvailableAsync(new AppSettings { TrivyPath = managedPath });

        Assert.True(result.InstalledOrUpdated);
        Assert.True(settingsService.Saved);
        Assert.Equal("test executable", await File.ReadAllTextAsync(managedPath));
    }

    [Fact]
    public async Task DoesNotDowngradeNewerExternalVersion()
    {
        using var directory = TempDirectory.Create();
        var externalPath = Path.Combine(directory.Path, "external", "trivy.exe");
        CreateExecutable(externalPath, "newer");
        var trivy = new FakeTrivyService(externalPath, new Dictionary<string, string> { [externalPath] = "Version: 0.74.0" });
        var release = FakeReleaseClient.Create("0.73.0");
        var settings = new AppSettings { TrivyPath = externalPath };
        var service = CreateService(directory.Path, trivy, release, out _);

        var result = await service.EnsureAvailableAsync(settings);

        Assert.False(result.InstalledOrUpdated);
        Assert.False(release.Downloaded);
        Assert.Equal(externalPath, result.ExecutablePath);
        Assert.Contains("downgrade", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidDigestPreservesExistingManagedExecutable()
    {
        using var directory = TempDirectory.Create();
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(managedPath, "old-version");
        var trivy = new FakeTrivyService(managedPath, new Dictionary<string, string> { [managedPath] = "Version: 0.72.0" });
        var release = FakeReleaseClient.Create("0.73.0", overrideSha256: new string('0', 64));
        var settings = new AppSettings { TrivyPath = managedPath };
        var service = CreateService(directory.Path, trivy, release, out _);

        var result = await service.EnsureAvailableAsync(settings);

        Assert.False(result.InstalledOrUpdated);
        Assert.Equal("old-version", await File.ReadAllTextAsync(managedPath));
        Assert.Contains("SHA-256", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MismatchedExtractedVersionPreservesExistingManagedExecutable()
    {
        using var directory = TempDirectory.Create();
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(managedPath, "old-version");
        var trivy = new FakeTrivyService(
            managedPath,
            new Dictionary<string, string> { [managedPath] = "Version: 0.72.0" },
            path => path.Contains("extract-", StringComparison.OrdinalIgnoreCase) ? "Version: 0.72.0" : null);
        var release = FakeReleaseClient.Create("0.73.0");
        var service = CreateService(directory.Path, trivy, release, out _);

        var result = await service.EnsureAvailableAsync(new AppSettings { TrivyPath = managedPath });

        Assert.False(result.InstalledOrUpdated);
        Assert.Equal("old-version", await File.ReadAllTextAsync(managedPath));
        Assert.Contains("não corresponde", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseFailureFallsBackToUsableExistingVersion()
    {
        using var directory = TempDirectory.Create();
        var managedPath = Path.Combine(directory.Path, "managed", "trivy.exe");
        CreateExecutable(managedPath, "current");
        var trivy = new FakeTrivyService(managedPath, new Dictionary<string, string> { [managedPath] = "Version: 0.72.0" });
        var release = FakeReleaseClient.Failing(new HttpRequestException("offline"));
        var service = CreateService(directory.Path, trivy, release, out _);

        var result = await service.EnsureAvailableAsync(new AppSettings { TrivyPath = managedPath });

        Assert.Equal(managedPath, result.ExecutablePath);
        Assert.Contains("versão instalada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseFailureWithoutExistingVersionIsFatal()
    {
        using var directory = TempDirectory.Create();
        var release = FakeReleaseClient.Failing(new HttpRequestException("offline"));
        var service = CreateService(directory.Path, new FakeTrivyService(null, new Dictionary<string, string>()), release, out _);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.EnsureAvailableAsync(new AppSettings()));

        Assert.Equal("offline", exception.Message);
    }

    private static TrivyBootstrapService CreateService(
        string root,
        FakeTrivyService trivy,
        FakeReleaseClient release,
        out RecordingSettingsService settingsService)
    {
        settingsService = new RecordingSettingsService();
        return new TrivyBootstrapService(trivy, release, settingsService, new TestStoragePathService(root));
    }

    private static void CreateExecutable(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private sealed class FakeReleaseClient(TrivyReleasePackage? package, byte[] archive, Exception? exception) : ITrivyReleaseClient
    {
        public bool Downloaded { get; private set; }

        public static FakeReleaseClient Create(string version, string? overrideSha256 = null)
        {
            var archive = CreateArchive();
            var sha256 = overrideSha256 ?? Convert.ToHexStringLower(SHA256.HashData(archive));
            var package = new TrivyReleasePackage($"v{version}", Version.Parse(version), $"trivy_{version}_windows-64bit.zip", new Uri("https://example.invalid/trivy.zip"), sha256, archive.Length);
            return new FakeReleaseClient(package, archive, null);
        }

        public static FakeReleaseClient Failing(Exception exception) => new(null, [], exception);

        public Task<TrivyReleasePackage> GetLatestWindowsX64Async(CancellationToken cancellationToken = default)
        {
            return exception is null
                ? Task.FromResult(package!)
                : Task.FromException<TrivyReleasePackage>(exception);
        }

        public async Task DownloadAsync(TrivyReleasePackage releasePackage, string destinationPath, CancellationToken cancellationToken = default)
        {
            Downloaded = true;
            await File.WriteAllBytesAsync(destinationPath, archive, cancellationToken);
        }

        private static byte[] CreateArchive()
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("trivy.exe");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("test executable");
            }

            return stream.ToArray();
        }
    }

    private sealed class FakeTrivyService(
        string? locatedExecutable,
        IReadOnlyDictionary<string, string> versions,
        Func<string, string?>? dynamicVersion = null) : ITrivyService
    {
        public string? LocateExecutable(string? configuredPath = null)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                return configuredPath;
            }

            return locatedExecutable;
        }

        public Task<bool> IsInstalledAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LocateExecutable(configuredPath) is not null);
        }

        public Task<string?> GetVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
        {
            var dynamicResult = dynamicVersion?.Invoke(configuredPath ?? string.Empty);
            if (dynamicResult is not null)
            {
                return Task.FromResult<string?>(dynamicResult);
            }

            if (configuredPath is not null && versions.TryGetValue(configuredPath, out var version))
            {
                return Task.FromResult<string?>(version);
            }

            return Task.FromResult<string?>(null);
        }

        public Task<ProcessResult> ScanFileSystemAsync(string projectPath, string outputJsonPath, TrivyOptions options, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingSettingsService : IAppSettingsService
    {
        public bool Saved { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppSettings());

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestStoragePathService(string root) : IStoragePathService
    {
        public string GetDatabasePath() => Path.Combine(root, "data.db");
        public string GetSettingsPath() => Path.Combine(root, "settings.json");
        public string GetManagedTrivyExecutablePath() => Path.Combine(root, "managed", "trivy.exe");
        public string GetReportDirectory(Project project) => Path.Combine(root, "reports");
        public string GetLogDirectory(Project project) => Path.Combine(root, "logs");
        public string GetSbomDirectory(Project project) => Path.Combine(root, "sbom");
        public string GetReportPath(Project project, Guid scanId) => Path.Combine(GetReportDirectory(project), $"{scanId}.json");
        public string GetLogPath(Project project, Guid scanId) => Path.Combine(GetLogDirectory(project), $"{scanId}.log");
    }
}
