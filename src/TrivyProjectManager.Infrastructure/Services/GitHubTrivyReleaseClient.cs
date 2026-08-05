using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class GitHubTrivyReleaseClient : ITrivyReleaseClient
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/aquasecurity/trivy/releases/latest";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public GitHubTrivyReleaseClient()
        : this(CreateHttpClient())
    {
    }

    internal GitHubTrivyReleaseClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TrivyReleasePackage> GetLatestWindowsX64Async(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(LatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseDto>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A resposta do GitHub para o release do Trivy está vazia.");
        var asset = release.Assets.FirstOrDefault(candidate =>
            candidate.Name.EndsWith("_windows-64bit.zip", StringComparison.OrdinalIgnoreCase));

        if (!TryParseVersion(release.TagName, out var version)
            || asset is null
            || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl)
            || !Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var downloadUrl)
            || !TryParseSha256(asset.Digest, out var sha256))
        {
            throw new InvalidOperationException("O release estável do Trivy não contém um pacote Windows x64 válido com digest SHA-256.");
        }

        return new TrivyReleasePackage(release.TagName, version, asset.Name, downloadUrl, sha256, asset.Size);
    }

    public async Task DownloadAsync(TrivyReleasePackage package, string destinationPath, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(package.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Package-Analyzer", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool TryParseVersion(string tagName, out Version version)
    {
        return Version.TryParse(tagName.Trim().TrimStart('v', 'V'), out version!);
    }

    private static bool TryParseSha256(string? digest, out string sha256)
    {
        sha256 = string.Empty;
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var candidate = digest["sha256:".Length..];
        if (candidate.Length != 64 || candidate.Any(character => !Uri.IsHexDigit(character)))
        {
            return false;
        }

        sha256 = candidate.ToLowerInvariant();
        return true;
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        public List<GitHubReleaseAssetDto> Assets { get; set; } = [];
    }

    private sealed class GitHubReleaseAssetDto
    {
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        public string? Digest { get; set; }

        public long Size { get; set; }
    }
}
