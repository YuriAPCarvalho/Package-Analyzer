using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.UnitTests;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunsProjectLocalWindowsBatchWrapperWithSpacedArgument()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = TempDirectory.Create();
        var wrapper = Path.Combine(directory.Path, "wrapper.cmd");
        await File.WriteAllTextAsync(wrapper, "@echo off\r\necho [%~1]\r\n");

        var result = await new ProcessRunner().RunAsync(new ProcessRequest("wrapper.cmd", ["hello world"], directory.Path, TimeSpan.FromSeconds(10)));

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("[hello world]", result.StandardOutput);
    }

    [Theory]
    [InlineData("npm")]
    [InlineData("pnpm")]
    [InlineData("yarn")]
    public async Task PrefersWindowsCommandShimOverExtensionlessScript(string commandName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = TempDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, commandName), "#!/usr/bin/env node\n");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, $"{commandName}.cmd"), $"@echo off\r\necho {commandName}-command-shim\r\n");

        var result = await new ProcessRunner().RunAsync(new ProcessRequest(commandName, [], directory.Path, TimeSpan.FromSeconds(10)));

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains($"{commandName}-command-shim", result.StandardOutput);
    }

    [Fact]
    public async Task DecodesUtf8AndRemovesAnsiSequencesFromBothStreams()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = TempDirectory.Create();
        var command = "[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);" +
            "[Console]::WriteLine([char]27 + '[34mRepositório atualizado[0m');" +
            "[Console]::Error.WriteLine([char]27 + '[33mAtenção necessária[0m')";

        var result = await new ProcessRunner().RunAsync(new ProcessRequest(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-Command", command],
            directory.Path,
            TimeSpan.FromSeconds(10)));

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("Repositório atualizado", result.StandardOutput);
        Assert.Contains("Atenção necessária", result.StandardError);
        Assert.DoesNotContain('\u001b', result.StandardOutput);
        Assert.DoesNotContain('\u001b', result.StandardError);
    }
}
