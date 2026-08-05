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
}
