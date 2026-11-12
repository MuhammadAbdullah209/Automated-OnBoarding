namespace AOB.BAL.DTOs.Customers
{
    /// <summary>A customer as an entry in a dropdown (filters, pickers).</summary>
    public class CustomerOptionDto
    {
        public Guid Id { get; set; }
        public string? CompanyName { get; set; }
    }
}
