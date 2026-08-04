using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Domain.Entities;

public sealed class Finding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScanId { get; set; }
    public Scan? Scan { get; set; }
    public string FindingKey { get; set; } = string.Empty;
    public FindingType FindingType { get; set; }
    public string? Target { get; set; }
    public string? VulnerabilityId { get; set; }
    public string? PackageName { get; set; }
    public string? PackagePath { get; set; }
    public string? InstalledVersion { get; set; }
    public string? FixedVersion { get; set; }
    public FindingSeverity Severity { get; set; }
    public FindingLifecycleStatus Status { get; set; } = FindingLifecycleStatus.New;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PrimaryUrl { get; set; }
    public DateTimeOffset? PublishedDate { get; set; }
    public DateTimeOffset? LastModifiedDate { get; set; }
    public string? FilePath { get; set; }
    public int? StartLine { get; set; }
    public string? MaskedCodeSnippet { get; set; }
    public List<FindingReference> References { get; set; } = [];
    public List<FindingOccurrence> Occurrences { get; set; } = [];
}
