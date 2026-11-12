using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>One gateway field included in the project's integration.</summary>
    public class ProjectGatewayFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public GatewayFieldType FieldType { get; set; }

        /// <summary>true = input, false = output (not meaningful for Authentication).</summary>
        public bool IsInput { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Everything a customer entered for a project, for the read-only "All details" page.
    /// A section is null when its step has not been saved yet.
    /// </summary>
    public class ProjectFullDetailsDto
    {
        public ProjectOverviewDto Project { get; set; } = new();

        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>When the project was last changed. For a submitted project this is the submission time, as it cannot be edited afterwards.</summary>
        public DateTimeOffset? LastUpdatedAt { get; set; }

        public ProjectDiscoveryDto? Discovery { get; set; }
        public ProjectTechnicalEnvironmentDto? TechnicalEnvironment { get; set; }
        public ProjectPaymentConfigurationDto? PaymentConfiguration { get; set; }

        /// <summary>Name of the payment gateway chosen on the Payment Configuration step.</summary>
        public string? PaymentGatewayName { get; set; }
        public ProjectGatewayConfigurationDto? GatewayConfiguration { get; set; }

        /// <summary>The uploaded API documentation, if any.</summary>
        public ProjectDocumentInfoDto? ApiDocumentation { get; set; }

        /// <summary>Authentication, required and selected optional fields, ordered by type then name.</summary>
        public List<ProjectGatewayFieldDto> GatewayFields { get; set; } = new();
    }
}
