using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class ScanComparisonService : IScanComparisonService
{
    public void Classify(IReadOnlyCollection<Finding> currentFindings, IReadOnlyCollection<Finding> previousFindings, IReadOnlyCollection<Finding> resolvedHistory)
    {
        var previousKeys = previousFindings.Select(f => f.FindingKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resolvedKeys = resolvedHistory.Select(f => f.FindingKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var current in currentFindings)
        {
            if (previousKeys.Contains(current.FindingKey))
            {
                current.Status = FindingLifecycleStatus.Existing;
            }
            else if (resolvedKeys.Contains(current.FindingKey))
            {
                current.Status = FindingLifecycleStatus.Regression;
            }
            else
            {
                current.Status = FindingLifecycleStatus.New;
            }
        }

        var currentKeys = currentFindings.Select(f => f.FindingKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var previous in previousFindings.Where(previous => !currentKeys.Contains(previous.FindingKey)))
        {
            previous.Status = FindingLifecycleStatus.Resolved;
        }
    }
}
