namespace AOB.BAL.DTOs.Projects
{
    /// <summary>
    /// What the Setup Documentation Pack (SDP) of a completed, provisioned project shows: the endpoint URLs,
    /// the tenant details and the flow with its input fields. The client secret itself is never included.
    /// </summary>
    public class SetupDocumentDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>"Staging" or "Production", from the platform.</summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>The region as written in the document, e.g. EUWest1.</summary>
        public string Region { get; set; } = string.Empty;

        public string AuthenticationEndpoint { get; set; } = string.Empty;
        public string SessionEndpoint { get; set; } = string.Empty;
        public string IframeUrl { get; set; } = string.Empty;

        public string? TenantName { get; set; }
        public string? TenantId { get; set; }
        public string? Username { get; set; }
        public string? ClientId { get; set; }

        /// <summary>The client secret expires three years after the project was provisioned.</summary>
        public DateTimeOffset ClientSecretExpiry { get; set; }

        public string? FlowId { get; set; }

        /// <summary>The input fields the customer's system sends to the flow.</summary>
        public List<SetupDocumentFieldDto> InputFields { get; set; } = new();
    }

    /// <summary>One row of the SDP's flow input table.</summary>
    public class SetupDocumentFieldDto
    {
        public string Caption { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
