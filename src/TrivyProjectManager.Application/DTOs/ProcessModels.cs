using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.DTOs;

public sealed record ProcessRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan? Timeout = null);

public sealed record ProcessLogLine(DateTimeOffset At, string Stream, string Message);

public sealed record ProcessResult(
    string FileName,
    IReadOnlyList<string> Arguments,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    int ExitCode,
    CommandExecutionStatus Status,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => Status == CommandExecutionStatus.Succeeded && ExitCode == 0;
}
