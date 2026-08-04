using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Abstractions;

public interface ITrivyReportParser
{
    Task<IReadOnlyList<Finding>> ParseAsync(string reportPath, CancellationToken cancellationToken = default);
}
