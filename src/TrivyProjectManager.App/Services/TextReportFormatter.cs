using System.Text;
using System.Text.RegularExpressions;
using TrivyProjectManager.App.ViewModels;

namespace TrivyProjectManager.App.Services;

public static class TextReportFormatter
{
    private const string Separator = "========================================";

    public static string FormatLogs(string? projectName, IEnumerable<LogEntryViewModel> logs)
    {
        var entries = logs.ToList();
        var builder = CreateHeader(projectName, "Logs", entries.Count);
        foreach (var entry in entries)
        {
            builder.AppendLine(entry.FormattedText);
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatFindings(string? projectName, string category, IEnumerable<FindingRowViewModel> findings)
    {
        var entries = findings.ToList();
        var builder = CreateHeader(projectName, category, entries.Count);
        for (var index = 0; index < entries.Count; index++)
        {
            var finding = entries[index];
            builder.AppendLine($"{index + 1}. {finding.Vulnerability}");
            builder.AppendLine($"Tipo: {finding.Type}");
            builder.AppendLine($"Título: {finding.Title}");
            builder.AppendLine($"Severidade: {finding.Severity}");
            builder.AppendLine($"Status: {finding.Status}");
            builder.AppendLine($"Alvo/arquivo: {finding.Target}");
            builder.AppendLine("Trecho mascarado:");
            builder.AppendLine(finding.MaskedSnippet);
            if (index < entries.Count - 1)
            {
                builder.AppendLine();
                builder.AppendLine(Separator);
                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildSuggestedFileName(string? projectName, string category, DateTimeOffset generatedAt)
    {
        return $"package-analyzer-{SanitizeSegment(projectName, "projeto")}-{SanitizeSegment(category, "relatorio")}-{generatedAt:yyyyMMdd-HHmmss}.txt";
    }

    private static StringBuilder CreateHeader(string? projectName, string category, int count)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Package Analyzer");
        builder.AppendLine($"Projeto: {DisplayValue(projectName)}");
        builder.AppendLine($"Categoria: {category}");
        builder.AppendLine($"Total: {count}");
        builder.AppendLine(Separator);
        return builder;
    }

    private static string SanitizeSegment(string? value, string fallback)
    {
        var normalized = Regex.Replace(value?.Trim() ?? string.Empty, @"[^\p{L}\p{Nd}]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized.ToLowerInvariant();
    }

    private static string DisplayValue(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}
