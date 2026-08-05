using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.App.ViewModels;

public sealed class ProjectCardViewModel(Project project)
{
    public Project Project { get; } = project;
    public Guid Id => Project.Id;
    public string Name => Project.Name;
    public string Path => Project.Path;
    public ProjectTechnology Technology => Project.Technology;
    public PackageManagerType PackageManager => Project.PackageManager;
    public string LastScan => Project.LastScanAt?.ToLocalTime().ToString("g") ?? "Nunca";
    public string LastStatus => Project.Scans.OrderByDescending(scan => scan.StartedAt).FirstOrDefault() is { } scan
        ? DisplayTextService.ScanStatus(scan.Status)
        : "Sem execução";
    public int Critical => LastSucceededScan?.CriticalCount ?? 0;
    public int High => LastSucceededScan?.HighCount ?? 0;
    public int Medium => LastSucceededScan?.MediumCount ?? 0;
    public int Low => LastSucceededScan?.LowCount ?? 0;
    public Scan? LastSucceededScan => Project.Scans
        .Where(scan => scan.Status is ScanStatus.Succeeded or ScanStatus.SucceededWithWarnings)
        .OrderByDescending(scan => scan.StartedAt)
        .FirstOrDefault();
}
