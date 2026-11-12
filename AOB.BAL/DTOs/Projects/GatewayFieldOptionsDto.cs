using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>One field of the project's payment gateway, as offered on the Adaptive Configuration screen.</summary>
    public class GatewayFieldOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public GatewayFieldType FieldType { get; set; }

        /// <summary>true = input, false = output (not meaningful for Authentication).</summary>
        public bool IsInput { get; set; }

        public string? Description { get; set; }

        /// <summary>Authentication and Required fields are always included; the customer chooses the rest.</summary>
        public bool IsSelectable => FieldType is GatewayFieldType.Conditional or GatewayFieldType.Optional;
    }

    /// <summary>The fields of the payment gateway chosen for a project.</summary>
    public class GatewayFieldOptionsDto
    {
        public Guid GatewayId { get; set; }
        public string GatewayName { get; set; } = string.Empty;
        public List<GatewayFieldOptionDto> Fields { get; set; } = new();
    }
}
