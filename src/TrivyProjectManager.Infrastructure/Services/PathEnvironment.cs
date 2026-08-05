namespace TrivyProjectManager.Infrastructure.Services;

internal static class PathEnvironment
{
    public static string? FindExecutable(string fileName, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var pathExt = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];
        var candidates = BuildCandidates(fileName, pathExt);

        if (Path.IsPathRooted(fileName))
        {
            return candidates.FirstOrDefault(File.Exists);
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            foreach (var candidate in candidates)
            {
                var localPath = Path.GetFullPath(Path.Combine(workingDirectory, candidate));
                if (File.Exists(localPath))
                {
                    return localPath;
                }
            }
        }

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(directory.Trim(), candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> BuildCandidates(string fileName, IReadOnlyCollection<string> pathExt)
    {
        if (!OperatingSystem.IsWindows() || Path.GetExtension(fileName).Length > 0)
        {
            return [fileName];
        }

        return pathExt
            .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
            .Select(extension => fileName + extension.ToLowerInvariant())
            .Append(fileName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
