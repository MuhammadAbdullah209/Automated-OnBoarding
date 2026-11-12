using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Customer data access.</summary>
    public interface ICustomerRepository
    {
        /// <summary>Finds a customer by id (tracked, so changes can be saved).</summary>
        Task<Customer?> GetByIdAsync(Guid id);

        /// <summary>One customer owned by the partner, with login email when present.</summary>
        Task<Customer?> GetByPartnerAndIdAsync(Guid partnerId, Guid customerId);

        /// <summary>Filtered, paged customers for a partner (newest first). TotalCount is before Skip/Take.</summary>
        Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListByPartnerAsync(Guid partnerId, CustomerListQuery query);

        /// <summary>Filtered, paged customers across all partners (admin). Includes Partner for display.</summary>
        Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAllAsync(CustomerListQuery query);

        /// <summary>All customers who signed up themselves (IsDirect), newest first.</summary>
        Task<IReadOnlyList<DirectCustomerSummaryDto>> GetDirectSummariesAsync();

        /// <summary>Every customer's id and company name (or only one partner's), ordered by name (for dropdowns).</summary>
        Task<IReadOnlyList<CustomerOptionDto>> GetOptionsAsync(Guid? partnerId = null);

        /// <summary>Adds the customer. Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddAsync(Customer customer);

        /// <summary>Customer count, optionally limited to one partner's portfolio.</summary>
        Task<int> CountAsync(Guid? partnerId = null, CancellationToken cancellationToken = default);

        /// <summary>Customers grouped by workflow status; optional partner filter.</summary>
        Task<IReadOnlyDictionary<CustomerStatus, int>> CountByStatusAsync(
            Guid? partnerId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Customers in scope (all, one partner, or one customer) split by project submission state.
        /// </summary>
        Task<CustomerProjectSegmentCountsDto> GetProjectSegmentCountsAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default);
    }
}
