using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Domain.ValueObjects;

public static class FindingKey
{
    public static string Create(
        FindingType type,
        string? vulnerabilityId,
        string? packageName,
        string? installedVersion,
        string? target = null,
        string? title = null)
    {
        var identity = FirstUseful(vulnerabilityId, title, target, "unknown");
        return string.Join('|',
            type.ToString().ToUpperInvariant(),
            Normalize(identity),
            Normalize(packageName),
            Normalize(installedVersion));
    }

    private static string FirstUseful(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "unknown";
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToUpperInvariant();
    }
}
