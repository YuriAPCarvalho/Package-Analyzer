using System.Text.RegularExpressions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Services;

public sealed class FixedVersionRecommendationService
{
    public FixedVersionRecommendation Recommend(string? installedVersion, string? fixedVersion)
    {
        var candidates = SplitVersions(fixedVersion);
        if (candidates.Count == 0)
        {
            return new FixedVersionRecommendation(null, []);
        }

        var installed = ParsedVersion.TryParse(installedVersion);
        if (installed is null)
        {
            return new FixedVersionRecommendation(candidates[0].Original, candidates.Skip(1).Select(candidate => candidate.Original).ToList());
        }

        var sameMajor = candidates
            .Where(candidate => candidate.Major == installed.Major && candidate.CompareTo(installed) >= 0)
            .Order()
            .ToList();

        if (sameMajor.Count > 0)
        {
            var recommended = sameMajor[0];
            var others = candidates
                .Where(candidate => !candidate.Original.Equals(recommended.Original, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Original)
                .ToList();
            return new FixedVersionRecommendation(recommended.Original, others);
        }

        var nonDowngrades = candidates
            .Where(candidate => candidate.CompareTo(installed) >= 0)
            .Order()
            .ToList();
        if (nonDowngrades.Count > 0)
        {
            var recommended = nonDowngrades[0];
            var others = candidates
                .Where(candidate => !candidate.Original.Equals(recommended.Original, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Original)
                .ToList();
            return new FixedVersionRecommendation(recommended.Original, others);
        }

        return new FixedVersionRecommendation(null, candidates.Select(candidate => candidate.Original).ToList());
    }

    private static List<ParsedVersion> SplitVersions(string? fixedVersion)
    {
        if (string.IsNullOrWhiteSpace(fixedVersion))
        {
            return [];
        }

        return fixedVersion
            .Split([',', ';', '|', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParsedVersion.TryParse)
            .Where(candidate => candidate is not null)
            .Cast<ParsedVersion>()
            .DistinctBy(candidate => candidate.Original, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record ParsedVersion(string Original, int Major, int Minor, int Patch, string? Suffix) : IComparable<ParsedVersion>
    {
        private static readonly Regex VersionRegex = new(@"(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?(?<suffix>[-+][A-Za-z0-9.\-]+)?", RegexOptions.Compiled);

        public static ParsedVersion? TryParse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            var match = VersionRegex.Match(trimmed);
            if (!match.Success)
            {
                return null;
            }

            return new ParsedVersion(
                trimmed,
                int.Parse(match.Groups["major"].Value),
                ParseOrZero(match.Groups["minor"].Value),
                ParseOrZero(match.Groups["patch"].Value),
                match.Groups["suffix"].Success ? match.Groups["suffix"].Value : null);
        }

        public int CompareTo(ParsedVersion? other)
        {
            if (other is null)
            {
                return 1;
            }

            var major = Major.CompareTo(other.Major);
            if (major != 0)
            {
                return major;
            }

            var minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
            {
                return minor;
            }

            return Patch.CompareTo(other.Patch);
        }

        private static int ParseOrZero(string value) => int.TryParse(value, out var parsed) ? parsed : 0;
    }
}
