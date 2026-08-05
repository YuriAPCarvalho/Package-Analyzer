using System.Text.RegularExpressions;

namespace TrivyProjectManager.Application.Services;

public sealed class FindingTextService
{
    public string Title(string? title, string? description)
    {
        return FirstUseful(title, description, "-");
    }

    public string Description(string? title, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Descrição detalhada não fornecida pelo Trivy.";
        }

        if (IsDuplicate(title, description))
        {
            return "Descrição detalhada não fornecida pelo Trivy.";
        }

        return description.Trim();
    }

    public bool HasDetailedDescription(string? title, string? description)
    {
        return !Description(title, description).Equals("Descrição detalhada não fornecida pelo Trivy.", StringComparison.Ordinal);
    }

    private static bool IsDuplicate(string? title, string? description)
    {
        var normalizedTitle = Normalize(title);
        var normalizedDescription = Normalize(description);
        if (string.IsNullOrWhiteSpace(normalizedTitle) || string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return false;
        }

        return normalizedTitle.Equals(normalizedDescription, StringComparison.OrdinalIgnoreCase)
            || normalizedTitle.Contains(normalizedDescription, StringComparison.OrdinalIgnoreCase)
            || normalizedDescription.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return Regex.Replace(value ?? string.Empty, @"[^a-zA-Z0-9]+", " ").Trim();
    }

    private static string FirstUseful(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "-";
    }
}
