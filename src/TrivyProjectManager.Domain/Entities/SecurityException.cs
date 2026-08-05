namespace TrivyProjectManager.Domain.Entities;

public sealed class SecurityException
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public string? FindingKey { get; set; }
    public string? VulnerabilityId { get; set; }
    public string? PackageName { get; set; }
    public string? InstalledVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = Environment.UserName;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
