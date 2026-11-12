using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Customer data access.</summary>
    public class CustomerRepository(OnboardingDbContext context) : ICustomerRepository
    {
        public async Task<Customer?> GetByIdAsync(Guid id)
            => await context.Customers.FindAsync(id);

        public async Task<Customer?> GetByPartnerAndIdAsync(Guid partnerId, Guid customerId)
            => await context.Customers.AsNoTracking()
                .Include(c => c.Auth)
                .FirstOrDefaultAsync(c => c.Id == customerId && c.PartnerId == partnerId);

        public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListByPartnerAsync(
            Guid partnerId,
            CustomerListQuery query)
        {
            IQueryable<Customer> q = context.Customers.AsNoTracking()
                .Where(c => c.PartnerId == partnerId);

            q = ApplyListFilters(q, query);
            return await PageAsync(q, query);
        }

        public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAllAsync(CustomerListQuery query)
        {
            IQueryable<Customer> q = context.Customers.AsNoTracking()
                .Include(c => c.Partner);

            if (query.PartnerId is Guid partnerId)
            {
                q = q.Where(c => c.PartnerId == partnerId);
            }

            q = ApplyListFilters(q, query);
            return await PageAsync(q, query);
        }

        public async Task<IReadOnlyList<DirectCustomerSummaryDto>> GetDirectSummariesAsync()
            => await context.Customers.AsNoTracking()
                .Where(c => c.IsDirect)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new DirectCustomerSummaryDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    // Every direct customer has a login (enforced by a database check constraint).
                    Email = c.Auth!.Email,
                    EmailConfirmed = c.Auth.EmailConfirmed,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

        public async Task<IReadOnlyList<CustomerOptionDto>> GetOptionsAsync(Guid? partnerId = null)
            => await context.Customers.AsNoTracking()
                .Where(c => partnerId == null || c.PartnerId == partnerId)
                .OrderBy(c => c.CompanyName == null)
                .ThenBy(c => c.CompanyName)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new CustomerOptionDto { Id = c.Id, CompanyName = c.CompanyName })
                .ToListAsync();

        public async Task AddAsync(Customer customer)
            => await context.Customers.AddAsync(customer);

        private static IQueryable<Customer> ApplyListFilters(IQueryable<Customer> q, CustomerListQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(c =>
                    EF.Functions.Like(c.CompanyName ?? "", $"%{term}%")
                    || EF.Functions.Like(c.PrimaryContactFullName ?? "", $"%{term}%")
                    || EF.Functions.Like(c.PrimaryContactEmail ?? "", $"%{term}%"));
            }

            if (query.HasLogin is bool hasLogin)
            {
                q = hasLogin
                    ? q.Where(c => c.UserAuthId != null)
                    : q.Where(c => c.UserAuthId == null);
            }

            if (query.IsDirect is bool isDirect)
            {
                q = q.Where(c => c.IsDirect == isDirect);
            }

            return q;
        }

        private static async Task<(IReadOnlyList<Customer> Items, int TotalCount)> PageAsync(
            IQueryable<Customer> q,
            CustomerListQuery query)
        {
            var totalCount = await q.CountAsync();

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? CustomerListQuery.DefaultPageSize : query.PageSize;
            if (pageSize > CustomerListQuery.MaxPageSize)
            {
                pageSize = CustomerListQuery.MaxPageSize;
            }

            var items = await q
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<int> CountAsync(Guid? partnerId = null, CancellationToken cancellationToken = default)
        {
            IQueryable<Customer> q = context.Customers.AsNoTracking();
            if (partnerId is Guid pid)
            {
                q = q.Where(c => c.PartnerId == pid);
            }

            return q.CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<CustomerStatus, int>> CountByStatusAsync(
            Guid? partnerId = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Customer> q = context.Customers.AsNoTracking();
            if (partnerId is Guid pid)
            {
                q = q.Where(c => c.PartnerId == pid);
            }

            var rows = await q
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(r => r.Status, r => r.Count);
        }

        public async Task<CustomerProjectSegmentCountsDto> GetProjectSegmentCountsAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Customer> customers = context.Customers.AsNoTracking();
            if (partnerId is Guid pid)
            {
                customers = customers.Where(c => c.PartnerId == pid);
            }

            if (customerId is Guid cid)
            {
                customers = customers.Where(c => c.Id == cid);
            }

            var customerIds = await customers.Select(c => c.Id).ToListAsync(cancellationToken);
            if (customerIds.Count == 0)
            {
                return new CustomerProjectSegmentCountsDto();
            }

            var submissionByCustomer = await context.Projects.AsNoTracking()
                .Where(p => customerIds.Contains(p.CustomerId))
                .GroupBy(p => p.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    HasSubmitted = g.Any(p => p.Status == ProjectStatus.Submitted
                                              || p.Status == ProjectStatus.Approval
                                              || p.Status == ProjectStatus.Provisioning
                                              || p.Status == ProjectStatus.AccountCreation
                                              || p.Status == ProjectStatus.Completed)
                })
                .ToListAsync(cancellationToken);

            var hasProjects = submissionByCustomer.Select(x => x.CustomerId).ToHashSet();
            var approvalCustomers = submissionByCustomer.Where(x => x.HasSubmitted).Select(x => x.CustomerId).ToHashSet();

            return new CustomerProjectSegmentCountsDto
            {
                TotalCustomers = customerIds.Count,
                WithNoProjects = customerIds.Count(id => !hasProjects.Contains(id)),
                WithApprovals = approvalCustomers.Count
            };
        }
    }
}
