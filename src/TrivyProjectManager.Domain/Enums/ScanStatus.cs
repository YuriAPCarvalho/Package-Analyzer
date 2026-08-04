namespace TrivyProjectManager.Domain.Enums;

public enum ScanStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    TimedOut = 5
}
