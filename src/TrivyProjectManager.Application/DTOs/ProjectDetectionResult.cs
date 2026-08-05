using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.DTOs;

public sealed record ProjectDetectionResult(
    string ProjectPath,
    IReadOnlyList<ProjectTechnology> Technologies,
    IReadOnlyList<PackageManagerType> PackageManagers,
    ProjectTechnology SuggestedTechnology,
    PackageManagerType SuggestedPackageManager,
    IReadOnlyList<DetectedProjectTarget> Targets,
    IReadOnlyList<string> Warnings);

public sealed record DetectedProjectTarget(
    string Key,
    ProjectTechnology Technology,
    PackageManagerType PackageManager,
    string RootPath,
    string ManifestPath,
    IReadOnlyList<string> RequiredExecutables,
    IReadOnlyList<string> BuildDirectories);
