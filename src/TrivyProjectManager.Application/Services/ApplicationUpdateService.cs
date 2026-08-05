using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Services;

public sealed class ApplicationUpdateService(
    IApplicationUpdateClient updateClient,
    IAppSettingsService settingsService) : IApplicationUpdateService
{
    public string InstalledVersion => updateClient.InstalledVersion;

    public async Task<ApplicationUpdateResult> CheckForUpdatesAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        await PersistStatusAsync(settings, ApplicationUpdateStatus.Checking, null, cancellationToken);

        try
        {
            var package = await updateClient.CheckForUpdatesAsync(NormalizeChannel(settings.ApplicationUpdateChannel), cancellationToken);
            var checkedAt = DateTimeOffset.UtcNow;
            if (package is null)
            {
                var result = new ApplicationUpdateResult(
                    InstalledVersion,
                    null,
                    null,
                    ApplicationUpdateStatus.UpToDate,
                    "Aplicação atualizada.",
                    checkedAt);
                await PersistStatusAsync(settings, result.Status, checkedAt, cancellationToken);
                return result;
            }

            var updateResult = new ApplicationUpdateResult(
                InstalledVersion,
                package.Version,
                SelectReleaseNotes(package),
                ApplicationUpdateStatus.UpdateAvailable,
                $"Atualização {package.Version} disponível.",
                checkedAt,
                package);
            await PersistStatusAsync(settings, updateResult.Status, checkedAt, cancellationToken);
            return updateResult;
        }
        catch (ApplicationUpdateNotInstalledException ex)
        {
            return await BuildAndPersistFailureAsync(settings, ApplicationUpdateStatus.NotInstalled, ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            return await BuildAndPersistFailureAsync(settings, ApplicationUpdateStatus.Failed, $"Não foi possível verificar atualização: {ex.Message}", cancellationToken);
        }
    }

    public async Task<ApplicationUpdateResult> DownloadAndApplyAsync(
        AppSettings settings,
        ApplicationUpdateResult update,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (update.Package is null)
        {
            return await BuildAndPersistFailureAsync(settings, ApplicationUpdateStatus.Failed, "Nenhuma atualização foi selecionada para download.", cancellationToken);
        }

        try
        {
            await PersistStatusAsync(settings, ApplicationUpdateStatus.Downloading, DateTimeOffset.UtcNow, cancellationToken);
            await updateClient.DownloadUpdateAsync(update.Package, progress, cancellationToken);
            await PersistStatusAsync(settings, ApplicationUpdateStatus.Applying, DateTimeOffset.UtcNow, cancellationToken);
            updateClient.ApplyUpdateAndRestart(update.Package);

            return update with
            {
                Status = ApplicationUpdateStatus.Applying,
                Message = "Aplicando atualização e reiniciando...",
                CheckedAtUtc = DateTimeOffset.UtcNow
            };
        }
        catch (ApplicationUpdateNotInstalledException ex)
        {
            return await BuildAndPersistFailureAsync(settings, ApplicationUpdateStatus.NotInstalled, ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            return await BuildAndPersistFailureAsync(settings, ApplicationUpdateStatus.Failed, $"Não foi possível aplicar atualização: {ex.Message}", cancellationToken);
        }
    }

    private static string NormalizeChannel(string? channel)
    {
        return string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();
    }

    private static string? SelectReleaseNotes(ApplicationUpdatePackage package)
    {
        return !string.IsNullOrWhiteSpace(package.ReleaseNotesMarkdown)
            ? package.ReleaseNotesMarkdown
            : package.ReleaseNotesHtml;
    }

    private async Task<ApplicationUpdateResult> BuildAndPersistFailureAsync(
        AppSettings settings,
        ApplicationUpdateStatus status,
        string message,
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        await PersistStatusAsync(settings, status, checkedAt, cancellationToken);
        return new ApplicationUpdateResult(InstalledVersion, null, null, status, message, checkedAt);
    }

    private async Task PersistStatusAsync(
        AppSettings settings,
        ApplicationUpdateStatus status,
        DateTimeOffset? checkedAt,
        CancellationToken cancellationToken)
    {
        settings.LastApplicationUpdateStatus = status.ToString();
        if (checkedAt.HasValue)
        {
            settings.LastApplicationUpdateCheckUtc = checkedAt;
        }

        await settingsService.SaveAsync(settings, cancellationToken);
    }
}
