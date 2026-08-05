using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class UpdateCommandService
{
    public string? Build(Project project, Finding finding)
    {
        if (finding.DependencyRelation != DependencyRelation.Direct
            || string.IsNullOrWhiteSpace(finding.PackageName)
            || string.IsNullOrWhiteSpace(finding.RecommendedFixedVersion))
        {
            return null;
        }

        var projectFile = finding.Occurrences.Select(occurrence => occurrence.ProjectFilePath).FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        return project.PackageManager switch
        {
            PackageManagerType.DotNetCli when !string.IsNullOrWhiteSpace(projectFile) && projectFile.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) =>
                $"dotnet add \"{projectFile}\" package {finding.PackageName} --version {finding.RecommendedFixedVersion}",
            PackageManagerType.Npm =>
                $"npm install {finding.PackageName}@{finding.RecommendedFixedVersion}",
            PackageManagerType.Pnpm =>
                $"pnpm add {finding.PackageName}@{finding.RecommendedFixedVersion}",
            PackageManagerType.Yarn =>
                $"yarn add {finding.PackageName}@{finding.RecommendedFixedVersion}",
            _ => null
        };
    }
}
