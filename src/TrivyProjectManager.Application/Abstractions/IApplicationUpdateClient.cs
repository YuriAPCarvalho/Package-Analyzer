using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface IApplicationUpdateClient
{
    string InstalledVersion { get; }

    Task<ApplicationUpdatePackage?> CheckForUpdatesAsync(
        string channel,
        CancellationToken cancellationToken = default);

    Task DownloadUpdateAsync(
        ApplicationUpdatePackage package,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    void ApplyUpdateAndRestart(ApplicationUpdatePackage package);
}
