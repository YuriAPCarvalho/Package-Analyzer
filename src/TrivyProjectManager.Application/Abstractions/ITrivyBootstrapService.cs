using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface ITrivyBootstrapService
{
    Task<TrivyBootstrapResult> EnsureAvailableAsync(
        AppSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
