using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Helpers
{
    public static class ProjectStatusRules
    {
        /// <summary>Customer can still edit project steps and documents.</summary>
        public static bool IsEditableByCustomer(ProjectStatus status)
            => status is ProjectStatus.InProgress or ProjectStatus.Draft;

        /// <summary>Customer has submitted; internal workflow may continue.</summary>
        public static bool IsPastCustomerSubmit(ProjectStatus status)
            => status is ProjectStatus.Submitted or ProjectStatus.Approval or ProjectStatus.Provisioning
                or ProjectStatus.AccountCreation or ProjectStatus.Completed;

        /// <summary>
        /// The customer can still add a missing document or URL (API documentation, redirect URL): before and after
        /// submitting, until the project is approved and provisioning starts.
        /// </summary>
        public static bool CanCustomerProvideMissingItems(ProjectStatus status)
            => status is ProjectStatus.InProgress or ProjectStatus.Draft or ProjectStatus.Submitted or ProjectStatus.Approval;

        public static bool CanCustomerSubmit(ProjectStatus status)
            => status is ProjectStatus.InProgress or ProjectStatus.Draft;

        public static bool CanCustomerSaveAsDraft(ProjectStatus status)
            => status is ProjectStatus.InProgress or ProjectStatus.Draft;

        /// <summary>Internal staff may advance Submitted → Approval → Provisioning.</summary>
        public static ProjectStatus? NextInternalStatus(ProjectStatus status)
            => status switch
            {
                ProjectStatus.Submitted => ProjectStatus.Approval,
                ProjectStatus.Approval => ProjectStatus.Provisioning,
                _ => null
            };
    }
}
