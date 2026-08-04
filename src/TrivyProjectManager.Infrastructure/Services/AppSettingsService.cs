using System.Text.Json;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class AppSettingsService(IStoragePathService storagePathService) : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = storagePathService.GetSettingsPath();
        if (!File.Exists(path))
        {
            var settings = new AppSettings();
            settings.StorageDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrivyProjectManager");
            await SaveAsync(settings, cancellationToken);
            return settings;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken)
            ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var path = storagePathService.GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
