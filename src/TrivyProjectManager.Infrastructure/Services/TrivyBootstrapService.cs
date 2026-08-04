using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class TrivyBootstrapService(ITrivyService trivyService, IAppSettingsService settingsService, IStoragePathService storagePathService) : ITrivyBootstrapService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/aquasecurity/trivy/releases/latest";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TrivyBootstrapResult> EnsureAvailableAsync(AppSettings settings, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var managedPath = storagePathService.GetManagedTrivyExecutablePath();
        var configuredExists = !string.IsNullOrWhiteSpace(settings.TrivyPath) && File.Exists(settings.TrivyPath);
        var pathExists = trivyService.LocateExecutable(settings.TrivyPath) is not null;

        if (!settings.AutoInstallTrivy)
        {
            var version = await trivyService.GetVersionAsync(settings.TrivyPath, cancellationToken);
            return new TrivyBootstrapResult(trivyService.LocateExecutable(settings.TrivyPath), version, false, "Instalação automática do Trivy desativada.");
        }

        if (settings.AutoUpdateTrivyOnStartup)
        {
            try
            {
                var release = await GetLatestReleaseAsync(cancellationToken);
                if (ShouldInstallOrUpdate(settings, managedPath, release.TagName))
                {
                    progress?.Report($"Baixando Trivy {release.TagName}...");
                    await InstallReleaseAsync(release, managedPath, cancellationToken);
                    settings.TrivyPath = managedPath;
                    await settingsService.SaveAsync(settings, cancellationToken);
                    var version = await trivyService.GetVersionAsync(managedPath, cancellationToken);
                    return new TrivyBootstrapResult(managedPath, version, true, $"Trivy {release.TagName} instalado/atualizado.");
                }
            }
            catch (Exception ex) when (pathExists || configuredExists)
            {
                var currentVersion = await trivyService.GetVersionAsync(settings.TrivyPath, cancellationToken);
                return new TrivyBootstrapResult(trivyService.LocateExecutable(settings.TrivyPath), currentVersion, false, $"Não foi possível verificar atualização do Trivy: {ex.Message}");
            }
        }

        if (pathExists)
        {
            var currentPath = trivyService.LocateExecutable(settings.TrivyPath);
            var currentVersion = await trivyService.GetVersionAsync(settings.TrivyPath, cancellationToken);
            return new TrivyBootstrapResult(currentPath, currentVersion, false, "Trivy disponível.");
        }

        var latest = await GetLatestReleaseAsync(cancellationToken);
        progress?.Report($"Baixando Trivy {latest.TagName}...");
        await InstallReleaseAsync(latest, managedPath, cancellationToken);
        settings.TrivyPath = managedPath;
        await settingsService.SaveAsync(settings, cancellationToken);
        var installedVersion = await trivyService.GetVersionAsync(managedPath, cancellationToken);
        return new TrivyBootstrapResult(managedPath, installedVersion, true, $"Trivy {latest.TagName} instalado.");
    }

    private static bool ShouldInstallOrUpdate(AppSettings settings, string managedPath, string latestTag)
    {
        if (!string.IsNullOrWhiteSpace(settings.TrivyPath) && !string.Equals(settings.TrivyPath, managedPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!File.Exists(managedPath))
        {
            return PathEnvironment.FindExecutable("trivy") is null && string.IsNullOrWhiteSpace(settings.TrivyPath)
                || string.Equals(settings.TrivyPath, managedPath, StringComparison.OrdinalIgnoreCase);
        }

        var markerPath = GetVersionMarkerPath(managedPath);
        var installedTag = File.Exists(markerPath) ? File.ReadAllText(markerPath).Trim() : string.Empty;
        return !installedTag.Equals(latestTag, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<TrivyRelease> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Package-Analyzer", "1.0"));
        using var response = await httpClient.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseDto>(stream, JsonOptions, cancellationToken);
        var asset = release?.Assets?
            .FirstOrDefault(asset => asset.Name.EndsWith("Windows-64bit.zip", StringComparison.OrdinalIgnoreCase));

        if (release is null || string.IsNullOrWhiteSpace(release.TagName) || asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
        {
            throw new InvalidOperationException("Não foi possível localizar o pacote Windows x64 do Trivy no GitHub.");
        }

        return new TrivyRelease(release.TagName, asset.BrowserDownloadUrl);
    }

    private static async Task InstallReleaseAsync(TrivyRelease release, string executablePath, CancellationToken cancellationToken)
    {
        var targetDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("Caminho gerenciado do Trivy inválido.");
        Directory.CreateDirectory(targetDirectory);

        var tempZipPath = Path.Combine(targetDirectory, $"trivy-{Guid.NewGuid():N}.zip");
        var tempExtractDirectory = Path.Combine(targetDirectory, $"extract-{Guid.NewGuid():N}");
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Package-Analyzer", "1.0"));
                await using var download = await httpClient.GetStreamAsync(release.DownloadUrl, cancellationToken);
                await using var file = File.Create(tempZipPath);
                await download.CopyToAsync(file, cancellationToken);
            }

            ZipFile.ExtractToDirectory(tempZipPath, tempExtractDirectory);
            var extractedExecutable = Directory.EnumerateFiles(tempExtractDirectory, "trivy.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("O pacote baixado do Trivy não contém trivy.exe.");

            File.Copy(extractedExecutable, executablePath, overwrite: true);
            await File.WriteAllTextAsync(GetVersionMarkerPath(executablePath), release.TagName, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            if (Directory.Exists(tempExtractDirectory))
            {
                Directory.Delete(tempExtractDirectory, recursive: true);
            }
        }
    }

    private static string GetVersionMarkerPath(string executablePath) => Path.Combine(Path.GetDirectoryName(executablePath)!, "version.txt");

    private sealed record TrivyRelease(string TagName, string DownloadUrl);

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        public List<GitHubReleaseAssetDto>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAssetDto
    {
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
