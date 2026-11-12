namespace AOB.BAL.Common.Enums
{
    // Enums for the Project (case) itself. Display labels live in AOB.BAL.Common.Constants.ProjectOptions.

    /// <summary>PCI Pal service a project is for. Stored comma-separated in Project.Services.</summary>
    public enum PciPalService
    {
        ClickToPay = 1,
        SpeakToPay = 2,
        KeyToPay = 3
    }

    /// <summary>The kind of environment the payments are taken in (Project.CrmSystem).</summary>
    public enum CrmSystem
    {
        ContactCentreEnvironment = 1,
        Crm = 2
    }

    /// <summary>
    /// Which CRM it is. Only used when Project.CrmSystem is Crm (Project.CrmPlatform).
    /// </summary>
    public enum CrmPlatform
    {
        Salesforce = 1,
        Dynamics = 2,
        WebApplication = 3
    }

    /// <summary>
    /// The kind of document uploaded to a project (ProjectDocument.Type). More types can be added here,
    /// with their labels in ProjectOptions.ProjectDocumentTypeLabels.
    /// </summary>
    public enum ProjectDocumentType
    {
        Other = 1,

        /// <summary>API documentation for the customer's web callback endpoint (one per project; a new upload replaces it).</summary>
        ApiDocumentation = 2,

        /// <summary>A file the customer uploaded to answer an information request from internal staff.</summary>
        InformationRequest = 3
    }

    /// <summary>Overall state of the project through customer onboarding and internal fulfilment.</summary>
    public enum ProjectStatus
    {
        InProgress = 1,
        Draft = 2,
        Submitted = 3,
        Approval = 4,
        Provisioning = 5,

        /// <summary>The account is being created (only for a project whose payment page is hosted by PCI Pal).</summary>
        AccountCreation = 6,

        /// <summary>Everything is done: the project has been provisioned (and its account created, where one is needed).</summary>
        Completed = 7
    }

    /// <summary>The onboarding step a project has reached (lets the customer resume where they left off).</summary>
    public enum ProjectStep
    {
        Discovery = 1,
        TechnicalEnvironment = 2,
        PaymentConfiguration = 3,
        GatewayConfiguration = 4,
        ReadinessCheck = 5
    }
}
