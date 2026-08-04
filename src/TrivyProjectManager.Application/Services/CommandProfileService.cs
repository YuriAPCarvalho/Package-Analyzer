using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class CommandProfileService : ICommandProfileService
{
    public IReadOnlyList<ProjectCommand> CreateDefaultCommands(ProjectTechnology technology, PackageManagerType packageManager, string projectPath)
    {
        return packageManager switch
        {
            PackageManagerType.DotNetCli => DotNetCommands(),
            PackageManagerType.Npm => NpmCommands(projectPath),
            PackageManagerType.Pnpm => PnpmCommands(),
            PackageManagerType.Yarn => YarnCommands(projectPath),
            _ => []
        };
    }

    private static IReadOnlyList<ProjectCommand> DotNetCommands()
    {
        return
        [
            Create("Restore", "dotnet", "restore", 1),
            Create("Build", "dotnet", "build --no-restore", 2),
            Create("Test", "dotnet", "test --no-build", 3, isEnabled: false)
        ];
    }

    private static IReadOnlyList<ProjectCommand> NpmCommands(string projectPath)
    {
        var hasLockFile = File.Exists(Path.Combine(projectPath, "package-lock.json"));
        return hasLockFile
            ?
            [
                Create("Install", "npm", "ci", 1),
                Create("Build", "npm", "run build", 2),
                Create("Test", "npm", "test", 3, isEnabled: false)
            ]
            :
            [
                Create("Install", "npm", "install", 1),
                Create("Build", "npm", "run build", 2)
            ];
    }

    private static IReadOnlyList<ProjectCommand> PnpmCommands()
    {
        return
        [
            Create("Install", "pnpm", "install --frozen-lockfile", 1),
            Create("Build", "pnpm", "run build", 2)
        ];
    }

    private static IReadOnlyList<ProjectCommand> YarnCommands(string projectPath)
    {
        var yarnRc = Path.Combine(projectPath, ".yarnrc.yml");
        var installArgs = File.Exists(yarnRc) ? "install --immutable" : "install --frozen-lockfile";
        return
        [
            Create("Install", "yarn", installArgs, 1),
            Create("Build", "yarn", "run build", 2)
        ];
    }

    private static ProjectCommand Create(string name, string command, string arguments, int order, bool isEnabled = true)
    {
        return new ProjectCommand
        {
            Name = name,
            Command = command,
            Arguments = arguments,
            ExecutionOrder = order,
            IsEnabled = isEnabled
        };
    }
}
