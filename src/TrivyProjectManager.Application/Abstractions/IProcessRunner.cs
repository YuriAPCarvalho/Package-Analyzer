using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        ProcessRequest request,
        IProgress<ProcessLogLine>? progress = null,
        CancellationToken cancellationToken = default);
}
