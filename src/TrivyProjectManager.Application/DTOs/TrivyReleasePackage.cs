namespace TrivyProjectManager.Application.DTOs;

public sealed record TrivyReleasePackage(
    string TagName,
    Version Version,
    string AssetName,
    Uri DownloadUrl,
    string Sha256,
    long Size);
