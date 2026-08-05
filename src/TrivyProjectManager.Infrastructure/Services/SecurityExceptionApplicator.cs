using Microsoft.EntityFrameworkCore;
using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class SecurityExceptionApplicator(TrivyProjectManagerDbContext dbContext)
{
    public async Task ApplyAsync(Guid projectId, IEnumerable<Finding> findings, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var exceptions = await dbContext.SecurityExceptions
            .AsNoTracking()
            .Where(exception => exception.ProjectId == projectId && exception.IsActive)
            .ToListAsync(cancellationToken);
        exceptions = exceptions.Where(exception => exception.ExpiresAt == null || exception.ExpiresAt > now).ToList();

        foreach (var finding in findings)
        {
            if (exceptions.Any(exception => Matches(exception, finding)))
            {
                finding.Status = FindingLifecycleStatus.Ignored;
            }
        }
    }

    private static bool Matches(Domain.Entities.SecurityException exception, Finding finding)
    {
        if (!string.IsNullOrWhiteSpace(exception.FindingKey)
            && exception.FindingKey.Equals(finding.FindingKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (string.IsNullOrWhiteSpace(exception.VulnerabilityId)
                || exception.VulnerabilityId.Equals(finding.VulnerabilityId, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(exception.PackageName)
                || exception.PackageName.Equals(finding.PackageName, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(exception.InstalledVersion)
                || exception.InstalledVersion.Equals(finding.InstalledVersion, StringComparison.OrdinalIgnoreCase));
    }
}
