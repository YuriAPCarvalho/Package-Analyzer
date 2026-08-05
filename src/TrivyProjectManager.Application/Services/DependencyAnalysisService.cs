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
        var result = new Dictionary<string, DirectDependency>(StringComparer.OrdinalIgnoreCase);
        foreach (var packages in new[]
        {
            await LoadDotNetPackagesAsync(project.Path, cancellationToken),
            await LoadNodePackagesAsync(project.Path, cancellationToken),
            await LoadMavenPackagesAsync(project.Path, cancellationToken)
        })
        {
            foreach (var package in packages)
            {
                result.TryAdd(package.Key, package.Value);
            }
        }

        return result;
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
        var result = new Dictionary<string, DirectDependency>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageJson in Directory.EnumerateFiles(projectPath, "package.json", SearchOption.AllDirectories)
                     .Where(path => !ContainsExcludedDirectory(projectPath, path)))
        {
            try
            {
                await using var stream = File.OpenRead(packageJson);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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
            }
            catch
            {
                // Ignore malformed manifests; findings remain usable.
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, DirectDependency>> LoadMavenPackagesAsync(string projectPath, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DirectDependency>(StringComparer.OrdinalIgnoreCase);
        foreach (var pom in Directory.EnumerateFiles(projectPath, "pom.xml", SearchOption.AllDirectories)
                     .Where(path => !ContainsExcludedDirectory(projectPath, path)))
        {
            try
            {
                await using var stream = File.OpenRead(pom);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                foreach (var dependency in document.Descendants().Where(element => element.Name.LocalName == "dependency"))
                {
                    var group = dependency.Elements().FirstOrDefault(element => element.Name.LocalName == "groupId")?.Value.Trim();
                    var artifact = dependency.Elements().FirstOrDefault(element => element.Name.LocalName == "artifactId")?.Value.Trim();
                    if (string.IsNullOrWhiteSpace(artifact))
                    {
                        continue;
                    }

                    result.TryAdd(artifact, new DirectDependency(artifact, pom));
                    if (!string.IsNullOrWhiteSpace(group))
                    {
                        var coordinate = $"{group}:{artifact}";
                        result.TryAdd(coordinate, new DirectDependency(coordinate, pom));
                    }
                }
            }
            catch
            {
                // Ignore malformed POMs; findings remain usable.
            }
        }

        return result;
    }

    private static bool ContainsExcludedDirectory(string projectPath, string path)
    {
        var relative = Path.GetRelativePath(projectPath, path);
        var segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "node_modules" or ".git" or "bin" or "obj" or "target" or "build" or ".gradle");
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
