using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface ITrivyService
{
    string? LocateExecutable(string? configuredPath = null);
    Task<bool> IsInstalledAsync(string? configuredPath = null, CancellationToken cancellationToken = default);
    Task<string?> GetVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default);
    Task<ProcessResult> ScanFileSystemAsync(
        string projectPath,
        string outputJsonPath,
        TrivyOptions options,
        IProgress<ProcessLogLine>? progress = null,
        CancellationToken cancellationToken = default);
}
