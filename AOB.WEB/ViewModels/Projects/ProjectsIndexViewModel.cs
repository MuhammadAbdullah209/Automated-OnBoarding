using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Projects;

namespace AOB.WEB.ViewModels.Projects
{
    public class ProjectsIndexViewModel
    {
        public PagedResult<ProjectSummaryDto> Result { get; set; } = new();
        public string? Search { get; set; }
        public ProjectStatus? Status { get; set; }
        public ProjectStep? Step { get; set; }
        public DateOnly? CreatedFrom { get; set; }
        public DateOnly? CreatedTo { get; set; }
        public ProjectListView View { get; set; } = ProjectListView.All;

        /// <summary>Counts per list tab (customer list; loaded when total projects is modest).</summary>
        public IReadOnlyDictionary<ProjectListView, int> ViewCounts { get; set; }
            = new Dictionary<ProjectListView, int>();

        /// <summary>Internal staff: projects across all customers, with a customer column and filter.</summary>
        public bool IsAdminList { get; set; }
        public Guid? CustomerId { get; set; }
        public IReadOnlyList<CustomerOptionDto> CustomerFilters { get; set; } = [];

        public bool HasActiveFilter => CustomerId is not null || HasNonCustomerFilter;

        /// <summary>Any filter apart from the customer (a customer with no projects is "no projects yet", not "no matches").</summary>
        public bool HasNonCustomerFilter =>
            !string.IsNullOrWhiteSpace(Search) || Status is not null || Step is not null
            || CreatedFrom is not null || CreatedTo is not null || View != ProjectListView.All;
    }
}
