using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using System.Text.Json;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class StoragePathService : IStoragePathService
{
    private readonly string _basePath;

    public StoragePathService()
    {
        _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrivyProjectManager");
    }

    public string GetDatabasePath() => Path.Combine(_basePath, "data", "trivy-project-manager.db");

    public string GetSettingsPath() => Path.Combine(_basePath, "settings.json");

    public string GetReportDirectory(Project project)
    {
        return project.StorageMode == ReportStorageMode.InsideProject
            ? Path.Combine(project.Path, ".security", "trivy", "history")
            : Path.Combine(GetConfiguredStorageBasePath(), "Projects", project.Id.ToString("N"), "reports");
    }

    public string GetLogDirectory(Project project)
    {
        return project.StorageMode == ReportStorageMode.InsideProject
            ? Path.Combine(project.Path, ".security", "trivy", "logs")
            : Path.Combine(GetConfiguredStorageBasePath(), "Projects", project.Id.ToString("N"), "logs");
    }

    public string GetSbomDirectory(Project project)
    {
        return project.StorageMode == ReportStorageMode.InsideProject
            ? Path.Combine(project.Path, ".security", "trivy", "sbom")
            : Path.Combine(GetConfiguredStorageBasePath(), "Projects", project.Id.ToString("N"), "sbom");
    }

    public string GetReportPath(Project project, Guid scanId)
    {
        return Path.Combine(GetReportDirectory(project), $"{scanId:N}.json");
    }

    public string GetLogPath(Project project, Guid scanId)
    {
        return Path.Combine(GetLogDirectory(project), $"{scanId:N}.log");
    }

    private string GetConfiguredStorageBasePath()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return _basePath;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (document.RootElement.TryGetProperty("StorageDirectory", out var element))
            {
                var configured = element.GetString();
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return configured;
                }
            }
        }
        catch (JsonException)
        {
            return _basePath;
        }

        return _basePath;
    }
}
