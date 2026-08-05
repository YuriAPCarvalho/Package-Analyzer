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
        if (!string.IsNullOrWhiteSpace(projectFile)
            && new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(Path.GetExtension(projectFile), StringComparer.OrdinalIgnoreCase))
        {
            return $"dotnet add \"{projectFile}\" package {finding.PackageName} --version {finding.RecommendedFixedVersion}";
        }

        var manager = project.PackageManager;
        if (!string.IsNullOrWhiteSpace(projectFile) && Path.GetFileName(projectFile).Equals("package.json", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(projectFile)!;
            manager = File.Exists(Path.Combine(directory, "pnpm-lock.yaml")) ? PackageManagerType.Pnpm
                : File.Exists(Path.Combine(directory, "yarn.lock")) ? PackageManagerType.Yarn
                : PackageManagerType.Npm;
        }

        return manager switch
        {
            PackageManagerType.Npm => $"npm install {finding.PackageName}@{finding.RecommendedFixedVersion}",
            PackageManagerType.Pnpm => $"pnpm add {finding.PackageName}@{finding.RecommendedFixedVersion}",
            PackageManagerType.Yarn => $"yarn add {finding.PackageName}@{finding.RecommendedFixedVersion}",
            _ => null
        };
    }
}
