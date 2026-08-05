using System.Text.Json;
using System.Xml.Linq;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class DependencyAnalysisService : IDependencyAnalysisService
{
    public async Task AnalyzeAsync(Project project, IReadOnlyCollection<Finding> findings, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(project.Path))
        {
            return;
        }

        var directPackages = await LoadDirectPackagesAsync(project, cancellationToken);
        var runtimeAlert = await RuntimeSupportService.TryBuildAlertAsync(project.Path, project.Technology, cancellationToken);

        foreach (var finding in findings.Where(finding => finding.FindingType == FindingType.Vulnerability))
        {
            if (!string.IsNullOrWhiteSpace(runtimeAlert))
            {
                finding.RuntimeSupportAlert = runtimeAlert;
            }

            if (string.IsNullOrWhiteSpace(finding.Ecosystem))
            {
                finding.Ecosystem = project.PackageManager switch
                {
                    PackageManagerType.DotNetCli => "NuGet",
                    PackageManagerType.Npm or PackageManagerType.Pnpm or PackageManagerType.Yarn => "npm",
                    _ => null
                };
            }

            if (string.IsNullOrWhiteSpace(finding.PackageName))
            {
                finding.DependencyRelation = DependencyRelation.Unknown;
                continue;
            }

            if (directPackages.TryGetValue(finding.PackageName, out var direct))
            {
                finding.DependencyRelation = DependencyRelation.Direct;
                ApplyProjectFileToOccurrences(project.Path, finding, direct.ProjectFilePath);
            }
            else
            {
                finding.DependencyRelation = directPackages.Count == 0
                    ? DependencyRelation.Unknown
                    : DependencyRelation.Transitive;
            }
        }
    }

    private static async Task<Dictionary<string, DirectDependency>> LoadDirectPackagesAsync(Project project, CancellationToken cancellationToken)
    {
        return project.PackageManager switch
        {
            PackageManagerType.DotNetCli => await LoadDotNetPackagesAsync(project.Path, cancellationToken),
            PackageManagerType.Npm or PackageManagerType.Pnpm or PackageManagerType.Yarn => await LoadNodePackagesAsync(project.Path, cancellationToken),
            _ => []
        };
    }

    private static async Task<Dictionary<string, DirectDependency>> LoadDotNetPackagesAsync(string projectPath, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DirectDependency>(StringComparer.OrdinalIgnoreCase);
        foreach (var csproj in Directory.EnumerateFiles(projectPath, "*.csproj", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            XDocument document;
            try
            {
                await using var stream = File.OpenRead(csproj);
                document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            }
            catch
            {
                continue;
            }

            foreach (var package in document.Descendants().Where(element => element.Name.LocalName == "PackageReference"))
            {
                var name = package.Attribute("Include")?.Value ?? package.Attribute("Update")?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result.TryAdd(name.Trim(), new DirectDependency(name.Trim(), csproj));
                }
            }
        }

        var centralProps = Path.Combine(projectPath, "Directory.Packages.props");
        if (File.Exists(centralProps))
        {
            try
            {
                await using var stream = File.OpenRead(centralProps);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                foreach (var package in document.Descendants().Where(element => element.Name.LocalName == "PackageVersion"))
                {
                    var name = package.Attribute("Include")?.Value;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result.TryAdd(name.Trim(), new DirectDependency(name.Trim(), centralProps));
                    }
                }
            }
            catch
            {
                // Ignore malformed project metadata; findings remain usable.
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, DirectDependency>> LoadNodePackagesAsync(string projectPath, CancellationToken cancellationToken)
    {
        var packageJson = Path.Combine(projectPath, "package.json");
        if (!File.Exists(packageJson))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(packageJson);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var result = new Dictionary<string, DirectDependency>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in new[] { "dependencies", "devDependencies", "optionalDependencies", "peerDependencies" })
            {
                if (document.RootElement.TryGetProperty(section, out var dependencies) && dependencies.ValueKind == JsonValueKind.Object)
                {
                    foreach (var package in dependencies.EnumerateObject())
                    {
                        result.TryAdd(package.Name, new DirectDependency(package.Name, packageJson));
                    }
                }
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static void ApplyProjectFileToOccurrences(string projectPath, Finding finding, string projectFilePath)
    {
        foreach (var occurrence in finding.Occurrences)
        {
            occurrence.ProjectFilePath ??= projectFilePath;
            occurrence.AbsolutePath ??= ResolveAbsolutePath(projectPath, occurrence.FilePath ?? occurrence.Target);
            occurrence.RelativePath ??= BuildRelativePath(projectPath, occurrence.AbsolutePath, occurrence.FilePath ?? occurrence.Target);
            occurrence.ProjectName = InferProjectName(occurrence.ProjectName, occurrence.Target, projectFilePath);
        }
    }

    private static string? ResolveAbsolutePath(string projectPath, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(projectPath, path));
    }

    private static string? BuildRelativePath(string projectPath, string? absolutePath, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(absolutePath) && Path.IsPathRooted(absolutePath))
        {
            return Path.GetRelativePath(projectPath, absolutePath);
        }

        return fallback;
    }

    private static string? InferProjectName(string? current, string? target, string projectFilePath)
    {
        if (!string.IsNullOrWhiteSpace(current) && !current.Equals(target, StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        if (!string.IsNullOrWhiteSpace(target))
        {
            var fileName = Path.GetFileName(target);
            if (fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^".deps.json".Length];
            }
        }

        return Path.GetFileNameWithoutExtension(projectFilePath);
    }

    private sealed record DirectDependency(string PackageName, string ProjectFilePath);
}
