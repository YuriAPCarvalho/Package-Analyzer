namespace TrivyProjectManager.App.DTOs;

public sealed record SecurityExceptionDialogResult(string Reason, DateTimeOffset? ExpiresAt);
