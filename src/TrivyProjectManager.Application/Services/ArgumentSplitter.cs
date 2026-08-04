namespace TrivyProjectManager.Application.Services;

public static class ArgumentSplitter
{
    public static IReadOnlyList<string> Split(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;
        var escape = false;

        foreach (var c in arguments)
        {
            if (escape)
            {
                current.Add(c);
                escape = false;
                continue;
            }

            if (c == '\\' && inQuotes)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                Flush(values, current);
                continue;
            }

            current.Add(c);
        }

        Flush(values, current);
        return values;
    }

    private static void Flush(List<string> values, List<char> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        values.Add(new string([.. current]));
        current.Clear();
    }
}
