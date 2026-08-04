namespace TrivyProjectManager.Application.DTOs;

public sealed class AppSettings
{
    public string ApplicationName { get; set; } = "Package-Analyzer by: YuriAPCarvalho";
    public string? TrivyPath { get; set; }
    public bool AutoInstallTrivy { get; set; } = true;
    public bool AutoUpdateTrivyOnStartup { get; set; } = true;
    public int DefaultTimeoutSeconds { get; set; } = 1800;
    public string? StorageDirectory { get; set; }
    public string Scanners { get; set; } = "vuln,misconfig,secret";
    public string Severities { get; set; } = "UNKNOWN,LOW,MEDIUM,HIGH,CRITICAL";
    public bool IgnoreUnfixed { get; set; }
    public string Theme { get; set; } = "System";
    public int MaxHistoryPerProject { get; set; } = 50;
    public List<string> SkipDirectories { get; set; } = [".git", "node_modules", ".next"];
}
