using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface IProjectDetectionService
{
    Task<ProjectDetectionResult> DetectAsync(string projectPath, CancellationToken cancellationToken = default);
}
