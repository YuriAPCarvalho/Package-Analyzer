namespace TrivyProjectManager.Domain.Entities;

public sealed class FindingOccurrence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FindingId { get; set; }
    public Finding? Finding { get; set; }
    public string? Target { get; set; }
    public string? FilePath { get; set; }
    public string? RelativePath { get; set; }
    public string? AbsolutePath { get; set; }
    public string? ProjectFilePath { get; set; }
    public string? ProjectName { get; set; }
}
