using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.App.ViewModels;

public sealed partial class FindingRowViewModel : ObservableObject
{
    private readonly Finding _finding;
    private readonly FindingTextService _textService;

    public FindingRowViewModel(Finding finding, Project? project, FindingTextService textService, UpdateCommandService updateCommandService)
    {
        _finding = finding;
        _textService = textService;
        Finding = finding;
        Project = project;
        ReferencesList = finding.References
            .Select(reference => new FindingReferenceViewModel(FriendlyReferenceName(reference.DisplayName, reference.Url), reference.Url, reference.IsPrimary))
            .ToList();
        OccurrenceList = finding.Occurrences
            .Select(occurrence => new FindingOccurrenceViewModel(
                CleanProjectName(occurrence.ProjectName, occurrence.Target),
                FirstUseful(occurrence.RelativePath, occurrence.FilePath, occurrence.Target, "-"),
                FirstUseful(occurrence.AbsolutePath, occurrence.FilePath, occurrence.Target, "-"),
                occurrence.ProjectFilePath))
            .ToList();
        UpdateCommand = project is null ? string.Empty : updateCommandService.Build(project, finding) ?? string.Empty;
    }

    public Finding Finding { get; }
    public Project? Project { get; }
    public FindingSeverity SeverityValue => _finding.Severity;
    public FindingLifecycleStatus StatusValue => _finding.Status;
    public FindingType TypeValue => _finding.FindingType;
    public DependencyRelation DependencyRelationValue => _finding.DependencyRelation;
    public FixAvailability FixAvailabilityValue => _finding.FixAvailability;
    public string Severity => DisplayTextService.Severity(_finding.Severity);
    public string Status => DisplayTextService.LifecycleStatus(_finding.Status);
    public string Type => DisplayTextService.FindingType(_finding.FindingType);
    public string Package => FirstUseful(_finding.PackageName, "-");
    public string Vulnerability => FirstUseful(_finding.VulnerabilityId, "-");
    public string Title => _textService.Title(_finding.Title, _finding.Description);
    public string Description => _textService.Description(_finding.Title, _finding.Description);
    public string InstalledVersion => FirstUseful(_finding.InstalledVersion, "-");
    public string FixedVersion => FirstUseful(_finding.FixedVersion, "-");
    public string RecommendedFixedVersion => FirstUseful(_finding.RecommendedFixedVersion, "-");
    public string OtherFixedVersions => FirstUseful(_finding.OtherFixedVersions, "Nenhuma outra linha corrigida informada.");
    public string FixStatus => _finding.FixAvailability switch
    {
        FixAvailability.Available => "Disponível",
        FixAvailability.Unavailable => "Não disponível",
        FixAvailability.NotInformed => "Não informada",
        _ => HasFix ? "Disponível" : "Não informada"
    };
    public string DependencyRelation => _finding.DependencyRelation switch
    {
        Domain.Enums.DependencyRelation.Direct => "Direta",
        Domain.Enums.DependencyRelation.Transitive => "Transitiva",
        _ => "Não informada"
    };
    public string DependencyGuidance => _finding.DependencyRelation == Domain.Enums.DependencyRelation.Transitive
        ? "Dependência transitiva. Identifique o pacote pai responsável antes de atualizar."
        : string.Empty;
    public string Ecosystem => FirstUseful(_finding.Ecosystem, "Não informado pela fonte");
    public string SeveritySource => FirstUseful(_finding.SeveritySource, "Não informada pela fonte");
    public string Cvss => _finding.CvssScore.HasValue ? _finding.CvssScore.Value.ToString("0.0") : "Não informado pela fonte";
    public string CvssVector => FirstUseful(_finding.CvssVector, "Não informado pela fonte");
    public string Cwe => FirstUseful(_finding.CweIds, "Não informado pela fonte");
    public string EnrichmentSource => FirstUseful(_finding.EnrichmentSource, "Dados locais do Trivy");
    public string PublishedDate => _finding.PublishedDate?.ToLocalTime().ToString("dd/MM/yyyy") ?? "Não informada pela fonte";
    public string LastModifiedDate => _finding.LastModifiedDate?.ToLocalTime().ToString("dd/MM/yyyy") ?? "Não informada pela fonte";
    public string RuntimeSupportAlert => _finding.RuntimeSupportAlert ?? string.Empty;
    public bool HasRuntimeSupportAlert => !string.IsNullOrWhiteSpace(RuntimeSupportAlert);
    public string Target => FirstUseful(_finding.Target, _finding.FilePath, "-");
    public string Occurrences => _finding.Occurrences.Count.ToString();
    public string OccurrenceSummary => _finding.Occurrences.Count == 1 ? "1 ocorrência" : $"{_finding.Occurrences.Count} ocorrências";
    public string PrimaryUrl => _finding.PrimaryUrl ?? _finding.References.FirstOrDefault(reference => reference.IsPrimary)?.Url ?? string.Empty;
    public bool HasPrimaryUrl => !string.IsNullOrWhiteSpace(PrimaryUrl);
    public bool HasFix => !string.IsNullOrWhiteSpace(_finding.RecommendedFixedVersion) || !string.IsNullOrWhiteSpace(_finding.FixedVersion);
    public bool HasReferences => ReferencesList.Count > 0;
    public bool HasUpdateCommand => !string.IsNullOrWhiteSpace(UpdateCommand);
    public string UpdateCommand { get; }
    public string MaskedSnippet => string.IsNullOrWhiteSpace(_finding.MaskedCodeSnippet) ? "-" : _finding.MaskedCodeSnippet;
    public IReadOnlyList<FindingReferenceViewModel> ReferencesList { get; }
    public IReadOnlyList<FindingOccurrenceViewModel> OccurrenceList { get; }

    public string CopyDetailsText => string.Join(Environment.NewLine, new[]
    {
        $"Pacote: {Package}",
        $"Vulnerabilidade: {Vulnerability}",
        $"Severidade: {Severity}",
        $"Situação: {Status}",
        $"Versão instalada: {InstalledVersion}",
        $"Atualização recomendada: {RecommendedFixedVersion}",
        $"Ocorrencias: {Occurrences}",
        $"Dependência: {DependencyRelation}"
    });

    public string SearchText => string.Join(' ', new[]
    {
        Package,
        Vulnerability,
        Title,
        Target,
        InstalledVersion,
        FixedVersion,
        RecommendedFixedVersion,
        string.Join(' ', OccurrenceList.Select(occurrence => $"{occurrence.ProjectName} {occurrence.RelativePath} {occurrence.AbsolutePath}"))
    });

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void Collapse()
    {
        IsExpanded = false;
    }

    private static string CleanProjectName(string? projectName, string? target)
    {
        var value = FirstUseful(projectName, target, "-");
        if (value.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase))
        {
            return value[..^".deps.json".Length];
        }

        return value;
    }

    private static string FirstUseful(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "-";
    }

    private static string FriendlyReferenceName(string? displayName, string url)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
    }
}

public sealed record FindingReferenceViewModel(string DisplayName, string Url, bool IsPrimary)
{
    public string Label => IsPrimary ? $"{DisplayName} (principal)" : DisplayName;
}

public sealed record FindingOccurrenceViewModel(string ProjectName, string RelativePath, string AbsolutePath, string? ProjectFilePath);
