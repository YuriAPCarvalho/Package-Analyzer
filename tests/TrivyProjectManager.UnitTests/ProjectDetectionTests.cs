using TrivyProjectManager.Domain.Enums;
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
}
