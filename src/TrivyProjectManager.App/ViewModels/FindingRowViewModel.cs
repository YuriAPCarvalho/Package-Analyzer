using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.App.ViewModels;

public sealed partial class FindingRowViewModel : ObservableObject
{
    private readonly Finding _finding;

    public FindingRowViewModel(Finding finding)
    {
        _finding = finding;
        Finding = finding;
    }

    public Finding Finding { get; }
    public FindingSeverity SeverityValue => _finding.Severity;
    public FindingLifecycleStatus StatusValue => _finding.Status;
    public FindingType TypeValue => _finding.FindingType;
    public string Severity => DisplayTextService.Severity(_finding.Severity);
    public string Status => DisplayTextService.LifecycleStatus(_finding.Status);
    public string Type => DisplayTextService.FindingType(_finding.FindingType);
    public string Package => _finding.PackageName ?? "-";
    public string Vulnerability => _finding.VulnerabilityId ?? "-";
    public string Title => _finding.Title ?? _finding.Description ?? "-";
    public string InstalledVersion => _finding.InstalledVersion ?? "-";
    public string FixedVersion => string.IsNullOrWhiteSpace(_finding.FixedVersion) ? "-" : _finding.FixedVersion;
    public string FixStatus => HasFix ? "Correção disponível" : "Sem correção informada";
    public string Target => _finding.Target ?? _finding.FilePath ?? "-";
    public string Occurrences => _finding.Occurrences.Count.ToString();
    public string OccurrenceSummary => _finding.Occurrences.Count == 1 ? "1 ocorrência" : $"{_finding.Occurrences.Count} ocorrências";
    public string PrimaryUrl => _finding.PrimaryUrl ?? _finding.References.FirstOrDefault()?.Url ?? string.Empty;
    public bool HasPrimaryUrl => !string.IsNullOrWhiteSpace(PrimaryUrl);
    public bool HasFix => !string.IsNullOrWhiteSpace(_finding.FixedVersion);
    public string Description => string.IsNullOrWhiteSpace(_finding.Description) ? "Sem descrição detalhada no relatório do Trivy." : _finding.Description;
    public string References => string.Join(Environment.NewLine, _finding.References.Select(reference => reference.Url));
    public bool HasReferences => _finding.References.Count > 0;
    public string MaskedSnippet => string.IsNullOrWhiteSpace(_finding.MaskedCodeSnippet) ? "-" : _finding.MaskedCodeSnippet;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void Collapse()
    {
        IsExpanded = false;
    }
}
