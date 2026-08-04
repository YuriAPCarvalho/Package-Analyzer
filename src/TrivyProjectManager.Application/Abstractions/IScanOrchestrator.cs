using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Abstractions;

public interface IScanOrchestrator
{
    Task<ScanExecutionResult> RunAsync(
        Guid projectId,
        ScanMode mode,
        IProgress<ScanProgress>? progress = null,
        IProgress<ProcessLogLine>? logs = null,
        CancellationToken cancellationToken = default);
}
