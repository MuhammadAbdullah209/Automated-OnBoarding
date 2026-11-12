using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Customers;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Customer management on behalf of partners.</summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Creates a non-direct customer for a partner. Only the company name is needed;
        /// no login is created, so the customer cannot sign in.
        /// </summary>
        Task<ServiceResult<Guid>> CreatePartnerCustomerAsync(Guid partnerId, string companyName);

        /// <summary>Filtered, paged customers for a partner.</summary>
        Task<PagedResult<CustomerSummaryDto>> GetPartnerCustomersPagedAsync(Guid partnerId, CustomerListQuery query);

        /// <summary>Filtered, paged customers across all partners (internal admin).</summary>
        Task<PagedResult<CustomerSummaryDto>> GetAllCustomersPagedAsync(CustomerListQuery query);

        /// <summary>Customer profile, documents and projects when owned by the partner.</summary>
        Task<ServiceResult<CustomerPartnerDetailDto>> GetPartnerCustomerDetailsAsync(Guid partnerId, Guid customerId);

        /// <summary>Every customer's id and company name, for dropdowns (internal staff), or only one partner's customers.</summary>
        Task<IReadOnlyList<CustomerOptionDto>> GetCustomerOptionsAsync(Guid? partnerId = null);

        /// <summary>The partner that owns the customer, or null when the customer does not exist.</summary>
        Task<Guid?> GetOwningPartnerIdAsync(Guid customerId);

        /// <summary>True when the customer belongs to the partner.</summary>
        Task<bool> PartnerOwnsCustomerAsync(Guid partnerId, Guid customerId);

        /// <summary>Lists all direct customers (those who signed up themselves). For internal users.</summary>
        Task<IReadOnlyList<DirectCustomerSummaryDto>> GetDirectCustomersAsync();

        /// <summary>Loads the Organisation &amp; Contacts details of a customer.</summary>
        Task<ServiceResult<CustomerProfileDto>> GetProfileAsync(Guid customerId);

        /// <summary>Saves the Organisation &amp; Contacts details of a customer.</summary>
        Task<ServiceResult> SaveProfileAsync(Guid customerId, CustomerProfileDto profile);

        /// <summary>Disables or re-enables a customer (re-enable recalculates Pending vs Onboarded).</summary>
        Task<ServiceResult> SetCustomerDisabledAsync(Guid customerId, bool disabled);
    }
}
