using System.Text.Json;
using System.Text.Json.Nodes;
using TrivyProjectManager.Application.Abstractions;

namespace TrivyProjectManager.Application.Services;

public sealed class TrivyReportRedactionService(ISecretMaskingService maskingService)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task RedactSecretsAsync(string reportPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(reportPath))
        {
            return;
        }

        var root = JsonNode.Parse(await File.ReadAllTextAsync(reportPath, cancellationToken));
        if (root?["Results"] is not JsonArray results)
        {
            return;
        }

        var changed = false;
        foreach (var result in results.OfType<JsonObject>())
        {
            if (result["Secrets"] is not JsonArray secrets)
            {
                continue;
            }

            foreach (var secret in secrets.OfType<JsonObject>())
            {
                changed |= MaskProperty(secret, "Match");
                if (secret["Code"]?["Lines"] is JsonArray lines)
                {
                    foreach (var line in lines.OfType<JsonObject>())
                    {
                        changed |= MaskProperty(line, "Content");
                    }
                }
            }
        }

        if (changed)
        {
            await File.WriteAllTextAsync(reportPath, root.ToJsonString(JsonOptions), cancellationToken);
        }
    }

    private bool MaskProperty(JsonObject node, string propertyName)
    {
        if (node[propertyName] is null)
        {
            return false;
        }

        var value = node[propertyName]?.GetValue<string>();
        node[propertyName] = maskingService.Mask(value);
        return true;
    }
}
