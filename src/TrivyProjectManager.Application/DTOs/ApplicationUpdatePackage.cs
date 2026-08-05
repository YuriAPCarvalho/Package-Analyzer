namespace TrivyProjectManager.Application.DTOs;

public sealed record ApplicationUpdatePackage(
    string Id,
    string Version,
    string? ReleaseNotesMarkdown,
    string? ReleaseNotesHtml);
