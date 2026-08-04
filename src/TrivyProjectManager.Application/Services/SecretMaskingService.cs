using System.Text.RegularExpressions;
using TrivyProjectManager.Application.Abstractions;

namespace TrivyProjectManager.Application.Services;

public sealed partial class SecretMaskingService : ISecretMaskingService
{
    public string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var masked = SensitiveAssignmentRegex().Replace(value, match =>
        {
            var key = match.Groups["key"].Value;
            return $"{key}=***";
        });

        if (masked.Length <= 12)
        {
            return "***";
        }

        return LongTokenRegex().Replace(masked, token =>
        {
            var text = token.Value;
            return text.Length <= 8 ? "***" : $"{text[..4]}***{text[^4..]}";
        });
    }

    [GeneratedRegex("(?i)(?<key>password|secret|token|authorization|connection string|client_secret|api_key)\\s*[:=]\\s*[^\\s;]+")]
    private static partial Regex SensitiveAssignmentRegex();

    [GeneratedRegex("(?<![A-Za-z0-9])[A-Za-z0-9_\\-]{20,}(?![A-Za-z0-9])")]
    private static partial Regex LongTokenRegex();
}
