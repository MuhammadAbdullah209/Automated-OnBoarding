using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using System.Linq.Expressions;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Project (onboarding case) data access.</summary>
    public class ProjectRepository(OnboardingDbContext context) : IProjectRepository
    {
        /// <summary>
        /// When anything last happened on the project: the newest of its own last change, its creation, and its
        /// newest activity-log entry (e.g. an information request raised by staff). Project lists sort on this,
        /// newest first, so a project moves to the top as soon as someone acts on it.
        /// </summary>
        private Expression<Func<Project, DateTimeOffset>> LastActivityAt => p =>
            context.ActivityLogs.Where(a => a.ProjectId == p.Id).Max(a => (DateTimeOffset?)a.CreatedAt) > (p.UpdatedAt ?? p.CreatedAt)
                ? context.ActivityLogs.Where(a => a.ProjectId == p.Id).Max(a => (DateTimeOffset?)a.CreatedAt)!.Value
                : p.UpdatedAt ?? p.CreatedAt;

        private IOrderedQueryable<Project> OrderByLastActivity(IQueryable<Project> q)
            => q.OrderByDescending(LastActivityAt).ThenByDescending(p => p.CreatedAt);

        public async Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId)
            => await OrderByLastActivity(context.Projects.AsNoTracking()
                .Where(p => p.CustomerId == customerId))
                .ToListAsync();

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ProjectStatus>>> GetProjectStatusesByCustomerIdsAsync(
            IReadOnlyList<Guid> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<Guid, IReadOnlyList<ProjectStatus>>();
            }

            var rows = await context.Projects.AsNoTracking()
                .Where(p => customerIds.Contains(p.CustomerId))
                .Select(p => new { p.CustomerId, p.Status })
                .ToListAsync();

            return rows
                .GroupBy(r => r.CustomerId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ProjectStatus>)g.Select(x => x.Status).ToList());
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAsync(Guid customerId, ProjectListQuery query)
        {
            IQueryable<Project> q = context.Projects.AsNoTracking()
                .Include(p => p.InformationRequests)
                .Include(p => p.Discovery)
                .Where(p => p.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
            }

            q = ApplyCommonFilters(q, query);
            return await PageAsync(q, query);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAllAsync(ProjectListQuery query)
        {
            IQueryable<Project> q = context.Projects.AsNoTracking()
                .Include(p => p.Customer)
                .Include(p => p.Discovery)
                .Include(p => p.InformationRequests);

            if (query.CustomerId is Guid customerId)
            {
                q = q.Where(p => p.CustomerId == customerId);
            }

            if (query.PartnerId is Guid partnerId)
            {
                q = q.Where(p => p.Customer.PartnerId == partnerId);
            }

            // Staff search by project or company name.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(p => EF.Functions.Like(p.Name, $"%{term}%")
                    || EF.Functions.Like(p.Customer.CompanyName ?? "", $"%{term}%"));
            }

            q = ApplyCommonFilters(q, query);
            return await PageAsync(q, query);
        }

        private static IQueryable<Project> ApplyCommonFilters(IQueryable<Project> q, ProjectListQuery query)
        {
            if (query.Status is ProjectStatus status)
            {
                q = q.Where(p => p.Status == status);
            }

            if (query.Step is ProjectStep step)
            {
                q = q.Where(p => p.CurrentStep == step);
            }

            // Days are whole UTC days; "to" includes the whole of that day.
            if (query.CreatedFrom is DateOnly from)
            {
                var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                q = q.Where(p => p.CreatedAt >= start);
            }

            if (query.CreatedTo is DateOnly to)
            {
                var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                q = q.Where(p => p.CreatedAt < end);
            }

            if (query.View is ProjectListView view && view != ProjectListView.All)
            {
                q = view switch
                {
                    ProjectListView.NeedsYou => q.Where(p =>
                        p.Status == ProjectStatus.InProgress
                        || p.Status == ProjectStatus.Draft
                        || p.InformationRequests.Any(r => r.Status == InformationRequestStatus.Requested)
                        || (p.Status == ProjectStatus.AccountCreation && p.UsersCsvRequestedAt != null)),
                    ProjectListView.WithPciPal => q.Where(p =>
                        p.Status == ProjectStatus.Submitted
                        || p.Status == ProjectStatus.Approval
                        || p.Status == ProjectStatus.Provisioning
                        || p.Status == ProjectStatus.AccountCreation),
                    ProjectListView.BeingPrepared => q.Where(p =>
                        p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.Draft),
                    ProjectListView.Live => q.Where(p => p.Status == ProjectStatus.Completed),
                    _ => q
                };
            }

            return q;
        }

        private async Task<(IReadOnlyList<Project> Items, int TotalCount)> PageAsync(
            IQueryable<Project> q,
            ProjectListQuery query)
        {
            var totalCount = await q.CountAsync();

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? ProjectListQuery.DefaultPageSize : query.PageSize;
            if (pageSize > ProjectListQuery.MaxPageSize)
            {
                pageSize = ProjectListQuery.MaxPageSize;
            }

            var items = await OrderByLastActivity(q)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Project?> GetWithConfigurationAsync(Guid customerId, Guid projectId)
            => await context.Projects
                .Include(p => p.Discovery)
                .Include(p => p.TechnicalEnvironment)
                .Include(p => p.PaymentConfiguration).ThenInclude(c => c!.PaymentGateway)
                .Include(p => p.GatewayConfiguration)
                .Include(p => p.GatewayFields).ThenInclude(f => f.PaymentGatewayField)
                .Include(p => p.Documents)
                .Include(p => p.InformationRequests)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == projectId && p.CustomerId == customerId);

        public async Task<Project?> GetForCustomerAsync(Guid customerId, Guid projectId)
            => await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.CustomerId == customerId);

        public Task<Project?> GetByIdAsync(Guid projectId)
            => context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

        public async Task<bool> NameExistsAsync(Guid customerId, string name)
            => await context.Projects.AnyAsync(p => p.CustomerId == customerId && p.Name == name);

        public async Task AddAsync(Project project)
            => await context.Projects.AddAsync(project);

        public async Task<bool> ProvisioningDetailExistsAsync(Guid projectId)
            => await context.ProjectProvisioningDetails.AnyAsync(d => d.ProjectId == projectId);

        public async Task<ProjectProvisioningDetail?> GetProvisioningDetailAsync(Guid projectId)
            => await context.ProjectProvisioningDetails.AsNoTracking().FirstOrDefaultAsync(d => d.ProjectId == projectId);

        public async Task<ProjectProvisioningDetail?> GetProvisioningDetailForUpdateAsync(Guid projectId)
            => await context.ProjectProvisioningDetails.FirstOrDefaultAsync(d => d.ProjectId == projectId);

        public async Task AddProvisioningDetailAsync(ProjectProvisioningDetail detail)
            => await context.ProjectProvisioningDetails.AddAsync(detail);

        public Task<int> CountAsync(Guid? partnerId = null, Guid? customerId = null, CancellationToken cancellationToken = default)
            => ScopedProjects(partnerId, customerId).CountAsync(cancellationToken);

        public async Task<int> CountCustomerUsersAsync(Guid projectId)
            => await context.CustomerUsers.CountAsync(u => u.ProjectId == projectId);

        public async Task<bool> HasOpenInformationRequestAsync(Guid projectId)
            => await context.ProjectInformationRequests.AnyAsync(r =>
                r.ProjectId == projectId && r.Status != InformationRequestStatus.Approved);

        public async Task<(int Requested, int Submitted)> CountInformationRequestsAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default)
        {
            // A project can have several open requests, so count the projects that have one, not the requests.
            var rows = await (
                    from r in context.ProjectInformationRequests.AsNoTracking()
                    join p in ScopedProjects(partnerId, customerId) on r.ProjectId equals p.Id
                    where r.Status == InformationRequestStatus.Requested || r.Status == InformationRequestStatus.Submitted
                    group r by r.Status into g
                    select new { Status = g.Key, Count = g.Select(x => x.ProjectId).Distinct().Count() })
                .ToListAsync(cancellationToken);

            return (
                rows.FirstOrDefault(r => r.Status == InformationRequestStatus.Requested)?.Count ?? 0,
                rows.FirstOrDefault(r => r.Status == InformationRequestStatus.Submitted)?.Count ?? 0);
        }

        public async Task<IReadOnlyDictionary<ProjectStatus, int>> CountByStatusAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default)
        {
            var rows = await ScopedProjects(partnerId, customerId)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(r => r.Status, r => r.Count);
        }

        public async Task<IReadOnlyList<DashboardRecentProjectDto>> GetRecentAsync(
            int take,
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default)
        {
            if (take < 1)
            {
                return Array.Empty<DashboardRecentProjectDto>();
            }

            var showCustomerName = customerId is null;

            var rows = await OrderByLastActivity(ScopedProjects(partnerId, customerId))
                .Take(take)
                .Select(p => new DashboardRecentProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    CurrentStep = p.CurrentStep,
                    InformationRequestStatus = context.ProjectInformationRequests
                        .Any(r => r.ProjectId == p.Id && r.Status == InformationRequestStatus.Requested)
                        ? InformationRequestStatus.Requested
                        : context.ProjectInformationRequests
                            .Any(r => r.ProjectId == p.Id && r.Status == InformationRequestStatus.Submitted)
                            ? InformationRequestStatus.Submitted
                            : context.ProjectInformationRequests.Any(r => r.ProjectId == p.Id)
                                ? InformationRequestStatus.Approved
                                : null,
                    AwaitingUsersCsv = p.Status == ProjectStatus.AccountCreation && p.UsersCsvRequestedAt != null,
                    RequiresAccount = p.Discovery != null && p.Discovery.CrmPaymentExperience == CrmPaymentExperience.HostedByPciPal,
                    CreatedAt = p.CreatedAt,
                    DeletionScheduledFor = p.DeletionScheduledFor,
                    CustomerId = p.CustomerId,
                    CustomerName = showCustomerName ? p.Customer.CompanyName : null
                })
                .ToListAsync(cancellationToken);

            return rows;
        }

        private IQueryable<Project> ScopedProjects(Guid? partnerId, Guid? customerId)
        {
            IQueryable<Project> q = context.Projects.AsNoTracking();

            if (customerId is Guid cid)
            {
                return q.Where(p => p.CustomerId == cid);
            }

            if (partnerId is Guid pid)
            {
                q = from p in q
                    join c in context.Customers.AsNoTracking() on p.CustomerId equals c.Id
                    where c.PartnerId == pid
                    select p;
            }

            return q;
        }
    }
}
