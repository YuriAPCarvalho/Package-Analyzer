using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Abstractions;

public interface IScanComparisonService
{
    void Classify(IReadOnlyCollection<Finding> currentFindings, IReadOnlyCollection<Finding> previousFindings, IReadOnlyCollection<Finding> resolvedHistory);
}
