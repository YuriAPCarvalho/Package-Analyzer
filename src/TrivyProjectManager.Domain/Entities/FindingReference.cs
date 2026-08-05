using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Domain.Entities;

public sealed class FindingReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FindingId { get; set; }
    public Finding? Finding { get; set; }
    public string Url { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public FindingReferenceSourceKind SourceKind { get; set; } = FindingReferenceSourceKind.Other;
    public bool IsPrimary { get; set; }
}
