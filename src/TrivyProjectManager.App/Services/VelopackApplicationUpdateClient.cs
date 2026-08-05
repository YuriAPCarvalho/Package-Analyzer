using System.Collections.Concurrent;
using System.Reflection;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace TrivyProjectManager.App.Services;

public sealed class VelopackApplicationUpdateClient : IApplicationUpdateClient
{
    private const string RepositoryUrl = "https://github.com/YuriAPCarvalho/Package-Analyzer";
    private readonly ConcurrentDictionary<string, UpdateInfo> _updates = new(StringComparer.OrdinalIgnoreCase);

    public string InstalledVersion
    {
        get
        {
            var manager = CreateManager("stable");
            return manager.CurrentVersion?.ToString() ?? GetAssemblyVersion();
        }
    }

    public async Task<ApplicationUpdatePackage?> CheckForUpdatesAsync(
        string channel,
        CancellationToken cancellationToken = default)
    {
        var manager = CreateManager(channel);
        if (!manager.IsInstalled)
        {
            throw new ApplicationUpdateNotInstalledException("A aplicação precisa estar instalada pelo Velopack para verificar atualizações.");
        }

        var updateInfo = await manager.CheckForUpdatesAsync();
        if (updateInfo is null)
        {
            return null;
        }

        var asset = updateInfo.TargetFullRelease;
        var id = string.IsNullOrWhiteSpace(asset.FileName)
            ? asset.Version.ToString()
            : asset.FileName;
        _updates[id] = updateInfo;

        return new ApplicationUpdatePackage(
            id,
            asset.Version.ToString(),
            asset.NotesMarkdown,
            asset.NotesHTML);
    }

    public async Task DownloadUpdateAsync(
        ApplicationUpdatePackage package,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var manager = CreateManager("stable");
        if (!manager.IsInstalled)
        {
            throw new ApplicationUpdateNotInstalledException("A aplicação precisa estar instalada pelo Velopack para baixar atualizações.");
        }

        var updateInfo = GetUpdateInfo(package);
        Action<int>? reportProgress = progress is null ? null : progress.Report;
        await manager.DownloadUpdatesAsync(updateInfo, reportProgress, cancellationToken);
    }

    public void ApplyUpdateAndRestart(ApplicationUpdatePackage package)
    {
        var manager = CreateManager("stable");
        if (!manager.IsInstalled)
        {
            throw new ApplicationUpdateNotInstalledException("A aplicação precisa estar instalada pelo Velopack para aplicar atualizações.");
        }

        var updateInfo = GetUpdateInfo(package);
        manager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
    }

    private UpdateInfo GetUpdateInfo(ApplicationUpdatePackage package)
    {
        if (_updates.TryGetValue(package.Id, out var updateInfo))
        {
            return updateInfo;
        }

        throw new InvalidOperationException("A atualização não está mais disponível nesta sessão. Verifique novamente.");
    }

    private static UpdateManager CreateManager(string channel)
    {
        var options = new UpdateOptions
        {
            ExplicitChannel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel
        };
        return new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false), options);
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(VelopackApplicationUpdateClient).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "0.1.0";
    }
}
