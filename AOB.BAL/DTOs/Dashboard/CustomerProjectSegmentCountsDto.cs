namespace AOB.BAL.DTOs.Dashboard
{
    /// <summary>Customer counts grouped by whether they have projects and approval state.</summary>
    public class CustomerProjectSegmentCountsDto
    {
        public int TotalCustomers { get; set; }
        public int WithNoProjects { get; set; }

        /// <summary>Customers with at least one submitted project (awaiting / in approval).</summary>
        public int WithApprovals { get; set; }
    }
}
