using TrivyProjectManager.App.Services;
using TrivyProjectManager.App.ViewModels;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.UnitTests;

public sealed class LogAndExportTests
{
    [Theory]
    [InlineData("stderr", "2026-08-07 INFO database updated", LogDisplayLevel.Info)]
    [InlineData("stdout", "WARN deprecated option", LogDisplayLevel.Warning)]
    [InlineData("stdout", "WARNING: review this", LogDisplayLevel.Warning)]
    [InlineData("stdout", "ERROR scan failed", LogDisplayLevel.Error)]
    [InlineData("stdout", "FATAL unexpected failure", LogDisplayLevel.Error)]
    [InlineData("warning", "Preparation was skipped", LogDisplayLevel.Warning)]
    [InlineData("stderr", "command failed", LogDisplayLevel.Error)]
    [InlineData("stdout", "command completed", LogDisplayLevel.Info)]
    public void LogClassificationUsesMessageLevelBeforeStream(string stream, string message, LogDisplayLevel expected)
    {
        Assert.Equal(expected, LogEntryViewModel.Classify(stream, message));
    }

    [Fact]
    public void LogReportPreservesOrderAndFormattedText()
    {
        var first = new LogEntryViewModel(new ProcessLogLine(new DateTimeOffset(2026, 8, 7, 10, 11, 12, TimeSpan.Zero), "stdout", "INFO início"));
        var second = new LogEntryViewModel(new ProcessLogLine(new DateTimeOffset(2026, 8, 7, 10, 11, 13, TimeSpan.Zero), "stderr", "ERROR falha"));

        var report = TextReportFormatter.FormatLogs("Aplicação Ágil", [first, second]);

        Assert.Contains("Projeto: Aplicação Ágil", report);
        Assert.Contains("Total: 2", report);
        Assert.True(report.IndexOf(first.FormattedText, StringComparison.Ordinal) < report.IndexOf(second.FormattedText, StringComparison.Ordinal));
    }

    [Fact]
    public void FindingReportContainsDetailsAndOnlyMaskedSnippet()
    {
        const string rawSecret = "FAKE_RAW_SECRET";
        var first = CreateFinding(FindingType.Misconfiguration, "AVD-DS-0002", "Imagem sem usuário", "Dockerfile.dev", "USER ***");
        var second = CreateFinding(FindingType.Secret, "generic-api-key", "Chave de API", ".env", $"TOKEN=***{rawSecret[..4]}");

        var report = TextReportFormatter.FormatFindings("Aplicação Ágil", "Segredos", [first, second]);

        Assert.Contains("Título: Imagem sem usuário", report);
        Assert.Contains("Alvo/arquivo: Dockerfile.dev", report);
        Assert.Contains("Trecho mascarado:", report);
        Assert.Contains("TOKEN=***FAKE", report);
        Assert.DoesNotContain(rawSecret, report);
        Assert.Contains("Total: 2", report);
    }

    [Fact]
    public void SuggestedFileNameIsSafeAndIncludesTimestamp()
    {
        var name = TextReportFormatter.BuildSuggestedFileName("Minha API / Produção", "Configurações incorretas", new DateTimeOffset(2026, 8, 7, 14, 30, 45, TimeSpan.Zero));

        Assert.Equal("package-analyzer-minha-api-produção-configurações-incorretas-20260807-143045.txt", name);
    }

    private static FindingRowViewModel CreateFinding(FindingType type, string id, string title, string target, string maskedSnippet)
    {
        var finding = new Finding
        {
            FindingType = type,
            VulnerabilityId = id,
            Title = title,
            Target = target,
            FilePath = target,
            Severity = FindingSeverity.High,
            Status = FindingLifecycleStatus.New,
            MaskedCodeSnippet = maskedSnippet
        };
        return new FindingRowViewModel(finding, null, new FindingTextService(), new UpdateCommandService());
    }
}
