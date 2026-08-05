using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.UnitTests;

public sealed class ProjectDetectionTests
{
    [Fact]
    public async Task DetectsDotNetProject()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "App.csproj"), "<Project />");

        var result = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Contains(ProjectTechnology.DotNet, result.Technologies);
        Assert.Equal(PackageManagerType.DotNetCli, result.SuggestedPackageManager);
    }

    [Fact]
    public async Task DetectsNpmProject()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{}");
        File.WriteAllText(Path.Combine(directory.Path, "package-lock.json"), "{}");

        var result = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Contains(ProjectTechnology.Node, result.Technologies);
        Assert.Contains(PackageManagerType.Npm, result.PackageManagers);
    }

    [Fact]
    public async Task NpmProjectWithoutLockFileUsesInstallBeforeBuild()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{\"scripts\":{\"build\":\"vite build\"}}");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);
        var commands = new CommandProfileService().CreateAutomaticCommands(detection);

        Assert.Collection(
            commands,
            install =>
            {
                Assert.Equal("npm", install.Command);
                Assert.Equal("install", install.Arguments);
            },
            build =>
            {
                Assert.Equal("npm", build.Command);
                Assert.Equal("run build", build.Arguments);
            });
    }

    [Fact]
    public async Task DetectsPnpmProject()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "pnpm-lock.yaml"), "");

        var result = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Equal(PackageManagerType.Pnpm, result.SuggestedPackageManager);
    }

    [Fact]
    public async Task DetectsYarnProject()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "yarn.lock"), "");

        var result = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Equal(PackageManagerType.Yarn, result.SuggestedPackageManager);
    }

    [Fact]
    public async Task DetectsMixedRepositoryAndUsesExplicitDotNetTarget()
    {
        using var directory = TempDirectory.Create();
        var backend = Directory.CreateDirectory(Path.Combine(directory.Path, "backend"));
        var projectPath = Path.Combine(backend.FullName, "Api.csproj");
        File.WriteAllText(projectPath, "<Project />");
        File.WriteAllText(Path.Combine(backend.FullName, "App.sln"), "Project(\"x\") = \"Api\", \"Api.csproj\", \"y\"");
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{\"scripts\":{\"build\":\"vite build\"}}");
        File.WriteAllText(Path.Combine(directory.Path, "package-lock.json"), "{}");
        File.WriteAllText(Path.Combine(directory.Path, "pom.xml"), "<project />");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);
        var commands = new CommandProfileService().CreateAutomaticCommands(detection);

        Assert.Equal(ProjectTechnology.Mixed, detection.SuggestedTechnology);
        Assert.Contains(detection.Targets, target => target.PackageManager == PackageManagerType.DotNetCli && target.ManifestPath.EndsWith("App.sln"));
        Assert.DoesNotContain(detection.Targets, target => target.ManifestPath.EndsWith("Api.csproj"));
        Assert.Contains(detection.Targets, target => target.PackageManager == PackageManagerType.Npm);
        Assert.Contains(detection.Targets, target => target.PackageManager == PackageManagerType.Maven);
        var restore = Assert.Single(commands, command => command.Name.StartsWith("Restore", StringComparison.Ordinal));
        Assert.Equal(backend.FullName, restore.WorkingDirectory);
        Assert.Equal("restore \"App.sln\"", restore.Arguments);
    }

    [Fact]
    public async Task PrefersSlnWhenMatchingSlnAndSlnxCoexist()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "App.sln"), string.Empty);
        File.WriteAllText(Path.Combine(directory.Path, "App.slnx"), "<Solution />");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);

        var target = Assert.Single(detection.Targets);
        Assert.EndsWith("App.sln", target.ManifestPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectsWorkspaceOnceAndBuildsPackagesWhenRootHasNoBuild()
    {
        using var directory = TempDirectory.Create();
        var package = Directory.CreateDirectory(Path.Combine(directory.Path, "packages", "web"));
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{\"workspaces\":[\"packages/*\"]}");
        File.WriteAllText(Path.Combine(directory.Path, "package-lock.json"), "{}");
        File.WriteAllText(Path.Combine(package.FullName, "package.json"), "{\"scripts\":{\"build\":\"tsc\"}}");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);
        var target = Assert.Single(detection.Targets);
        var commands = new CommandProfileService().CreateAutomaticCommands(detection);

        Assert.Equal(PackageManagerType.Npm, target.PackageManager);
        Assert.Equal(package.FullName, Assert.Single(target.BuildDirectories));
        Assert.Equal("ci", commands[0].Arguments);
        Assert.Equal(package.FullName, commands[1].WorkingDirectory);
    }

    [Fact]
    public async Task DetectsPnpmWorkspaceFromWorkspaceFile()
    {
        using var directory = TempDirectory.Create();
        var package = Directory.CreateDirectory(Path.Combine(directory.Path, "apps", "web"));
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{\"packageManager\":\"pnpm@10.0.0\"}");
        File.WriteAllText(Path.Combine(directory.Path, "pnpm-lock.yaml"), "");
        File.WriteAllText(Path.Combine(directory.Path, "pnpm-workspace.yaml"), "packages:\n  - 'apps/*'\n");
        File.WriteAllText(Path.Combine(package.FullName, "package.json"), "{\"scripts\":{\"build\":\"vite build\"}}");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);

        var target = Assert.Single(detection.Targets);
        Assert.Equal(PackageManagerType.Pnpm, target.PackageManager);
        Assert.Equal(package.FullName, Assert.Single(target.BuildDirectories));
    }

    [Fact]
    public async Task ConflictingNodeLockFilesRequireExplicitPackageManager()
    {
        using var directory = TempDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "package.json"), "{}");
        File.WriteAllText(Path.Combine(directory.Path, "package-lock.json"), "{}");
        File.WriteAllText(Path.Combine(directory.Path, "yarn.lock"), "");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Empty(detection.Targets);
        Assert.Single(detection.Warnings);
    }

    [Fact]
    public async Task DetectsMavenAggregatorAndGradleRootWithoutModulesOrGeneratedFolders()
    {
        using var directory = TempDirectory.Create();
        var mavenModule = Directory.CreateDirectory(Path.Combine(directory.Path, "maven", "module"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "maven"));
        File.WriteAllText(Path.Combine(directory.Path, "maven", "pom.xml"), "<project><modules><module>module</module></modules></project>");
        File.WriteAllText(Path.Combine(mavenModule.FullName, "pom.xml"), "<project />");
        var gradle = Directory.CreateDirectory(Path.Combine(directory.Path, "gradle-app"));
        File.WriteAllText(Path.Combine(gradle.FullName, "settings.gradle.kts"), "rootProject.name = \"app\"");
        File.WriteAllText(Path.Combine(gradle.FullName, "build.gradle.kts"), "plugins { java }");
        var ignored = Directory.CreateDirectory(Path.Combine(directory.Path, "target", "ignored"));
        File.WriteAllText(Path.Combine(ignored.FullName, "pom.xml"), "<project />");

        var detection = await new ProjectDetectionService().DetectAsync(directory.Path);

        Assert.Single(detection.Targets, target => target.PackageManager == PackageManagerType.Maven);
        Assert.Single(detection.Targets, target => target.PackageManager == PackageManagerType.Gradle);
        Assert.DoesNotContain(detection.Targets, target => target.ManifestPath.Contains("target", StringComparison.OrdinalIgnoreCase));
    }
}
