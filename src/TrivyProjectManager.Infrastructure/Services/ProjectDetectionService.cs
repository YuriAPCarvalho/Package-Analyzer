using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class ProjectDetectionService : IProjectDetectionService
{
    public Task<ProjectDetectionResult> DetectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Project folder was not found: {projectPath}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var technologies = new List<ProjectTechnology>();
        var managers = new List<PackageManagerType>();

        if (Directory.EnumerateFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly).Any()
            || Directory.EnumerateFiles(projectPath, "*.csproj", SearchOption.AllDirectories).Any()
            || File.Exists(Path.Combine(projectPath, "global.json"))
            || File.Exists(Path.Combine(projectPath, "Directory.Packages.props"))
            || File.Exists(Path.Combine(projectPath, "packages.lock.json")))
        {
            technologies.Add(ProjectTechnology.DotNet);
            managers.Add(PackageManagerType.DotNetCli);
        }

        var hasPackageJson = File.Exists(Path.Combine(projectPath, "package.json"));
        if (hasPackageJson || File.Exists(Path.Combine(projectPath, "package-lock.json")))
        {
            technologies.Add(ProjectTechnology.Node);
            managers.Add(PackageManagerType.Npm);
        }

        if (File.Exists(Path.Combine(projectPath, "pnpm-lock.yaml")))
        {
            technologies.Add(ProjectTechnology.Node);
            managers.Insert(0, PackageManagerType.Pnpm);
        }

        if (File.Exists(Path.Combine(projectPath, "yarn.lock")))
        {
            technologies.Add(ProjectTechnology.Node);
            managers.Insert(0, PackageManagerType.Yarn);
        }

        technologies = [.. technologies.Distinct()];
        managers = [.. managers.Distinct()];

        return Task.FromResult(new ProjectDetectionResult(
            projectPath,
            technologies,
            managers,
            technologies.FirstOrDefault(ProjectTechnology.Unknown),
            managers.FirstOrDefault(PackageManagerType.Unknown)));
    }
}
