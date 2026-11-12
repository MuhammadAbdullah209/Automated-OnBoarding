using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Deletes projects whose countdown has finished. The database removes the setup answers, gateway fields,
    /// provisioning details and document rows with the project (cascade); the stored files are deleted here.
    /// The ActivityLogs table has no link to projects, so a project's history stays and gets a final entry.
    /// </summary>
    public class ProjectPurgeService(
        OnboardingDbContext context,
        IFileStorage fileStorage,
        IActivityLogService activityLog,
        ILogger<ProjectPurgeService> logger) : IProjectPurgeService
    {
        public async Task<int> PurgeDueAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var due = await context.Projects
                .Include(p => p.Documents)
                .Where(p => p.DeletionScheduledFor != null && p.DeletionScheduledFor <= now)
                .ToListAsync(cancellationToken);

            var deleted = 0;
            foreach (var project in due)
            {
                var customerId = project.CustomerId;
                var projectId = project.Id;
                var name = project.Name;
                var filePaths = project.Documents.Select(d => d.StoredFilePath).ToList();

                try
                {
                    context.Projects.Remove(project);
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Leave it scheduled: the next run tries again.
                    logger.LogError(ex, "Could not delete project {ProjectId}.", projectId);
                    context.ChangeTracker.Clear();
                    continue;
                }

                deleted++;
                await activityLog.LogProjectAsync(customerId, projectId, "Project deleted",
                    $"\"{name}\" and all of its data were permanently deleted.");

                // Files last: a leftover file is harmless, a missing file for an existing project is not.
                foreach (var path in filePaths)
                {
                    try
                    {
                        await fileStorage.DeleteAsync(path);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not delete file {Path} of deleted project {ProjectId}.", path, projectId);
                    }
                }
            }

            return deleted;
        }
    }
}
