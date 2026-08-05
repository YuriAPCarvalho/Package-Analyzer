using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface ITrivyReleaseClient
{
    Task<TrivyReleasePackage> GetLatestWindowsX64Async(CancellationToken cancellationToken = default);

    Task DownloadAsync(TrivyReleasePackage package, string destinationPath, CancellationToken cancellationToken = default);
}
