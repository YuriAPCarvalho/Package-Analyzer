using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.DTOs;

public sealed record ProjectDetectionResult(
    string ProjectPath,
    IReadOnlyList<ProjectTechnology> Technologies,
    IReadOnlyList<PackageManagerType> PackageManagers,
    ProjectTechnology SuggestedTechnology,
    PackageManagerType SuggestedPackageManager);
