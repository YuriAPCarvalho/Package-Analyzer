namespace TrivyProjectManager.Application.DTOs;

public sealed record TrivyBootstrapResult(
    string? ExecutablePath,
    string? Version,
    bool InstalledOrUpdated,
    string Message);
