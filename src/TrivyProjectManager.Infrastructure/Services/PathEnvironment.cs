namespace TrivyProjectManager.Infrastructure.Services;

internal static class PathEnvironment
{
    public static string? FindExecutable(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (Path.IsPathRooted(fileName))
        {
            return File.Exists(fileName) ? fileName : null;
        }

        var pathExt = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];

        var candidates = new List<string> { fileName };
        if (OperatingSystem.IsWindows() && Path.GetExtension(fileName).Length == 0)
        {
            candidates.AddRange(pathExt.Select(ext => fileName + ext.ToLowerInvariant()));
            candidates.AddRange(pathExt.Select(ext => fileName + ext.ToUpperInvariant()));
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
}
