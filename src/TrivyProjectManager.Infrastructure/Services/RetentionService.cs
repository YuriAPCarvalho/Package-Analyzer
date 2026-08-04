using Microsoft.EntityFrameworkCore;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Infrastructure.Data;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class RetentionService(TrivyProjectManagerDbContext dbContext) : IRetentionService
{
    public async Task ApplyAsync(Guid projectId, int maxHistory, CancellationToken cancellationToken = default)
    {
        if (maxHistory <= 0)
        {
            return;
        }

        var scans = await dbContext.Scans
            .Where(scan => scan.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var scansToDelete = scans
            .OrderByDescending(scan => scan.StartedAt)
            .Skip(maxHistory)
            .ToList();

        foreach (var scan in scansToDelete)
        {
            DeleteIfExists(scan.RawReportPath);
            DeleteIfExists(scan.LogPath);
        }

        dbContext.Scans.RemoveRange(scansToDelete);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void DeleteIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }
}
