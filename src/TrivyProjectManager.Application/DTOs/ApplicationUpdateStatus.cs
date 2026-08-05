namespace TrivyProjectManager.Application.DTOs;

public enum ApplicationUpdateStatus
{
    Idle,
    Checking,
    UpToDate,
    UpdateAvailable,
    Downloading,
    Applying,
    Failed,
    NotInstalled
}
