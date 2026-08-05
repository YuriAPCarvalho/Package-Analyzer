using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class ScanOrchestrator(
    TrivyProjectManagerDbContext dbContext,
    IProcessRunner processRunner,
    ITrivyService trivyService,
    ITrivyReportParser reportParser,
    TrivyReportRedactionService redactionService,
    IScanComparisonService comparisonService,
    IDependencyAnalysisService dependencyAnalysisService,
    IVulnerabilityEnrichmentService vulnerabilityEnrichmentService,
    IAppSettingsService appSettingsService,
    IStoragePathService storagePathService,
    IRetentionService retentionService,
    ILogger<ScanOrchestrator> logger) : IScanOrchestrator
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly HashSet<Guid> RunningProjects = [];

    public async Task<ScanExecutionResult> RunAsync(Guid projectId, ScanMode mode, IProgress<ScanProgress>? progress = null, IProgress<ProcessLogLine>? logs = null, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (!RunningProjects.Add(projectId))
            {
                throw new InvalidOperationException("A scan is already running for this project.");
            }
        }
        finally
        {
            Gate.Release();
        }

        var capturedLogs = new List<ProcessLogLine>();
        var combinedProgress = new Progress<ProcessLogLine>(line =>
        {
            capturedLogs.Add(line);
            logs?.Report(line);
        });

        try
        {
            return await RunCoreAsync(projectId, mode, progress, combinedProgress, capturedLogs, cancellationToken);
        }
        finally
        {
            await Gate.WaitAsync(CancellationToken.None);
            try
            {
                RunningProjects.Remove(projectId);
            }
            finally
            {
                Gate.Release();
            }
        }
    }

    private async Task<ScanExecutionResult> RunCoreAsync(Guid projectId, ScanMode mode, IProgress<ScanProgress>? progress, IProgress<ProcessLogLine> logs, List<ProcessLogLine> capturedLogs, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.Include(p => p.Commands).FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Project was not found.");

        if (!Directory.Exists(project.Path))
        {
            throw new DirectoryNotFoundException($"Project folder no longer exists: {project.Path}");
        }

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        var scan = new Scan
        {
            ProjectId = project.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ScanStatus.Running
        };
        scan.RawReportPath = storagePathService.GetReportPath(project, scan.Id);
        scan.LogPath = storagePathService.GetLogPath(project, scan.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(scan.RawReportPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(scan.LogPath)!);

        dbContext.Scans.Add(scan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var enabledCommands = mode == ScanMode.Full
            ? project.Commands.Where(command => command.IsEnabled).OrderBy(command => command.ExecutionOrder).ToList()
            : [];
        var totalSteps = enabledCommands.Count + 2;
        var step = 1;

        try
        {
            foreach (var command in enabledCommands)
            {
                progress?.Report(new ScanProgress(command.Name, CommandExecutionStatus.Running, step, totalSteps, $"{step} de {totalSteps} - {command.Name}"));
                var errors = CommandValidationService.Validate(command);
                if (errors.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                }

                var request = new ProcessRequest(
                    command.Command,
                    ArgumentSplitter.Split(command.Arguments),
                    string.IsNullOrWhiteSpace(command.WorkingDirectory) ? project.Path : command.WorkingDirectory!,
                    TimeSpan.FromSeconds(settings.DefaultTimeoutSeconds));
                var result = await processRunner.RunAsync(request, logs, cancellationToken);
                await AppendLogAsync(scan.LogPath, capturedLogs, cancellationToken);
                if (!result.Succeeded && !command.ContinueOnError)
                {
                    throw new InvalidOperationException($"Command '{command.Name}' failed with exit code {result.ExitCode}.");
                }

                progress?.Report(new ScanProgress(command.Name, result.Status, step++, totalSteps, command.Name));
            }

            progress?.Report(new ScanProgress("Trivy scan", CommandExecutionStatus.Running, step, totalSteps, $"{step} de {totalSteps} - Executando Trivy"));
            var trivyOptions = new TrivyOptions
            {
                Scanners = settings.Scanners,
                TrivyPath = settings.TrivyPath,
                Severities = settings.Severities,
                IgnoreUnfixed = settings.IgnoreUnfixed,
                Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutSeconds),
                SkipDirectories = settings.SkipDirectories
            };
            var trivyResult = await trivyService.ScanFileSystemAsync(project.Path, scan.RawReportPath, trivyOptions, logs, cancellationToken);
            await AppendLogAsync(scan.LogPath, capturedLogs, cancellationToken);
            if (!trivyResult.Succeeded)
            {
                throw new InvalidOperationException($"Trivy failed with exit code {trivyResult.ExitCode}.");
            }

            scan.TrivyVersion = await trivyService.GetVersionAsync(settings.TrivyPath, cancellationToken);
            progress?.Report(new ScanProgress("Parsing", CommandExecutionStatus.Running, totalSteps, totalSteps, $"{totalSteps} de {totalSteps} - Processando relatório"));
            await redactionService.RedactSecretsAsync(scan.RawReportPath, cancellationToken);
            var findings = (await reportParser.ParseAsync(scan.RawReportPath, cancellationToken)).ToList();
            await dependencyAnalysisService.AnalyzeAsync(project, findings, cancellationToken);
            await EnrichFindingsAsync(findings, cancellationToken);
            var previousScans = await dbContext.Scans
                .AsNoTracking()
                .Include(s => s.Findings)
                .Where(s => s.ProjectId == project.Id && s.Id != scan.Id && s.Status == ScanStatus.Succeeded)
                .ToListAsync(cancellationToken);
            var previousScan = previousScans.OrderByDescending(s => s.StartedAt).FirstOrDefault();
            var olderFindings = await dbContext.Findings
                .AsNoTracking()
                .Where(f => f.Scan!.ProjectId == project.Id && f.ScanId != scan.Id)
                .ToListAsync(cancellationToken);
            comparisonService.Classify(findings, previousScan?.Findings ?? [], olderFindings);
            await ApplySecurityExceptionsAsync(project.Id, findings, cancellationToken);

            var counters = FindingCounterService.Calculate(findings);
            scan.CriticalCount = counters.Critical;
            scan.HighCount = counters.High;
            scan.MediumCount = counters.Medium;
            scan.LowCount = counters.Low;
            scan.UnknownCount = counters.Unknown;
            scan.MisconfigurationCount = counters.Misconfigurations;
            scan.SecretCount = counters.Secrets;
            scan.UniqueVulnerabilityCount = counters.UniqueVulnerabilities;
            scan.TotalOccurrenceCount = counters.TotalOccurrences;
            scan.NewCount = findings.Count(f => f.Status == FindingLifecycleStatus.New);
            scan.ExistingCount = findings.Count(f => f.Status == FindingLifecycleStatus.Existing);
            scan.RegressionCount = findings.Count(f => f.Status == FindingLifecycleStatus.Regression);
            scan.ResolvedCount = previousScan?.Findings.Count(previous => findings.All(current => current.FindingKey != previous.FindingKey)) ?? 0;
            PrepareFindingsForInsert(scan.Id, findings);
            scan.Status = ScanStatus.Succeeded;
            scan.FinishedAt = DateTimeOffset.UtcNow;
            project.LastScanAt = scan.FinishedAt;
            project.UpdatedAt = DateTimeOffset.UtcNow;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Findings.AddRange(findings);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await retentionService.ApplyAsync(project.Id, settings.MaxHistoryPerProject, cancellationToken);
            progress?.Report(new ScanProgress("Persistência", CommandExecutionStatus.Succeeded, totalSteps, totalSteps, "Scan concluído"));
            return new ScanExecutionResult(scan, capturedLogs);
        }
        catch (OperationCanceledException)
        {
            DetachPendingFindingGraph();
            scan.Status = ScanStatus.Cancelled;
            scan.FinishedAt = DateTimeOffset.UtcNow;
            scan.ErrorMessage = "Scan cancelled by user.";
            await dbContext.SaveChangesAsync(CancellationToken.None);
            progress?.Report(new ScanProgress("Cancelamento", CommandExecutionStatus.Cancelled, totalSteps, totalSteps, "Scan cancelado"));
            return new ScanExecutionResult(scan, capturedLogs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scan failed for project {ProjectId}", project.Id);
            DetachPendingFindingGraph();
            scan.Status = ScanStatus.Failed;
            scan.FinishedAt = DateTimeOffset.UtcNow;
            scan.ErrorMessage = ex.Message;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            progress?.Report(new ScanProgress("Erro", CommandExecutionStatus.Failed, totalSteps, totalSteps, ex.Message));
            return new ScanExecutionResult(scan, capturedLogs);
        }
    }

    private static async Task AppendLogAsync(string? path, IReadOnlyCollection<ProcessLogLine> lines, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var content = string.Join(Environment.NewLine, lines.Select(line => $"[{line.At:O}] {line.Stream}: {line.Message}"));
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    private static void PrepareFindingsForInsert(Guid scanId, IEnumerable<Finding> findings)
    {
        foreach (var finding in findings)
        {
            finding.ScanId = scanId;
            finding.Scan = null;

            foreach (var reference in finding.References)
            {
                reference.FindingId = finding.Id;
                reference.Finding = null;
            }

            foreach (var occurrence in finding.Occurrences)
            {
                occurrence.FindingId = finding.Id;
                occurrence.Finding = null;
            }
        }
    }

    private async Task EnrichFindingsAsync(IEnumerable<Finding> findings, CancellationToken cancellationToken)
    {
        foreach (var finding in findings.Where(finding => finding.FindingType == FindingType.Vulnerability && !string.IsNullOrWhiteSpace(finding.VulnerabilityId)))
        {
            var enrichment = await vulnerabilityEnrichmentService.TryEnrichAsync(finding.VulnerabilityId!, cancellationToken);
            if (enrichment is null)
            {
                continue;
            }

            finding.CvssScore ??= enrichment.CvssScore;
            finding.CvssVector ??= enrichment.CvssVector;
            finding.CvssSource ??= enrichment.Source;
            finding.CweIds ??= enrichment.CweIds.Count == 0 ? null : string.Join(", ", enrichment.CweIds);
            finding.EnrichmentSource = enrichment.Source;
            finding.EnrichedAt = enrichment.RetrievedAt;
        }
    }

    private async Task ApplySecurityExceptionsAsync(Guid projectId, IEnumerable<Finding> findings, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var exceptions = await dbContext.SecurityExceptions
            .AsNoTracking()
            .Where(exception => exception.ProjectId == projectId
                && exception.IsActive
                && (exception.ExpiresAt == null || exception.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        foreach (var finding in findings)
        {
            if (exceptions.Any(exception => MatchesException(exception, finding)))
            {
                finding.Status = FindingLifecycleStatus.Ignored;
            }
        }
    }

    private static bool MatchesException(Domain.Entities.SecurityException exception, Finding finding)
    {
        if (!string.IsNullOrWhiteSpace(exception.FindingKey)
            && exception.FindingKey.Equals(finding.FindingKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (string.IsNullOrWhiteSpace(exception.VulnerabilityId)
                || exception.VulnerabilityId.Equals(finding.VulnerabilityId, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(exception.PackageName)
                || exception.PackageName.Equals(finding.PackageName, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(exception.InstalledVersion)
                || exception.InstalledVersion.Equals(finding.InstalledVersion, StringComparison.OrdinalIgnoreCase));
    }

    private void DetachPendingFindingGraph()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
            .Where(entry => entry.Entity is Finding or FindingReference or FindingOccurrence)
            .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
