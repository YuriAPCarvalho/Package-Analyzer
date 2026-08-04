using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TrivyProjectManager.App.Services;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly TrivyProjectManagerDbContext _dbContext;
    private readonly IProjectDetectionService _detectionService;
    private readonly ICommandProfileService _commandProfileService;
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IAppSettingsService _settingsService;
    private readonly ITrivyBootstrapService _trivyBootstrapService;
    private readonly IExternalLinkService _externalLinkService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _scanCancellation;
    private List<FindingRowViewModel> _allFindings = [];

    public MainWindowViewModel(
        TrivyProjectManagerDbContext dbContext,
        IProjectDetectionService detectionService,
        ICommandProfileService commandProfileService,
        IScanOrchestrator scanOrchestrator,
        IAppSettingsService settingsService,
        ITrivyBootstrapService trivyBootstrapService,
        IExternalLinkService externalLinkService,
        IDialogService dialogService)
    {
        _dbContext = dbContext;
        _detectionService = detectionService;
        _commandProfileService = commandProfileService;
        _scanOrchestrator = scanOrchestrator;
        _settingsService = settingsService;
        _trivyBootstrapService = trivyBootstrapService;
        _externalLinkService = externalLinkService;
        _dialogService = dialogService;
    }

    public string AppTitle => "Package-Analyzer by: YuriAPCarvalho";
    public ObservableCollection<ProjectCardViewModel> Projects { get; } = [];
    public ObservableCollection<FindingRowViewModel> Findings { get; } = [];
    public ObservableCollection<FindingRowViewModel> MisconfigurationFindings { get; } = [];
    public ObservableCollection<FindingRowViewModel> SecretFindings { get; } = [];
    public ObservableCollection<ScanRowViewModel> Scans { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<ProjectCommand> Commands { get; } = [];
    public IReadOnlyList<string> SeverityOptions { get; } =
    [
        "Todas",
        DisplayTextService.Severity(FindingSeverity.Critical),
        DisplayTextService.Severity(FindingSeverity.High),
        DisplayTextService.Severity(FindingSeverity.Medium),
        DisplayTextService.Severity(FindingSeverity.Low),
        DisplayTextService.Severity(FindingSeverity.Unknown)
    ];

    public IReadOnlyList<string> StatusOptions { get; } =
    [
        "Todos",
        DisplayTextService.LifecycleStatus(FindingLifecycleStatus.New),
        DisplayTextService.LifecycleStatus(FindingLifecycleStatus.Existing),
        DisplayTextService.LifecycleStatus(FindingLifecycleStatus.Resolved),
        DisplayTextService.LifecycleStatus(FindingLifecycleStatus.Regression),
        DisplayTextService.LifecycleStatus(FindingLifecycleStatus.Ignored)
    ];
    public IReadOnlyList<ProjectTechnology> TechnologyOptions { get; } = Enum.GetValues<ProjectTechnology>();
    public IReadOnlyList<PackageManagerType> PackageManagerOptions { get; } = Enum.GetValues<PackageManagerType>();

    private ProjectCardViewModel? _selectedProject;
    public ProjectCardViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnPropertyChanged(nameof(HasSelectedProject));
                OnPropertyChanged(nameof(UseInsideProjectStorage));
                RunQuickScanCommand.NotifyCanExecuteChanged();
                RunFullScanCommand.NotifyCanExecuteChanged();
                _ = LoadProjectDetailsAsync(value?.Id ?? Guid.Empty);
            }
        }
    }

    public bool HasSelectedProject => SelectedProject is not null;

    public bool UseInsideProjectStorage
    {
        get => SelectedProject?.Project.StorageMode == ReportStorageMode.InsideProject;
        set
        {
            if (SelectedProject is null)
            {
                return;
            }

            SelectedProject.Project.StorageMode = value ? ReportStorageMode.InsideProject : ReportStorageMode.Central;
            OnPropertyChanged();
        }
    }

    private string _progressText = "Pronto";
    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    private bool _isScanRunning;
    public bool IsScanRunning
    {
        get => _isScanRunning;
        set
        {
            if (SetProperty(ref _isScanRunning, value))
            {
                OnPropertyChanged(nameof(CanRunScan));
                RunQuickScanCommand.NotifyCanExecuteChanged();
                RunFullScanCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanRunScan => HasSelectedProject && !IsScanRunning;

    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    private string _selectedSeverity = "Todas";
    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set
        {
            if (SetProperty(ref _selectedSeverity, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _selectedStatus = "Todos";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _packageFilter = string.Empty;
    public string PackageFilter
    {
        get => _packageFilter;
        set
        {
            if (SetProperty(ref _packageFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _idFilter = string.Empty;
    public string IdFilter
    {
        get => _idFilter;
        set
        {
            if (SetProperty(ref _idFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _targetFilter = string.Empty;
    public string TargetFilter
    {
        get => _targetFilter;
        set
        {
            if (SetProperty(ref _targetFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private bool _onlyWithFix;
    public bool OnlyWithFix
    {
        get => _onlyWithFix;
        set
        {
            if (SetProperty(ref _onlyWithFix, value))
            {
                ApplyFilters();
            }
        }
    }

    public int CriticalCount => SelectedProject?.LastSucceededScan?.CriticalCount ?? 0;
    public int HighCount => SelectedProject?.LastSucceededScan?.HighCount ?? 0;
    public int MediumCount => SelectedProject?.LastSucceededScan?.MediumCount ?? 0;
    public int LowCount => SelectedProject?.LastSucceededScan?.LowCount ?? 0;
    public int UnknownCount => SelectedProject?.LastSucceededScan?.UnknownCount ?? 0;
    public int UniqueVulnerabilityCount => SelectedProject?.LastSucceededScan?.UniqueVulnerabilityCount ?? 0;
    public int OccurrenceCount => SelectedProject?.LastSucceededScan?.TotalOccurrenceCount ?? 0;
    public int MisconfigurationCount => SelectedProject?.LastSucceededScan?.MisconfigurationCount ?? 0;
    public int SecretCount => SelectedProject?.LastSucceededScan?.SecretCount ?? 0;
    public int NewCount => SelectedProject?.LastSucceededScan?.NewCount ?? 0;
    public int ResolvedCount => SelectedProject?.LastSucceededScan?.ResolvedCount ?? 0;
    public int RegressionCount => SelectedProject?.LastSucceededScan?.RegressionCount ?? 0;
    public bool HasFindings => Findings.Count > 0;
    public bool HasNoFindings => !HasFindings;
    public bool HasMisconfigurationFindings => MisconfigurationFindings.Count > 0;
    public bool HasNoMisconfigurationFindings => !HasMisconfigurationFindings;
    public bool HasSecretFindings => SecretFindings.Count > 0;
    public bool HasNoSecretFindings => !HasSecretFindings;
    public bool HasScans => Scans.Count > 0;
    public bool HasNoScans => !HasScans;

    [RelayCommand]
    private async Task LoadAsync()
    {
        Settings = await _settingsService.LoadAsync();
        await EnsureTrivyAvailableAsync();
        await ReloadProjectsAsync(selectFirstWhenMissing: true);
    }

    private async Task EnsureTrivyAvailableAsync()
    {
        if (!Settings.AutoInstallTrivy)
        {
            return;
        }

        try
        {
            ProgressText = "Verificando Trivy...";
            var progress = new Progress<string>(message => Dispatcher.UIThread.Post(() => ProgressText = message));
            var result = await _trivyBootstrapService.EnsureAvailableAsync(Settings, progress);
            Settings.TrivyPath = result.ExecutablePath ?? Settings.TrivyPath;
            ProgressText = result.Version is null
                ? result.Message
                : $"{result.Message} {result.Version}";
        }
        catch (Exception ex)
        {
            ProgressText = "Não foi possível preparar o Trivy";
            await _dialogService.ShowMessageAsync(
                "Trivy automático",
                $"Não foi possível baixar ou atualizar o Trivy automaticamente. Você ainda pode configurar o caminho do trivy.exe manualmente em Configurações.\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        var folder = await _dialogService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            var detection = await _detectionService.DetectAsync(folder);
            var name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var project = new Project
            {
                Name = string.IsNullOrWhiteSpace(name) ? folder : name,
                Path = folder,
                Technology = detection.SuggestedTechnology,
                PackageManager = detection.SuggestedPackageManager,
                StorageMode = ReportStorageMode.Central
            };
            project.Commands.AddRange(_commandProfileService.CreateDefaultCommands(project.Technology, project.PackageManager, project.Path));
            _dbContext.Projects.Add(project);
            await _dbContext.SaveChangesAsync();
            await ReloadProjectsAsync(project.Id, selectFirstWhenMissing: true);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Erro ao cadastrar projeto", ex.Message);
        }
    }

    [RelayCommand]
    private Task RunQuickScanAsync() => RunScanAsync(ScanMode.Quick);

    [RelayCommand]
    private async Task RunFullScanAsync()
    {
        if (SelectedProject is null)
        {
            await _dialogService.ShowMessageAsync("Projeto não selecionado", "Selecione um projeto na lista antes de executar o scan.");
            return;
        }

        if (IsScanRunning)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Executar scan completo",
            "Os comandos de preparação podem executar scripts definidos pelo próprio projeto. Execute apenas projetos confiáveis.");
        if (confirmed)
        {
            await RunScanAsync(ScanMode.Full);
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCancellation?.Cancel();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveAsync(Settings);
        await _dialogService.ShowMessageAsync("Configurações", "Configurações salvas.");
    }

    [RelayCommand]
    private async Task SaveProjectSettingsAsync()
    {
        if (SelectedProject is null)
        {
            await _dialogService.ShowMessageAsync("Projeto não selecionado", "Selecione um projeto na lista antes de executar o scan.");
            return;
        }

        if (IsScanRunning)
        {
            return;
        }

        var project = await _dbContext.Projects.Include(p => p.Commands).FirstAsync(p => p.Id == SelectedProject.Id);
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        await ReloadProjectsAsync(project.Id, selectFirstWhenMissing: true);
    }

    [RelayCommand]
    private async Task AddGitIgnoreEntriesAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(".gitignore", "Adicionar entradas de .security/trivy ao .gitignore do projeto?");
        if (!confirmed)
        {
            return;
        }

        var gitignore = Path.Combine(SelectedProject.Path, ".gitignore");
        var entries = new[]
        {
            ".security/trivy/latest.json",
            ".security/trivy/history/",
            ".security/trivy/logs/",
            ".security/trivy/sbom/"
        };
        var current = File.Exists(gitignore) ? await File.ReadAllTextAsync(gitignore) : string.Empty;
        var missing = entries.Where(entry => !current.Contains(entry, StringComparison.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
        {
            await File.AppendAllTextAsync(gitignore, Environment.NewLine + string.Join(Environment.NewLine, missing) + Environment.NewLine);
        }
    }

    [RelayCommand]
    private async Task OpenUrlAsync(FindingRowViewModel finding)
    {
        if (!string.IsNullOrWhiteSpace(finding.PrimaryUrl))
        {
            await _externalLinkService.OpenAsync(finding.PrimaryUrl);
        }
    }

    private async Task RunScanAsync(ScanMode mode)
    {
        if (SelectedProject is null)
        {
            return;
        }

        Logs.Clear();
        IsScanRunning = true;
        _scanCancellation = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(value => Dispatcher.UIThread.Post(() => ProgressText = value.Message));
        var logs = new Progress<ProcessLogLine>(line => Dispatcher.UIThread.Post(() => Logs.Add($"{line.At:HH:mm:ss} {line.Stream}: {line.Message}")));

        try
        {
            await _scanOrchestrator.RunAsync(SelectedProject.Id, mode, progress, logs, _scanCancellation.Token);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Erro no scan", ex.Message);
        }
        finally
        {
            var selectedProjectId = SelectedProject?.Id;
            _scanCancellation.Dispose();
            _scanCancellation = null;
            IsScanRunning = false;
            await ReloadProjectsAsync(selectedProjectId, selectFirstWhenMissing: true);
        }
    }

    private async Task ReloadProjectsAsync(Guid? preferredProjectId = null, bool selectFirstWhenMissing = false)
    {
        var selectedId = preferredProjectId ?? SelectedProject?.Id;
        var projects = await _dbContext.Projects
            .Include(project => project.Scans)
            .Where(project => project.IsActive)
            .OrderBy(project => project.Name)
            .ToListAsync();
        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(new ProjectCardViewModel(project));
        }

        var nextSelection = selectedId.HasValue
            ? Projects.FirstOrDefault(project => project.Id == selectedId.Value)
            : null;

        if (nextSelection is null && selectFirstWhenMissing)
        {
            nextSelection = Projects.FirstOrDefault();
        }

        SelectedProject = nextSelection;
    }

    private async Task LoadProjectDetailsAsync(Guid projectId)
    {
        Findings.Clear();
        MisconfigurationFindings.Clear();
        SecretFindings.Clear();
        Scans.Clear();
        Commands.Clear();
        _allFindings = [];
        NotifyCollectionStateChanged();

        if (projectId == Guid.Empty)
        {
            return;
        }

        var project = await _dbContext.Projects
            .Include(p => p.Commands)
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
        {
            return;
        }

        foreach (var command in project.Commands.OrderBy(command => command.ExecutionOrder))
        {
            Commands.Add(command);
        }

        foreach (var scan in project.Scans.OrderByDescending(scan => scan.StartedAt))
        {
            Scans.Add(new ScanRowViewModel(scan, project.Name));
        }
        NotifyCollectionStateChanged();

        var lastScanId = project.Scans.Where(scan => scan.Status == ScanStatus.Succeeded).OrderByDescending(scan => scan.StartedAt).FirstOrDefault()?.Id;
        if (lastScanId is null)
        {
            return;
        }

        var findings = await _dbContext.Findings
            .Include(f => f.References)
            .Include(f => f.Occurrences)
            .Where(f => f.ScanId == lastScanId)
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.PackageName)
            .ToListAsync();
        _allFindings = findings.Select(finding => new FindingRowViewModel(finding)).ToList();
        ApplyFilters();
        NotifyCountersChanged();
    }

    private void ApplyFilters()
    {
        var selectedSeverity = TryGetSeverityFilter(SelectedSeverity);
        var selectedStatus = TryGetStatusFilter(SelectedStatus);
        var filtered = _allFindings.Where(finding =>
            (selectedSeverity is null || finding.SeverityValue == selectedSeverity)
            && (selectedStatus is null || finding.StatusValue == selectedStatus)
            && (string.IsNullOrWhiteSpace(PackageFilter) || finding.Package.Contains(PackageFilter, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(IdFilter) || finding.Vulnerability.Contains(IdFilter, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(TargetFilter) || finding.Target.Contains(TargetFilter, StringComparison.OrdinalIgnoreCase))
            && (!OnlyWithFix || finding.HasFix));

        Findings.Clear();
        foreach (var finding in filtered)
        {
            Findings.Add(finding);
        }

        MisconfigurationFindings.Clear();
        foreach (var finding in _allFindings.Where(f => f.TypeValue == FindingType.Misconfiguration))
        {
            MisconfigurationFindings.Add(finding);
        }

        SecretFindings.Clear();
        foreach (var finding in _allFindings.Where(f => f.TypeValue == FindingType.Secret))
        {
            SecretFindings.Add(finding);
        }

        NotifyCollectionStateChanged();
    }

    private void NotifyCountersChanged()
    {
        OnPropertyChanged(nameof(CriticalCount));
        OnPropertyChanged(nameof(HighCount));
        OnPropertyChanged(nameof(MediumCount));
        OnPropertyChanged(nameof(LowCount));
        OnPropertyChanged(nameof(UnknownCount));
        OnPropertyChanged(nameof(UniqueVulnerabilityCount));
        OnPropertyChanged(nameof(OccurrenceCount));
        OnPropertyChanged(nameof(MisconfigurationCount));
        OnPropertyChanged(nameof(SecretCount));
        OnPropertyChanged(nameof(NewCount));
        OnPropertyChanged(nameof(ResolvedCount));
        OnPropertyChanged(nameof(RegressionCount));
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasFindings));
        OnPropertyChanged(nameof(HasNoFindings));
        OnPropertyChanged(nameof(HasMisconfigurationFindings));
        OnPropertyChanged(nameof(HasNoMisconfigurationFindings));
        OnPropertyChanged(nameof(HasSecretFindings));
        OnPropertyChanged(nameof(HasNoSecretFindings));
        OnPropertyChanged(nameof(HasScans));
        OnPropertyChanged(nameof(HasNoScans));
    }

    private static FindingSeverity? TryGetSeverityFilter(string value)
    {
        foreach (var severity in Enum.GetValues<FindingSeverity>())
        {
            if (DisplayTextService.Severity(severity).Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return severity;
            }
        }

        return null;
    }

    private static FindingLifecycleStatus? TryGetStatusFilter(string value)
    {
        foreach (var status in Enum.GetValues<FindingLifecycleStatus>())
        {
            if (DisplayTextService.LifecycleStatus(status).Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }
        }

        return null;
    }
}
