using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>A project with how far its onboarding steps have got, for the project page and step screens.</summary>
    public class ProjectOverviewDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public List<PciPalService> Services { get; set; } = new();
        public ProjectStatus Status { get; set; }
        public ProjectStep CurrentStep { get; set; }

        /// <summary>Set while the project is waiting to be deleted (it can still be restored until then).</summary>
        public DateTimeOffset? DeletionScheduledFor { get; set; }

        /// <summary>Standard or custom, decided when the project was submitted (null before that).</summary>
        public BuildType? BuildType { get; set; }

        /// <summary>When an internal user asked the customer for the users CSV (null when not asked or already uploaded).</summary>
        public DateTimeOffset? UsersCsvRequestedAt { get; set; }

        /// <summary>An account is created only when PCI Pal hosts the payment page (not when it is embedded in the CRM).</summary>
        public bool RequiresAccount => PaymentExperience == CrmPaymentExperience.HostedByPciPal;

        /// <summary>The latest information request: Requested (customer to act), Submitted (to review) or Approved.</summary>
        public InformationRequestStatus? InformationRequestStatus { get; set; }

        /// <summary>How the payment page is shown (Discovery). Decides whether an account is created after provisioning.</summary>
        public CrmPaymentExperience? PaymentExperience { get; set; }

        /// <summary>Why the build is custom, one reason per entry.</summary>
        public IReadOnlyList<string> BuildReasons { get; set; } = Array.Empty<string>();

        /// <summary>Reason given when deletion was scheduled. Empty once the project is restored.</summary>
        public string? DeletionReason { get; set; }

        /// <summary>Set once the customer has answered the Discovery screen.</summary>
        public CrmSystem? CrmSystem { get; set; }
        public CrmPlatform? CrmPlatform { get; set; }

        /// <summary>True if the customer takes payments by phone with an agent (asked on Discovery); decides whether contact centre questions are shown.</summary>
        public bool TakesPhonePayments { get; set; }

        /// <summary>True if Web Callback was chosen for post-transactional data transfer, so API documentation is needed.</summary>
        public bool UsesWebCallback { get; set; }

        /// <summary>True once API documentation has been uploaded to the project.</summary>
        public bool HasApiDocumentation { get; set; }

        // A step counts as done once the customer has saved its screen.
        public bool HasDiscovery { get; set; }
        public bool HasTechnicalEnvironment { get; set; }
        public bool HasPaymentConfiguration { get; set; }
        public bool HasGatewayConfiguration { get; set; }

        /// <summary>True once the customer has submitted (project is locked for customer edits).</summary>
        public bool IsSubmitted => ProjectStatusRules.IsPastCustomerSubmit(Status);

        public bool IsEditableByCustomer => ProjectStatusRules.IsEditableByCustomer(Status);

        public bool IsReadyToSubmit =>
            HasDiscovery && HasTechnicalEnvironment && HasPaymentConfiguration && HasGatewayConfiguration;
    }
}
