namespace TrivyProjectManager.Application.DTOs;

public sealed class TrivyOptions
{
    public string Scanners { get; set; } = "vuln,misconfig,secret";
    public string Severities { get; set; } = "UNKNOWN,LOW,MEDIUM,HIGH,CRITICAL";
    public bool IgnoreUnfixed { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> SkipDirectories { get; set; } = [".git", "node_modules", ".next"];
}
