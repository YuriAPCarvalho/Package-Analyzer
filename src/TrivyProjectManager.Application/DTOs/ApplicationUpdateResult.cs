namespace TrivyProjectManager.Application.DTOs;

public sealed record ApplicationUpdateResult(
    string InstalledVersion,
    string? AvailableVersion,
    string? ReleaseNotes,
    ApplicationUpdateStatus Status,
    string Message,
    DateTimeOffset CheckedAtUtc,
    ApplicationUpdatePackage? Package = null);
