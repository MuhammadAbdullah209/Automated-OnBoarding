using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>A user of the project (a row of the customer's users CSV).</summary>
    public class CustomerUserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CustomerUserRole Role { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
