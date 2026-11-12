using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// An onboarding case ("project") a customer sets up with PCI Pal. The questions of each
    /// onboarding screen are stored in a separate 1:1 table (ProjectDiscovery,
    /// ProjectTechnicalEnvironment, ProjectPaymentConfiguration, ProjectGatewayConfiguration),
    /// created when the customer first saves that screen. The gateway fields are many rows
    /// (ProjectGatewayFields), one per field.
    ///
    /// Convention for all project tables: a single choice is an enum column; a multiple choice is a
    /// string holding the enum names separated by commas (e.g. "ClickToPay,SpeakToPay"). Display
    /// labels come from AOB.BAL.Common.Constants.ProjectOptions. Answers are nullable in the
    /// database so a screen can be saved part-way; the form decides what is required.
    /// </summary>
    public class Project
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Comma-separated PciPalService names, e.g. "ClickToPay".
        [MaxLength(200)]
        public string? Services { get; set; }

        // Contact centre environment or CRM. Null until chosen.
        public CrmSystem? CrmSystem { get; set; }

        // Which CRM (Salesforce, Dynamics, Web application). Only set when CrmSystem is Crm.
        public CrmPlatform? CrmPlatform { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.InProgress;

        // The furthest onboarding screen the customer has reached.
        public ProjectStep CurrentStep { get; set; } = ProjectStep.Discovery;

        /// <summary>
        /// Standard or custom, decided by the domain rules when the project is submitted.
        /// Null until then.
        /// </summary>
        public BuildType? BuildType { get; set; }

        /// <summary>
        /// When an internal user asked the customer for the users CSV; cleared when the customer uploads it.
        /// </summary>
        public DateTimeOffset? UsersCsvRequestedAt { get; set; }

        /// <summary>
        /// The status that needs attention across the information requests: Requested while any request still waits
        /// for the customer, else Submitted while any waits for review, else Approved; null if there is none.
        /// Read from <see cref="InformationRequests"/>, which must be loaded. It is not stored on the project.
        /// </summary>
        [NotMapped]
        public InformationRequestStatus? InformationRequestStatus
            => InformationRequests.Any(r => r.Status == Common.Enums.InformationRequestStatus.Requested)
                ? Common.Enums.InformationRequestStatus.Requested
                : InformationRequests.Any(r => r.Status == Common.Enums.InformationRequestStatus.Submitted)
                    ? Common.Enums.InformationRequestStatus.Submitted
                    : InformationRequests.Any() ? Common.Enums.InformationRequestStatus.Approved : null;

        /// <summary>Why the build is custom (one reason per line); null for a standard build.</summary>
        [MaxLength(2000)]
        public string? BuildReasons { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// When set, the project is waiting to be deleted at this time; a background job removes it then.
        /// Clearing it before that time restores the project.
        /// </summary>
        public DateTimeOffset? DeletionScheduledFor { get; set; }

        /// <summary>Why the project was scheduled for deletion. Cleared if it is restored in time.</summary>
        [MaxLength(1000)]
        public string? DeletionReason { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Customer Customer { get; set; } = null!;

        public ProjectDiscovery? Discovery { get; set; }

        public ProjectTechnicalEnvironment? TechnicalEnvironment { get; set; }

        public ProjectPaymentConfiguration? PaymentConfiguration { get; set; }

        public ProjectGatewayConfiguration? GatewayConfiguration { get; set; }

        // Created when the project is submitted.
        public ProjectProvisioningDetail? ProvisioningDetail { get; set; }

        // One row per gateway field included in the integration.
        public ICollection<ProjectGatewayFields> GatewayFields { get; set; } = new List<ProjectGatewayFields>();

        // Files uploaded to the project.
        public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();

        // Requests from internal staff for more information (custom builds waiting for approval).
        public ICollection<ProjectInformationRequest> InformationRequests { get; set; } = new List<ProjectInformationRequest>();

        // The users the customer wants accounts for (from the users CSV).
        public ICollection<CustomerUser> CustomerUsers { get; set; } = new List<CustomerUser>();
    }
}
