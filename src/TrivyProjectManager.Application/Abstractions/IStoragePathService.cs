using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Abstractions;

public interface IStoragePathService
{
    string GetDatabasePath();
    string GetSettingsPath();
    string GetReportDirectory(Project project);
    string GetLogDirectory(Project project);
    string GetSbomDirectory(Project project);
    string GetReportPath(Project project, Guid scanId);
    string GetLogPath(Project project, Guid scanId);
}
