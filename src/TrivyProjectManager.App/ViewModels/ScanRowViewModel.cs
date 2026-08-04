using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.App.ViewModels;

public sealed class ScanRowViewModel
{
    private readonly Scan _scan;
    private readonly string _projectName;

    public ScanRowViewModel(Scan scan, string projectName)
    {
        _scan = scan;
        _projectName = projectName;
        Scan = scan;
    }

    public Scan Scan { get; }
    public string ProjectName => _projectName;
    public string Date => _scan.StartedAt.ToLocalTime().ToString("g");
    public string Duration => _scan.FinishedAt is null ? "-" : (_scan.FinishedAt.Value - _scan.StartedAt).ToString(@"hh\:mm\:ss");
    public string Status => DisplayTextService.ScanStatus(_scan.Status);
    public string TrivyVersion => _scan.TrivyVersion ?? "-";
    public int Critical => _scan.CriticalCount;
    public int High => _scan.HighCount;
    public int Medium => _scan.MediumCount;
    public int Low => _scan.LowCount;
    public int New => _scan.NewCount;
    public int Resolved => _scan.ResolvedCount;
}
