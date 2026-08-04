using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Domain.Entities;

public sealed class Scan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public string? TrivyVersion { get; set; }
    public DateTimeOffset? TrivyDatabaseUpdatedAt { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int UnknownCount { get; set; }
    public int MisconfigurationCount { get; set; }
    public int SecretCount { get; set; }
    public int UniqueVulnerabilityCount { get; set; }
    public int TotalOccurrenceCount { get; set; }
    public int NewCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ExistingCount { get; set; }
    public int RegressionCount { get; set; }
    public string? RawReportPath { get; set; }
    public string? LogPath { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Finding> Findings { get; set; } = [];
}
