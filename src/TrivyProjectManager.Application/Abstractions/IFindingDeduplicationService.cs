using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Abstractions;

public interface IFindingDeduplicationService
{
    IReadOnlyList<Finding> Deduplicate(IEnumerable<Finding> findings);
}
