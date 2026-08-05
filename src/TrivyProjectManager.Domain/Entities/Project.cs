using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ProjectTechnology Technology { get; set; }
    public PackageManagerType PackageManager { get; set; }
    public ReportStorageMode StorageMode { get; set; } = ReportStorageMode.Central;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastScanAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoDetectPreparation { get; set; } = true;
    public bool IsPreparationTrusted { get; set; }
    public List<ProjectCommand> Commands { get; set; } = [];
    public List<Scan> Scans { get; set; } = [];
    public List<SecurityException> SecurityExceptions { get; set; } = [];
}
