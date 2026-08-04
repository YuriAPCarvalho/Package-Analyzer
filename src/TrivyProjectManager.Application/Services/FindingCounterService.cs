using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public static class FindingCounterService
{
    public static FindingCounters Calculate(IEnumerable<Finding> findings)
    {
        var list = findings.ToList();
        return new FindingCounters(
            Critical: list.Count(f => f.Severity == FindingSeverity.Critical),
            High: list.Count(f => f.Severity == FindingSeverity.High),
            Medium: list.Count(f => f.Severity == FindingSeverity.Medium),
            Low: list.Count(f => f.Severity == FindingSeverity.Low),
            Unknown: list.Count(f => f.Severity == FindingSeverity.Unknown),
            Misconfigurations: list.Count(f => f.FindingType == FindingType.Misconfiguration),
            Secrets: list.Count(f => f.FindingType == FindingType.Secret),
            UniqueVulnerabilities: list.Count(f => f.FindingType == FindingType.Vulnerability),
            TotalOccurrences: list.Sum(f => Math.Max(1, f.Occurrences.Count)));
    }
}
