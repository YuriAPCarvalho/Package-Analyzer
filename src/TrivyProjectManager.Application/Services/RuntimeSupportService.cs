using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public static class RuntimeSupportService
{
    private static readonly IReadOnlyDictionary<int, DateOnly> DotNetEndOfSupport = new Dictionary<int, DateOnly>
    {
        [6] = new(2024, 11, 12),
        [7] = new(2024, 5, 14),
        [8] = new(2026, 11, 10),
        [9] = new(2026, 11, 10),
        [10] = new(2028, 11, 14)
    };

    private static readonly IReadOnlyDictionary<int, DateOnly> NodeEndOfSupport = new Dictionary<int, DateOnly>
    {
        [18] = new(2025, 4, 30),
        [20] = new(2026, 4, 30),
        [22] = new(2027, 4, 30),
        [24] = new(2028, 4, 30),
        [25] = new(2026, 6, 1),
        [26] = new(2029, 4, 30)
    };

    public static async Task<string?> TryBuildAlertAsync(string projectPath, ProjectTechnology technology, CancellationToken cancellationToken = default)
    {
        return technology switch
        {
            ProjectTechnology.DotNet => await BuildDotNetAlertAsync(projectPath, cancellationToken),
            ProjectTechnology.Node => await BuildNodeAlertAsync(projectPath, cancellationToken),
            _ => null
        };
    }

    private static async Task<string?> BuildDotNetAlertAsync(string projectPath, CancellationToken cancellationToken)
    {
        var versions = new HashSet<int>();
        foreach (var csproj in Directory.EnumerateFiles(projectPath, "*.csproj", SearchOption.AllDirectories))
        {
            try
            {
                await using var stream = File.OpenRead(csproj);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                var frameworks = document.Descendants()
                    .Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
                    .SelectMany(element => (element.Value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                foreach (var framework in frameworks)
                {
                    var match = Regex.Match(framework, @"net(?<major>\d+)\.");
                    if (match.Success && int.TryParse(match.Groups["major"].Value, out var major))
                    {
                        versions.Add(major);
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return BuildAlert(".NET", versions, DotNetEndOfSupport);
    }

    private static async Task<string?> BuildNodeAlertAsync(string projectPath, CancellationToken cancellationToken)
    {
        var packageJson = Path.Combine(projectPath, "package.json");
        if (!File.Exists(packageJson))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(packageJson);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("engines", out var engines)
                || !engines.TryGetProperty("node", out var node)
                || node.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var versions = Regex.Matches(node.GetString() ?? string.Empty, @"\d+")
                .Select(match => int.TryParse(match.Value, out var major) ? major : 0)
                .Where(major => major > 0)
                .ToHashSet();
            return BuildAlert("Node.js", versions, NodeEndOfSupport);
        }
        catch
        {
            return null;
        }
    }

    private static string? BuildAlert(string platform, IReadOnlyCollection<int> versions, IReadOnlyDictionary<int, DateOnly> endOfSupport)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var unsupported = versions
            .Where(version => endOfSupport.TryGetValue(version, out var end) && end < today)
            .Order()
            .ToList();

        if (unsupported.Count == 0)
        {
            return null;
        }

        return $"Atenção: o projeto utiliza {platform} {string.Join(", ", unsupported)} fora de suporte. A atualização pontual pode corrigir esta vulnerabilidade, mas recomenda-se planejar a atualização da plataforma.";
    }
}
