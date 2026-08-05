using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Abstractions;

public interface IDependencyAnalysisService
{
    Task AnalyzeAsync(Project project, IReadOnlyCollection<Finding> findings, CancellationToken cancellationToken = default);
}
