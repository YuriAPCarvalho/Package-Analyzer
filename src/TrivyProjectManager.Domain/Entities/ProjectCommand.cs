namespace TrivyProjectManager.Domain.Entities;

public sealed class ProjectCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool ContinueOnError { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? PreparationTargetKey { get; set; }
}
