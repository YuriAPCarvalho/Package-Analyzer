using System.Text;
using System.Text.RegularExpressions;

namespace TrivyProjectManager.Infrastructure.Services;

internal static partial class ProcessOutputSanitizer
{
    public static string Sanitize(string value)
    {
        var sanitized = OscSequenceRegex().Replace(value, string.Empty);
        sanitized = CsiSequenceRegex().Replace(sanitized, string.Empty);
        sanitized = EscapeSequenceRegex().Replace(sanitized, string.Empty);

        if (!sanitized.Any(character => char.IsControl(character) && character != '\t'))
        {
            return sanitized;
        }

        var builder = new StringBuilder(sanitized.Length);
        foreach (var character in sanitized)
        {
            if (!char.IsControl(character) || character == '\t')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex("\\x1B\\][^\\x07]*(?:\\x07|\\x1B\\\\)", RegexOptions.CultureInvariant)]
    private static partial Regex OscSequenceRegex();

    [GeneratedRegex("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.CultureInvariant)]
    private static partial Regex CsiSequenceRegex();

    [GeneratedRegex("\\x1B[@-_]", RegexOptions.CultureInvariant)]
    private static partial Regex EscapeSequenceRegex();
}
