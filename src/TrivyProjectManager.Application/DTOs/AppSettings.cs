namespace TrivyProjectManager.Application.DTOs;

public sealed class AppSettings
{
    public string ApplicationName { get; set; } = "Package Analyzer";
    public string? TrivyPath { get; set; }
    public int DefaultTimeoutSeconds { get; set; } = 1800;
    public string? StorageDirectory { get; set; }
    public string Scanners { get; set; } = "vuln,misconfig,secret";
    public string Severities { get; set; } = "UNKNOWN,LOW,MEDIUM,HIGH,CRITICAL";
    public bool IgnoreUnfixed { get; set; }
    public bool EnableVulnerabilityEnrichment { get; set; }
    public bool EnableNvdEnrichment { get; set; }
    public bool EnableOsvEnrichment { get; set; }
    public bool EnableGitHubAdvisoryEnrichment { get; set; }
    public string? GitHubAdvisoryToken { get; set; }
    public string Theme { get; set; } = "System";
    public int MaxHistoryPerProject { get; set; } = 50;
    public DateTimeOffset? LastApplicationUpdateCheckUtc { get; set; }
    public string LastApplicationUpdateStatus { get; set; } = "Idle";
    public string ApplicationUpdateChannel { get; set; } = "stable";
    public List<string> SkipDirectories { get; set; } = [".git", "node_modules", ".next"];
}
