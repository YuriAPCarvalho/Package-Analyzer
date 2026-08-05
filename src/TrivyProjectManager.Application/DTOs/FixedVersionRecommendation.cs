namespace TrivyProjectManager.Application.DTOs;

public sealed record FixedVersionRecommendation(
    string? RecommendedVersion,
    IReadOnlyList<string> OtherVersions);
