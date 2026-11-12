using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Partners;

namespace AOB.WEB.ViewModels.Customers
{
    public class CustomersIndexViewModel
    {
        public PagedResult<CustomerSummaryDto> Result { get; set; } = new();
        public string? Search { get; set; }
        public bool? HasLogin { get; set; }
        public bool? IsDirect { get; set; }
        public Guid? PartnerId { get; set; }

        /// <summary>Internal staff list (all partners); partners see only their own customers.</summary>
        public bool IsAdminList { get; set; }

        public IReadOnlyList<PartnerSummaryDto> PartnerFilters { get; set; } = [];

        /// <summary>Totals per list tab: all / direct / partner (staff), or all / login / nologin (partners).</summary>
        public IReadOnlyDictionary<string, int> TabCounts { get; set; } = new Dictionary<string, int>();

        public bool HasActiveFilter =>
            !string.IsNullOrWhiteSpace(Search)
            || HasLogin is not null
            || IsDirect is not null
            || PartnerId is not null;
    }
}
