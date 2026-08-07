using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class TrivyService(IProcessRunner processRunner, IStoragePathService storagePathService) : ITrivyService
{
    public string? LocateExecutable(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var managedPath = storagePathService.GetManagedTrivyExecutablePath();
        if (File.Exists(managedPath))
        {
            return managedPath;
        }

        return PathEnvironment.FindExecutable("trivy");
    }

    public async Task<bool> IsInstalledAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
    {
        return await GetVersionAsync(configuredPath, cancellationToken) is not null;
    }

    public async Task<string?> GetVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
    {
        var executable = LocateExecutable(configuredPath);
        if (executable is null)
        {
            return null;
        }

        var result = await processRunner.RunAsync(new ProcessRequest(executable, ["--version"], Environment.CurrentDirectory, TimeSpan.FromSeconds(20)), cancellationToken: cancellationToken);
        var firstLine = result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return result.Succeeded ? firstLine : null;
    }

    public Task<ProcessResult> ScanFileSystemAsync(string projectPath, string outputJsonPath, TrivyOptions options, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
    {
        var executable = LocateExecutable(options.TrivyPath)
            ?? throw new FileNotFoundException("Trivy executable was not found. Configure the trivy.exe path in settings.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)!);
        var arguments = new List<string>
        {
            "fs",
            projectPath,
            "--scanners",
            options.Scanners,
            "--severity",
            options.Severities,
            "--format",
            "json",
            "--output",
            outputJsonPath
        };

        if (options.IgnoreUnfixed)
        {
            arguments.Add("--ignore-unfixed");
        }

        foreach (var skipDirectory in options.SkipDirectories.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            arguments.Add("--skip-dirs");
            arguments.Add(skipDirectory);
        }

        foreach (var filePattern in options.FilePatterns.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            arguments.Add("--file-patterns");
            arguments.Add(filePattern);
        }

        return processRunner.RunAsync(new ProcessRequest(executable, arguments, projectPath, options.Timeout), progress, cancellationToken);
    }
}
