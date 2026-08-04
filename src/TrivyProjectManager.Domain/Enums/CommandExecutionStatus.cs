namespace TrivyProjectManager.Domain.Enums;

public enum CommandExecutionStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4,
    Cancelled = 5,
    TimedOut = 6
}
