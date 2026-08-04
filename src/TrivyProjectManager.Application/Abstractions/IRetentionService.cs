namespace TrivyProjectManager.Application.Abstractions;

public interface IRetentionService
{
    Task ApplyAsync(Guid projectId, int maxHistory, CancellationToken cancellationToken = default);
}
