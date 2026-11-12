using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Activity;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    public class ActivityLogService(
        OnboardingDbContext context,
        ICurrentActor actor,
        ILogger<ActivityLogService> logger) : IActivityLogService
    {
        public Task LogCustomerAsync(Guid customerId, string action, string? reason = null)
            => AddAsync(customerId, null, ActivityTarget.Customer, action, reason);

        public Task LogProjectAsync(Guid customerId, Guid projectId, string action, string? reason = null)
            => AddAsync(customerId, projectId, ActivityTarget.Project, action, reason);

        public async Task<IReadOnlyList<ActivityLogDto>> GetForCustomerAsync(Guid customerId, int take = 20)
            => await Query(context.ActivityLogs.AsNoTracking().Where(a => a.CustomerId == customerId), take);

        public async Task<IReadOnlyList<ActivityLogDto>> GetForProjectAsync(Guid customerId, Guid projectId, int? take = null)
            => await Query(context.ActivityLogs.AsNoTracking()
                .Where(a => a.CustomerId == customerId && a.ProjectId == projectId), take);

        private async Task<IReadOnlyList<ActivityLogDto>> Query(IQueryable<ActivityLog> source, int? take)
        {
            IQueryable<ActivityLog> query = source.OrderByDescending(a => a.CreatedAt);
            if (take is int limit)
                query = query.Take(limit);

            return await query
                .Select(a => new ActivityLogDto
                {
                    Target = a.Target,
                    ProjectId = a.ProjectId,
                    ProjectName = context.Projects.Where(p => p.Id == a.ProjectId).Select(p => p.Name).FirstOrDefault(),
                    Action = a.Action,
                    Reason = a.Reason,
                    ActorName = a.ActorName,
                    ActorRole = a.ActorRole,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }

        private async Task AddAsync(Guid customerId, Guid? projectId, ActivityTarget target, string action, string? reason)
        {
            try
            {
                context.ActivityLogs.Add(new ActivityLog
                {
                    CustomerId = customerId,
                    ProjectId = projectId,
                    Target = target,
                    Action = Truncate(action, 150)!,
                    Reason = Truncate(reason, 1000),
                    ActorUserAuthId = actor.UserAuthId,
                    ActorName = Truncate(actor.Name, 200),
                    ActorRole = Truncate(actor.Role, 30)
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record activity {Action} for customer {CustomerId}.", action, customerId);
            }
        }

        private static string? Truncate(string? value, int max)
            => value is { Length: > 0 } && value.Length > max ? value[..max] : value;
    }
}
