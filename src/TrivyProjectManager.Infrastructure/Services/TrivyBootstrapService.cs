using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed partial class TrivyBootstrapService(
    ITrivyService trivyService,
    ITrivyReleaseClient releaseClient,
    IAppSettingsService settingsService,
    IStoragePathService storagePathService) : ITrivyBootstrapService
{
    public async Task<TrivyBootstrapResult> EnsureAvailableAsync(AppSettings settings, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var managedPath = storagePathService.GetManagedTrivyExecutablePath();
        var (existingPath, existingVersionText, existingVersion) = await FindUsableExistingAsync(settings, managedPath, cancellationToken);

        TrivyReleasePackage release;
        try
        {
            progress?.Report("Verificando atualização do Trivy...");
            release = await releaseClient.GetLatestWindowsX64Async(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && existingVersion is not null)
        {
            return new TrivyBootstrapResult(existingPath, existingVersionText, false, $"Não foi possível verificar atualização do Trivy; usando a versão instalada. {ex.Message}");
        }

        var existingIsManaged = PathsEqual(existingPath, managedPath);
        if (existingVersion is not null && existingVersion > release.Version)
        {
            return new TrivyBootstrapResult(existingPath, existingVersionText, false, "A versão instalada do Trivy é mais recente que o release estável; nenhum downgrade foi realizado.");
        }

        if (existingIsManaged && existingVersion == release.Version)
        {
            settings.TrivyPath = managedPath;
            try
            {
                await settingsService.SaveAsync(settings, cancellationToken);
                return new TrivyBootstrapResult(managedPath, existingVersionText, false, "Trivy já está atualizado.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new TrivyBootstrapResult(managedPath, existingVersionText, false, $"Trivy já está atualizado, mas não foi possível salvar a configuração: {ex.Message}");
            }
        }

        try
        {
            progress?.Report($"Baixando Trivy {release.TagName}...");
            await InstallReleaseAsync(release, managedPath, cancellationToken);
            var installedVersionText = await GetUsableVersionAsync(managedPath, cancellationToken)
                ?? throw new InvalidOperationException("O Trivy instalado não respondeu à verificação de versão.");
            settings.TrivyPath = managedPath;
            try
            {
                await settingsService.SaveAsync(settings, cancellationToken);
                return new TrivyBootstrapResult(managedPath, installedVersionText, true, $"Trivy {release.TagName} instalado/atualizado.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new TrivyBootstrapResult(managedPath, installedVersionText, true, $"Trivy {release.TagName} instalado/atualizado, mas não foi possível salvar a configuração: {ex.Message}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException && existingVersion is not null)
        {
            settings.TrivyPath = existingPath;
            return new TrivyBootstrapResult(existingPath, existingVersionText, false, $"Não foi possível atualizar o Trivy; usando a versão instalada. {ex.Message}");
        }
    }

    private async Task InstallReleaseAsync(TrivyReleasePackage release, string executablePath, CancellationToken cancellationToken)
    {
        var targetDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("Caminho gerenciado do Trivy inválido.");
        Directory.CreateDirectory(targetDirectory);

        var operationId = Guid.NewGuid().ToString("N");
        var tempZipPath = Path.Combine(targetDirectory, $"trivy-{operationId}.zip");
        var tempExtractDirectory = Path.Combine(targetDirectory, $"extract-{operationId}");
        var stagedExecutable = Path.Combine(targetDirectory, $"trivy-{operationId}.new.exe");
        var backupExecutable = Path.Combine(targetDirectory, $"trivy-{operationId}.bak.exe");
        var replacedExisting = false;

        try
        {
            await releaseClient.DownloadAsync(release, tempZipPath, cancellationToken);
            if (release.Size > 0 && new FileInfo(tempZipPath).Length != release.Size)
            {
                throw new InvalidOperationException("O tamanho do pacote baixado do Trivy não corresponde ao asset publicado.");
            }

            await ValidateSha256Async(tempZipPath, release.Sha256, cancellationToken);
            ZipFile.ExtractToDirectory(tempZipPath, tempExtractDirectory);

            var extractedExecutable = Directory.EnumerateFiles(tempExtractDirectory, "trivy.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("O pacote baixado do Trivy não contém trivy.exe.");
            var extractedVersionText = await GetUsableVersionAsync(extractedExecutable, cancellationToken);
            if (ParseVersion(extractedVersionText) != release.Version)
            {
                throw new InvalidOperationException($"A versão do executável baixado não corresponde ao release {release.TagName}.");
            }

            File.Copy(extractedExecutable, stagedExecutable, overwrite: false);
            if (File.Exists(executablePath))
            {
                File.Replace(stagedExecutable, executablePath, backupExecutable, ignoreMetadataErrors: true);
                replacedExisting = true;
            }
            else
            {
                File.Move(stagedExecutable, executablePath);
            }
        }
        catch
        {
            if (replacedExisting && File.Exists(backupExecutable))
            {
                File.Replace(backupExecutable, executablePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }

            throw;
        }
        finally
        {
            DeleteFileIfExists(tempZipPath);
            DeleteFileIfExists(stagedExecutable);
            DeleteFileIfExists(backupExecutable);
            if (Directory.Exists(tempExtractDirectory))
            {
                Directory.Delete(tempExtractDirectory, recursive: true);
            }
        }
    }

    private async Task<string?> GetUsableVersionAsync(string? executablePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return null;
        }

        try
        {
            return await trivyService.GetVersionAsync(executablePath, cancellationToken);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<(string? Path, string? VersionText, Version? Version)> FindUsableExistingAsync(AppSettings settings, string managedPath, CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            !string.IsNullOrWhiteSpace(settings.TrivyPath) && File.Exists(settings.TrivyPath) ? settings.TrivyPath : null,
            File.Exists(managedPath) ? managedPath : null,
            PathEnvironment.FindExecutable("trivy")
        }
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase);

        var usable = new List<(string Path, string VersionText, Version Version)>();
        foreach (var candidate in candidates)
        {
            var versionText = await GetUsableVersionAsync(candidate, cancellationToken);
            if (ParseVersion(versionText) is { } version)
            {
                usable.Add((candidate!, versionText!, version));
            }
        }

        var selected = usable
            .OrderByDescending(candidate => candidate.Version)
            .ThenByDescending(candidate => PathsEqual(candidate.Path, managedPath))
            .FirstOrDefault();
        return selected.Path is null
            ? (null, null, null)
            : selected;
    }

    private static Version? ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = VersionRegex().Match(value);
        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
    }

    private static async Task ValidateSha256Async(string path, string expectedSha256, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var actualHash = await SHA256.HashDataAsync(stream, cancellationToken);
        var actualSha256 = Convert.ToHexStringLower(actualHash);
        if (!actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O pacote baixado do Trivy falhou na validação SHA-256.");
        }
    }

    private static bool PathsEqual(string? left, string right)
    {
        return !string.IsNullOrWhiteSpace(left)
            && Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    [GeneratedRegex(@"(?<!\d)\d+\.\d+\.\d+(?:\.\d+)?(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex VersionRegex();
}
