using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface IAppSettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
