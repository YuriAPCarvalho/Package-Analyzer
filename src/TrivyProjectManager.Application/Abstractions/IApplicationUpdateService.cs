using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface IApplicationUpdateService
{
    string InstalledVersion { get; }

    Task<ApplicationUpdateResult> CheckForUpdatesAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default);

    Task<ApplicationUpdateResult> DownloadAndApplyAsync(
        AppSettings settings,
        ApplicationUpdateResult update,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
