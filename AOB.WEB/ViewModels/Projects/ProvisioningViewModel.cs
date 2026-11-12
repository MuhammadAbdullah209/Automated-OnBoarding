using AOB.BAL.Common.Enums;

namespace AOB.WEB.ViewModels.Projects
{
    /// <summary>Backing model for the provisioning status page (in progress and live in staging).</summary>
    public class ProvisioningViewModel
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string TenantId { get; set; } = "—";
        public string Environment { get; set; } = "—";
        public string ShortReference { get; set; } = "—";
        public string ServicesLabel { get; set; } = "—";
        public string RouteLabel { get; set; } = "—";
        public string PrimaryContact { get; set; } = "—";
        public DateTimeOffset SubmittedAt { get; set; }
        public ProjectStatus Status { get; set; }
        public BuildType? BuildType { get; set; }
        public CrmPlatform? CrmPlatform { get; set; }

        /// <summary>PCI Pal hosts the payment page; account creation and a users CSV apply.</summary>
        public bool RequiresAccount { get; set; }

        /// <summary>An internal user marked the hosted account as created.</summary>
        public bool AccountCreated { get; set; }

        /// <summary>Internal staff watch only; they cannot run the demo controls.</summary>
        public bool ReadOnly { get; set; }

        /// <summary>True when the page was opened right after submitting the project.</summary>
        public bool ContinueToOverview { get; set; }

        /// <summary>Provisioning has finished (tenant and flow exist).</summary>
        public bool IsComplete { get; set; }

        /// <summary>Play the step walk-through once, right after submit (even if provisioning already finished server-side).</summary>
        public bool Animate => ContinueToOverview;

        /// <summary>Hide the in-progress stack and show the post-provision layout.</summary>
        public bool ShowPostProvisionLayout => IsComplete && !Animate;

        /// <summary>Hosted-by-PCI-Pal project still in the account creation lifecycle step.</summary>
        public bool HostedAccountCreationPhase =>
            IsComplete && Status == ProjectStatus.AccountCreation && RequiresAccount;

        /// <summary>Customer must upload the users list (hosted, account not created yet).</summary>
        public bool PostProvisionAccountCreationGate => HostedAccountCreationPhase && !AccountCreated;

        /// <summary>Embedded CRM builds only; hosted (account creation) projects never get an SDP.</summary>
        public bool PostProvisionShowSetupPack =>
            !RequiresAccount && IsComplete && Status == ProjectStatus.Completed;

        /// <summary>Live-in-staging experience (hero, test payment); hosted builds get this after the account exists, before Completed.</summary>
        public bool PostProvisionLiveStaging =>
            IsComplete && (Status == ProjectStatus.Completed || (HostedAccountCreationPhase && AccountCreated));

        /// <summary>Customer must upload the users list.</summary>
        public bool ShowAccountCreationGate => ShowPostProvisionLayout && PostProvisionAccountCreationGate;

        /// <summary>Full live-in-staging (visible after animation / on return visits).</summary>
        public bool ShowLiveStaging => ShowPostProvisionLayout && PostProvisionLiveStaging;

        /// <summary>Legacy alias for views that mean “live in staging”, not account creation upload.</summary>
        public bool ShowLiveView => ShowLiveStaging;

        public bool CanLaunchFlow =>
            BuildType != AOB.BAL.Common.Enums.BuildType.Custom
            && (Status == ProjectStatus.Completed || (HostedAccountCreationPhase && AccountCreated));

        /// <summary>SDP is ready whenever the pack applies (including while the post-submit walk-through is playing).</summary>
        public bool CanDownloadSetupPack => PostProvisionShowSetupPack;

        public bool ShowLaunchUi => PostProvisionLiveStaging && BuildType != AOB.BAL.Common.Enums.BuildType.Custom;
    }
}
