namespace TrivyProjectManager.Application.Abstractions;

public interface IExternalLinkService
{
    Task OpenAsync(string url, CancellationToken cancellationToken = default);
}
