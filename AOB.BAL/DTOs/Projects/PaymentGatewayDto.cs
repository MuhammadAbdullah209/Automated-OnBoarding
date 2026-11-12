namespace AOB.BAL.DTOs.Projects
{
    /// <summary>A configured payment gateway, for the "PSP / Gateway" dropdown.</summary>
    public class PaymentGatewayDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
