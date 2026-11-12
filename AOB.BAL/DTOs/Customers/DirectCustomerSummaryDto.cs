namespace AOB.BAL.DTOs.Customers
{
    /// <summary>One row of the internal Direct customers list.</summary>
    public class DirectCustomerSummaryDto
    {
        public Guid Id { get; set; }

        /// <summary>Null until the customer has completed their organisation details.</summary>
        public string? CompanyName { get; set; }

        /// <summary>The customer's login email.</summary>
        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
