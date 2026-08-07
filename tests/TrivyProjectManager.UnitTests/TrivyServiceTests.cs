using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.UnitTests;

public sealed class TrivyServiceTests
{
    [Fact]
    public async Task FileSystemScanAddsDockerfilePatternAndKeepsExistingOptions()
    {
        using var directory = TempDirectory.Create();
        var executable = Path.Combine(directory.Path, "trivy.exe");
        File.WriteAllText(executable, string.Empty);
        var runner = new CapturingProcessRunner();
        var service = new TrivyService(runner, new FakeStoragePathService(executable));
        var reportPath = Path.Combine(directory.Path, "reports", "scan.json");

        await service.ScanFileSystemAsync(directory.Path, reportPath, new TrivyOptions
        {
            TrivyPath = executable,
            Scanners = "vuln,misconfig,secret",
            Severities = "HIGH,CRITICAL",
            SkipDirectories = [".git", "node_modules"]
        });

        var request = Assert.IsType<ProcessRequest>(runner.Request);
        Assert.Equal("fs", request.Arguments[0]);
        AssertOption(request.Arguments, "--scanners", "vuln,misconfig,secret");
        AssertOption(request.Arguments, "--severity", "HIGH,CRITICAL");
        AssertOption(request.Arguments, "--file-patterns", TrivyOptions.DockerfilePattern);
        Assert.Equal(2, request.Arguments.Count(argument => argument == "--skip-dirs"));
    }

    [Theory]
    [InlineData("Dockerfile")]
    [InlineData("Dockerfile-hom")]
    [InlineData("Dockerfile.dev")]
    [InlineData("api-Dockerfile")]
    [InlineData("API.dOcKeRfIlE.production")]
    public void DockerfilePatternCoversNamesWithPrefixesSuffixesAndDifferentCase(string fileName)
    {
        var expression = TrivyOptions.DockerfilePattern[(TrivyOptions.DockerfilePattern.IndexOf(':') + 1)..];

        Assert.Matches(expression, fileName);
    }

    private static void AssertOption(IReadOnlyList<string> arguments, string option, string expectedValue)
    {
        var index = arguments.ToList().IndexOf(option);
        Assert.True(index >= 0, $"Option '{option}' was not found.");
        Assert.Equal(expectedValue, arguments[index + 1]);
    }

    private sealed class CapturingProcessRunner : IProcessRunner
    {
        public ProcessRequest? Request { get; private set; }

        public Task<ProcessResult> RunAsync(ProcessRequest request, IProgress<ProcessLogLine>? progress = null, CancellationToken cancellationToken = default)
        {
            Request = request;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new ProcessResult(request.FileName, request.Arguments, now, now, 0, CommandExecutionStatus.Succeeded, string.Empty, string.Empty));
        }
    }

    private sealed class FakeStoragePathService(string executable) : IStoragePathService
    {
        public string GetDatabasePath() => throw new NotSupportedException();
        public string GetSettingsPath() => throw new NotSupportedException();
        public string GetManagedTrivyExecutablePath() => executable;
        public string GetReportDirectory(Project project) => throw new NotSupportedException();
        public string GetLogDirectory(Project project) => throw new NotSupportedException();
        public string GetSbomDirectory(Project project) => throw new NotSupportedException();
        public string GetReportPath(Project project, Guid scanId) => throw new NotSupportedException();
        public string GetLogPath(Project project, Guid scanId) => throw new NotSupportedException();
    }
}
