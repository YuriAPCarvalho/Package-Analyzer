using System.Diagnostics;
using System.Text;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(request.WorkingDirectory))
        {
            throw new DirectoryNotFoundException($"Working directory was not found: {request.WorkingDirectory}");
        }

        var executable = PathEnvironment.FindExecutable(request.FileName, request.WorkingDirectory)
            ?? throw new FileNotFoundException($"Executable was not found: {request.FileName}", request.FileName);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var startedAt = DateTimeOffset.UtcNow;

        using var timeoutCts = request.Timeout.HasValue ? new CancellationTokenSource(request.Timeout.Value) : null;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts?.Token ?? CancellationToken.None);

        var startInfo = BuildStartInfo(executable, request);
        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) => Append(stdout, "stdout", args.Data, progress);
        process.ErrorDataReceived += (_, args) => Append(stderr, "stderr", args.Data, progress);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(linkedCts.Token);
            var finishedAt = DateTimeOffset.UtcNow;
            var status = process.ExitCode == 0 ? CommandExecutionStatus.Succeeded : CommandExecutionStatus.Failed;
            return new ProcessResult(request.FileName, request.Arguments, startedAt, finishedAt, process.ExitCode, status, stdout.ToString(), stderr.ToString());
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
        {
            KillProcess(process);
            return new ProcessResult(request.FileName, request.Arguments, startedAt, DateTimeOffset.UtcNow, -1, CommandExecutionStatus.TimedOut, stdout.ToString(), stderr.ToString());
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            return new ProcessResult(request.FileName, request.Arguments, startedAt, DateTimeOffset.UtcNow, -1, CommandExecutionStatus.Cancelled, stdout.ToString(), stderr.ToString());
        }
    }

    private static ProcessStartInfo BuildStartInfo(string executable, ProcessRequest request)
    {
        var isBatch = OperatingSystem.IsWindows()
            && Path.GetExtension(executable) is { } extension
            && (extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase));
        var startInfo = new ProcessStartInfo
        {
            FileName = isBatch ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe" : executable,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (isBatch)
        {
            var tokens = new[] { executable }.Concat(request.Arguments).Select(QuoteBatchToken);
            startInfo.Arguments = $"/d /v:off /s /c \"call {string.Join(' ', tokens)}\"";
        }
        else
        {
            foreach (var argument in request.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }
        }

        return startInfo;
    }

    private static string QuoteBatchToken(string value)
    {
        if (value.Contains('\r') || value.Contains('\n') || value.Contains('\0'))
        {
            throw new InvalidOperationException("Batch command arguments cannot contain control characters.");
        }

        return $"\"{value.Replace("%", "%%", StringComparison.Ordinal).Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void Append(StringBuilder builder, string stream, string? message, IProgress<ProcessLogLine>? progress)
    {
        if (message is null)
        {
            return;
        }

        builder.AppendLine(message);
        progress?.Report(new ProcessLogLine(DateTimeOffset.UtcNow, stream, message));
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
