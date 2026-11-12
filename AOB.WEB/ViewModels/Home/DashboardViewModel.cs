using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;

namespace AOB.WEB.ViewModels.Home
{
    public class DashboardViewModel
    {
        public DashboardAudience Audience { get; set; }

        public DashboardDto Data { get; set; } = new();

        /// <summary>True when the customer has not finished minimum profile fields (dashboard still shown).</summary>
        public bool ProfileIncomplete { get; set; }

        /// <summary>Customer organisation name for the home band (customer dashboard only).</summary>
        public string? CompanyName { get; set; }

        public CustomerProfileDto? CustomerProfile { get; set; }

        public IReadOnlyList<DocumentChecklistItemDto> Documents { get; set; } = Array.Empty<DocumentChecklistItemDto>();

        public IReadOnlyList<ProjectSummaryDto> CustomerProjects { get; set; } = Array.Empty<ProjectSummaryDto>();

        public IReadOnlyList<CustomerHomeQueueItem> Queue { get; set; } = Array.Empty<CustomerHomeQueueItem>();

        public IReadOnlyList<ReadinessRow> ReadinessSections { get; set; } = Array.Empty<ReadinessRow>();

        public int ReadinessReady { get; set; }

        public int ReadinessTotal { get; set; }
    }

    public enum DashboardAudience
    {
        Internal,
        Partner,
        Customer
    }
}
