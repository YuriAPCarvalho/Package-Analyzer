using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class CommandProfileService : ICommandProfileService
{
    public IReadOnlyList<ProjectCommand> CreateAutomaticCommands(ProjectDetectionResult detection)
    {
        var commands = new List<ProjectCommand>();
        var order = 1;
        foreach (var target in detection.Targets)
        {
            switch (target.PackageManager)
            {
                case PackageManagerType.DotNetCli:
                    commands.Add(Create(target, "Restore", "dotnet", $"restore {Quote(Path.GetFileName(target.ManifestPath))}", order++, target.RootPath));
                    commands.Add(Create(target, "Build", "dotnet", $"build {Quote(Path.GetFileName(target.ManifestPath))} --no-restore", order++, target.RootPath));
                    break;
                case PackageManagerType.Npm:
                case PackageManagerType.Pnpm:
                case PackageManagerType.Yarn:
                    commands.Add(Create(target, "Install", Executable(target.PackageManager), InstallArguments(target), order++, target.RootPath));
                    foreach (var buildDirectory in target.BuildDirectories)
                    {
                        commands.Add(Create(target, "Build", Executable(target.PackageManager), "run build", order++, buildDirectory));
                    }
                    break;
                case PackageManagerType.Maven:
                    commands.Add(Create(target, "Build", target.RequiredExecutables.Last(), "package -DskipTests", order++, target.RootPath));
                    break;
                case PackageManagerType.Gradle:
                    commands.Add(Create(target, "Build", target.RequiredExecutables.Last(), "build -x test", order++, target.RootPath));
                    break;
            }
        }

        return commands;
    }

    private static string InstallArguments(DetectedProjectTarget target)
    {
        return target.PackageManager switch
        {
            PackageManagerType.Npm when File.Exists(Path.Combine(target.RootPath, "package-lock.json"))
                || File.Exists(Path.Combine(target.RootPath, "npm-shrinkwrap.json")) => "ci",
            PackageManagerType.Npm => "install",
            PackageManagerType.Pnpm when File.Exists(Path.Combine(target.RootPath, "pnpm-lock.yaml")) => "install --frozen-lockfile",
            PackageManagerType.Pnpm => "install",
            PackageManagerType.Yarn when File.Exists(Path.Combine(target.RootPath, ".yarnrc.yml")) => "install --immutable",
            PackageManagerType.Yarn when File.Exists(Path.Combine(target.RootPath, "yarn.lock")) => "install --frozen-lockfile",
            PackageManagerType.Yarn => "install",
            _ => "install"
        };
    }

    private static string Executable(PackageManagerType manager) => manager switch
    {
        PackageManagerType.Pnpm => "pnpm",
        PackageManagerType.Yarn => "yarn",
        _ => "npm"
    };

    private static ProjectCommand Create(DetectedProjectTarget target, string operation, string executable, string arguments, int order, string workingDirectory)
    {
        var relativeTarget = Path.GetFileName(target.ManifestPath);
        return new ProjectCommand
        {
            Name = $"{operation} ({target.PackageManager}: {relativeTarget})",
            Command = executable,
            Arguments = arguments,
            ExecutionOrder = order,
            IsEnabled = true,
            WorkingDirectory = workingDirectory,
            PreparationTargetKey = target.Key
        };
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
