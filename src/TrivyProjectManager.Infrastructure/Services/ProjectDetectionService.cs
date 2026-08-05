using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class ProjectDetectionService : IProjectDetectionService
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg", ".idea", ".vs", "node_modules", "bin", "obj", "target", "build", ".gradle", "dist", "out"
    };

    public async Task<ProjectDetectionResult> DetectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Project folder was not found: {projectPath}");
        }

        var root = Path.GetFullPath(projectPath);
        var files = EnumerateFiles(root, cancellationToken).ToList();
        var warnings = new List<string>();
        var targets = new List<DetectedProjectTarget>();

        AddDotNetTargets(root, files, targets);
        await AddNodeTargetsAsync(root, files, targets, warnings, cancellationToken);
        await AddMavenTargetsAsync(root, files, targets, cancellationToken);
        AddGradleTargets(root, files, targets);

        targets = targets
            .OrderBy(target => target.Technology)
            .ThenBy(target => target.RootPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => target.ManifestPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var technologies = targets.Select(target => target.Technology).Distinct().ToList();
        var managers = targets.Select(target => target.PackageManager).Distinct().ToList();
        var suggestedTechnology = technologies.Count switch
        {
            0 => ProjectTechnology.Unknown,
            1 => technologies[0],
            _ => ProjectTechnology.Mixed
        };
        var suggestedManager = managers.Count switch
        {
            0 => PackageManagerType.Unknown,
            1 => managers[0],
            _ => PackageManagerType.Multiple
        };

        return new ProjectDetectionResult(root, technologies, managers, suggestedTechnology, suggestedManager, targets, warnings);
    }

    private static IEnumerable<string> EnumerateFiles(string root, CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            IEnumerable<string> files;
            IEnumerable<string> directories;
            try
            {
                files = Directory.EnumerateFiles(directory).ToList();
                directories = Directory.EnumerateDirectories(directory).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var child in directories)
            {
                var info = new DirectoryInfo(child);
                if (!ExcludedDirectories.Contains(info.Name) && !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    pending.Push(child);
                }
            }
        }
    }

    private static void AddDotNetTargets(string root, IReadOnlyCollection<string> files, ICollection<DetectedProjectTarget> targets)
    {
        var solutions = files.Where(IsSolutionFile)
            .GroupBy(path => Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path)), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(path => Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First())
            .ToList();
        var projects = files.Where(IsDotNetProjectFile).ToList();
        var coveredProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var solution in solutions)
        {
            foreach (var project in ReadSolutionProjects(solution))
            {
                coveredProjects.Add(project);
            }

            targets.Add(Target(root, ProjectTechnology.DotNet, PackageManagerType.DotNetCli, solution, ["dotnet"]));
        }

        foreach (var project in projects.Where(project => !coveredProjects.Contains(Path.GetFullPath(project))))
        {
            targets.Add(Target(root, ProjectTechnology.DotNet, PackageManagerType.DotNetCli, project, ["dotnet"]));
        }
    }

    private static bool IsSolutionFile(string path) => Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".slnx", StringComparison.OrdinalIgnoreCase);

    private static bool IsDotNetProjectFile(string path) => Path.GetExtension(path) is { } extension
        && (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> ReadSolutionProjects(string solutionPath)
    {
        string content;
        try
        {
            content = File.ReadAllText(solutionPath);
        }
        catch
        {
            yield break;
        }

        var directory = Path.GetDirectoryName(solutionPath)!;
        foreach (Match match in Regex.Matches(content, @"(?<path>[^\""'<>|]+\.(?:csproj|fsproj|vbproj))", RegexOptions.IgnoreCase))
        {
            var relative = match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).Trim();
            string resolved;
            try
            {
                resolved = Path.GetFullPath(Path.Combine(directory, relative));
            }
            catch
            {
                continue;
            }

            if (File.Exists(resolved))
            {
                yield return resolved;
            }
        }
    }

    private static async Task AddNodeTargetsAsync(string root, IReadOnlyCollection<string> files, ICollection<DetectedProjectTarget> targets, ICollection<string> warnings, CancellationToken cancellationToken)
    {
        var packages = files.Where(path => Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path.Count(character => character == Path.DirectorySeparatorChar))
            .ToList();
        var workspaceRoots = new List<(string Directory, IReadOnlyList<string> Patterns)>();

        foreach (var packageJson in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(packageJson)!;
            if (workspaceRoots.Any(workspace => IsWorkspaceMember(workspace.Directory, directory, workspace.Patterns)))
            {
                continue;
            }

            var metadata = await ReadPackageJsonAsync(packageJson, cancellationToken);
            var manager = DetectNodeManager(directory, metadata.PackageManager, warnings);
            if (manager is null)
            {
                continue;
            }

            if (manager == PackageManagerType.Pnpm && metadata.Workspaces.Count == 0)
            {
                metadata = metadata with { Workspaces = ReadPnpmWorkspaces(Path.Combine(directory, "pnpm-workspace.yaml")) };
            }

            var workspacePackages = packages
                .Where(candidate => !candidate.Equals(packageJson, StringComparison.OrdinalIgnoreCase)
                    && IsWorkspaceMember(directory, Path.GetDirectoryName(candidate)!, metadata.Workspaces))
                .ToList();
            var buildDirectories = new List<string>();
            if (metadata.HasBuild)
            {
                buildDirectories.Add(directory);
            }
            else
            {
                foreach (var workspacePackage in workspacePackages)
                {
                    var workspaceMetadata = await ReadPackageJsonAsync(workspacePackage, cancellationToken);
                    if (workspaceMetadata.HasBuild)
                    {
                        buildDirectories.Add(Path.GetDirectoryName(workspacePackage)!);
                    }
                }
            }

            if (metadata.Workspaces.Count > 0)
            {
                workspaceRoots.Add((directory, metadata.Workspaces));
            }

            targets.Add(Target(root, ProjectTechnology.Node, manager.Value, packageJson, [ManagerExecutable(manager.Value)], buildDirectories));
        }

        var detectedDirectories = targets.Where(target => target.Technology == ProjectTechnology.Node)
            .Select(target => target.RootPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packageDirectories = packages.Select(path => Path.GetDirectoryName(path)!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var lockFile in files.Where(IsNodeLockFile))
        {
            var directory = Path.GetDirectoryName(lockFile)!;
            if (detectedDirectories.Contains(directory) || packageDirectories.Contains(directory))
            {
                continue;
            }

            var manager = Path.GetFileName(lockFile).Equals("pnpm-lock.yaml", StringComparison.OrdinalIgnoreCase) ? PackageManagerType.Pnpm
                : Path.GetFileName(lockFile).Equals("yarn.lock", StringComparison.OrdinalIgnoreCase) ? PackageManagerType.Yarn
                : PackageManagerType.Npm;
            targets.Add(Target(root, ProjectTechnology.Node, manager, lockFile, [ManagerExecutable(manager)], []));
            detectedDirectories.Add(directory);
        }
    }

    private static bool IsNodeLockFile(string path) => Path.GetFileName(path) is "package-lock.json" or "npm-shrinkwrap.json" or "pnpm-lock.yaml" or "yarn.lock";

    private static PackageManagerType? DetectNodeManager(string directory, string? declaredManager, ICollection<string> warnings)
    {
        var declared = declaredManager?.Split('@', 2)[0].Trim().ToLowerInvariant() switch
        {
            "npm" => PackageManagerType.Npm,
            "pnpm" => PackageManagerType.Pnpm,
            "yarn" => PackageManagerType.Yarn,
            _ => (PackageManagerType?)null
        };
        if (declared.HasValue)
        {
            return declared.Value;
        }

        var detected = new List<PackageManagerType>();
        if (File.Exists(Path.Combine(directory, "pnpm-lock.yaml"))) detected.Add(PackageManagerType.Pnpm);
        if (File.Exists(Path.Combine(directory, "yarn.lock"))) detected.Add(PackageManagerType.Yarn);
        if (File.Exists(Path.Combine(directory, "package-lock.json")) || File.Exists(Path.Combine(directory, "npm-shrinkwrap.json"))) detected.Add(PackageManagerType.Npm);
        if (detected.Count > 1)
        {
            warnings.Add($"Vários lockfiles foram encontrados em '{directory}'. Defina o campo packageManager no package.json para habilitar a preparação automática desse alvo.");
            return null;
        }

        return detected.FirstOrDefault(PackageManagerType.Npm);
    }

    private static async Task<PackageMetadata> ReadPackageJsonAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var packageManager = root.TryGetProperty("packageManager", out var manager) && manager.ValueKind == JsonValueKind.String ? manager.GetString() : null;
            var hasBuild = root.TryGetProperty("scripts", out var scripts) && scripts.ValueKind == JsonValueKind.Object && scripts.TryGetProperty("build", out _);
            var workspaces = ReadWorkspaces(root);
            return new PackageMetadata(packageManager, hasBuild, workspaces);
        }
        catch
        {
            return new PackageMetadata(null, false, []);
        }
    }

    private static IReadOnlyList<string> ReadWorkspaces(JsonElement root)
    {
        if (!root.TryGetProperty("workspaces", out var workspaces)) return [];
        if (workspaces.ValueKind == JsonValueKind.Object && workspaces.TryGetProperty("packages", out var packages)) workspaces = packages;
        return workspaces.ValueKind == JsonValueKind.Array
            ? workspaces.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).ToList()
            : [];
    }

    private static IReadOnlyList<string> ReadPnpmWorkspaces(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return File.ReadLines(path)
                .Select(line => Regex.Match(line, @"^\s*-\s*['\""']?(?<pattern>[^'\""#]+)['\""']?\s*(?:#.*)?$"))
                .Where(match => match.Success)
                .Select(match => match.Groups["pattern"].Value.Trim())
                .Where(pattern => pattern.Length > 0 && !pattern.StartsWith('!'))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsWorkspaceMember(string root, string candidate, IReadOnlyList<string> patterns)
    {
        if (patterns.Count == 0 || root.Equals(candidate, StringComparison.OrdinalIgnoreCase)) return false;
        var relative = Path.GetRelativePath(root, candidate).Replace('\\', '/');
        if (relative.StartsWith("../", StringComparison.Ordinal) || relative == "..") return false;
        return patterns.Any(pattern => GlobMatches(pattern.TrimEnd('/'), relative));
    }

    private static bool GlobMatches(string pattern, string value)
    {
        var regex = "^" + Regex.Escape(pattern.Replace('\\', '/')).Replace("\\*\\*", ".*").Replace("\\*", "[^/]*") + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase);
    }

    private static async Task AddMavenTargetsAsync(string root, IReadOnlyCollection<string> files, ICollection<DetectedProjectTarget> targets, CancellationToken cancellationToken)
    {
        var poms = files.Where(path => Path.GetFileName(path).Equals("pom.xml", StringComparison.OrdinalIgnoreCase)).ToList();
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var aggregators = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pom in poms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(pom);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                var moduleNames = document.Descendants().Where(element => element.Name.LocalName == "module").Select(element => element.Value.Trim()).Where(value => value.Length > 0).ToList();
                if (moduleNames.Count > 0) aggregators.Add(pom);
                foreach (var module in moduleNames)
                {
                    modules.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(pom)!, module, "pom.xml")));
                }
            }
            catch
            {
                // Malformed POMs remain standalone targets so the build can report the real error.
            }
        }

        foreach (var pom in poms.Where(pom => !modules.Contains(pom) || aggregators.Contains(pom) && !IsNestedModuleAggregator(pom, modules, aggregators)))
        {
            var directory = Path.GetDirectoryName(pom)!;
            var wrapper = OperatingSystem.IsWindows() ? "mvnw.cmd" : "mvnw";
            var executable = File.Exists(Path.Combine(directory, wrapper)) ? wrapper : "mvn";
            targets.Add(Target(root, ProjectTechnology.Java, PackageManagerType.Maven, pom, ["java", executable]));
        }
    }

    private static bool IsNestedModuleAggregator(string pom, IReadOnlySet<string> modules, IReadOnlySet<string> aggregators) => modules.Contains(pom) && aggregators.Contains(pom);

    private static void AddGradleTargets(string root, IReadOnlyCollection<string> files, ICollection<DetectedProjectTarget> targets)
    {
        var settings = files.Where(path => Path.GetFileName(path) is "settings.gradle" or "settings.gradle.kts").ToList();
        var rootDirectories = settings.Select(Path.GetDirectoryName).Where(path => path is not null).Cast<string>()
            .Where(candidate => !settings.Select(Path.GetDirectoryName).Where(parent => parent is not null && !parent.Equals(candidate, StringComparison.OrdinalIgnoreCase)).Any(parent => IsBelow(candidate, parent!)))
            .ToList();
        var buildFiles = files.Where(path => Path.GetFileName(path) is "build.gradle" or "build.gradle.kts").ToList();
        foreach (var directory in rootDirectories)
        {
            var manifest = settings.First(path => Path.GetDirectoryName(path)!.Equals(directory, StringComparison.OrdinalIgnoreCase));
            AddGradleTarget(root, directory, manifest, targets);
        }

        foreach (var buildFile in buildFiles.Where(path => !rootDirectories.Any(directory =>
                     Path.GetDirectoryName(path)!.Equals(directory, StringComparison.OrdinalIgnoreCase)
                     || IsBelow(Path.GetDirectoryName(path)!, directory))))
        {
            AddGradleTarget(root, Path.GetDirectoryName(buildFile)!, buildFile, targets);
        }
    }

    private static void AddGradleTarget(string root, string directory, string manifest, ICollection<DetectedProjectTarget> targets)
    {
        var wrapper = OperatingSystem.IsWindows() ? "gradlew.bat" : "gradlew";
        var executable = File.Exists(Path.Combine(directory, wrapper)) ? wrapper : "gradle";
        targets.Add(Target(root, ProjectTechnology.Java, PackageManagerType.Gradle, manifest, ["java", executable]));
    }

    private static bool IsBelow(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return relative != "." && relative != ".." && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static DetectedProjectTarget Target(string projectRoot, ProjectTechnology technology, PackageManagerType manager, string manifest, IReadOnlyList<string> requiredExecutables, IReadOnlyList<string>? buildDirectories = null)
    {
        var directory = Path.GetDirectoryName(manifest)!;
        var relative = Path.GetRelativePath(projectRoot, manifest).Replace('\\', '/');
        return new DetectedProjectTarget($"{manager}:{relative}", technology, manager, directory, manifest, requiredExecutables, buildDirectories ?? [directory]);
    }

    private static string ManagerExecutable(PackageManagerType manager) => manager switch
    {
        PackageManagerType.Pnpm => "pnpm",
        PackageManagerType.Yarn => "yarn",
        _ => "npm"
    };

    private sealed record PackageMetadata(string? PackageManager, bool HasBuild, IReadOnlyList<string> Workspaces);
}
