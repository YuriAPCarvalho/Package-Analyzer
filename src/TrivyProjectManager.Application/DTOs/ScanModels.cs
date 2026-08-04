using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.DTOs;

public sealed record ScanProgress(
    string StepName,
    CommandExecutionStatus Status,
    int StepNumber,
    int TotalSteps,
    string Message);

public sealed record ScanExecutionResult(Scan Scan, IReadOnlyList<ProcessLogLine> Logs);

public sealed record FindingCounters(
    int Critical,
    int High,
    int Medium,
    int Low,
    int Unknown,
    int Misconfigurations,
    int Secrets,
    int UniqueVulnerabilities,
    int TotalOccurrences);
