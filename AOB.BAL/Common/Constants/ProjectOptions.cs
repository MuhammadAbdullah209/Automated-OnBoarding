using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Constants
{
    /// <summary>
    /// Display labels for the project enums, used by dropdowns, checkboxes and read-only views.
    /// The database stores the enum names (single choices as the enum, multiple choices as a
    /// comma-separated list of enum names); labels can change without touching data.
    /// </summary>
    public static class ProjectOptions
    {
        // ----- Project -----

        public static readonly IReadOnlyDictionary<PciPalService, string> ServiceLabels = new Dictionary<PciPalService, string>
        {
            [PciPalService.ClickToPay] = "Click to Pay",
            [PciPalService.SpeakToPay] = "Speak to Pay",
            [PciPalService.KeyToPay] = "Key to Pay"
        };

        /// <summary>Services that can be selected today; the others are shown as "Coming soon".</summary>
        public static readonly IReadOnlySet<PciPalService> AvailableServices = new HashSet<PciPalService>
        {
            PciPalService.ClickToPay
        };

        public static readonly IReadOnlyDictionary<CrmSystem, string> CrmSystemLabels = new Dictionary<CrmSystem, string>
        {
            [CrmSystem.ContactCentreEnvironment] = "Contact centre environment",
            [CrmSystem.Crm] = "CRM (e.g. Salesforce, Microsoft Dynamics, web application)"
        };

        public static readonly IReadOnlyDictionary<CrmPlatform, string> CrmPlatformLabels = new Dictionary<CrmPlatform, string>
        {
            [CrmPlatform.Salesforce] = "Salesforce",
            [CrmPlatform.Dynamics] = "Microsoft Dynamics",
            [CrmPlatform.WebApplication] = "Web application"
        };

        public static readonly IReadOnlyDictionary<ProjectDocumentType, string> ProjectDocumentTypeLabels = new Dictionary<ProjectDocumentType, string>
        {
            [ProjectDocumentType.Other] = "Other",
            [ProjectDocumentType.ApiDocumentation] = "Web callback API documentation",
            [ProjectDocumentType.InformationRequest] = "Requested information"
        };

        public static readonly IReadOnlyDictionary<ProjectStatus, string> ProjectStatusLabels = new Dictionary<ProjectStatus, string>
        {
            [ProjectStatus.InProgress] = "In progress",
            [ProjectStatus.Draft] = "Draft",
            [ProjectStatus.Submitted] = "Submitted",
            [ProjectStatus.Approval] = "Pending approval",
            [ProjectStatus.Provisioning] = "Provisioning",
            [ProjectStatus.AccountCreation] = "Account creation",
            [ProjectStatus.Completed] = "Completed"
        };

        public static readonly IReadOnlyDictionary<CustomerStatus, string> CustomerStatusLabels =
            new Dictionary<CustomerStatus, string>
            {
                [CustomerStatus.Pending] = "Pending",
                [CustomerStatus.Onboarded] = "Onboarded",
                [CustomerStatus.Disabled] = "Disabled"
            };

        public static readonly IReadOnlyDictionary<ProjectStep, string> ProjectStepLabels = new Dictionary<ProjectStep, string>
        {
            [ProjectStep.Discovery] = "Business & Use-Case Discovery",
            [ProjectStep.TechnicalEnvironment] = "Technical Environment",
            [ProjectStep.PaymentConfiguration] = "Payment Configuration",
            [ProjectStep.GatewayConfiguration] = "Adaptive Configuration",
            [ProjectStep.ReadinessCheck] = "Readiness Check"
        };

        // ----- Discovery -----

        public static readonly IReadOnlyDictionary<CurrentPaymentMethod, string> CurrentPaymentMethodLabels = new Dictionary<CurrentPaymentMethod, string>
        {
            [CurrentPaymentMethod.PhoneCallWithAgent] = "During a phone call with an agent",
            [CurrentPaymentMethod.WebsiteOrOnlineCheckout] = "Through a website or online checkout",
            [CurrentPaymentMethod.PaymentLinkOrDigitalMessage] = "Via a payment link or digital message",
            [CurrentPaymentMethod.AnotherWay] = "Another way"
        };

        public static readonly IReadOnlyDictionary<CrmPaymentExperience, string> CrmPaymentExperienceLabels = new Dictionary<CrmPaymentExperience, string>
        {
            [CrmPaymentExperience.EmbeddedIframe] = "Embedded in our CRM (iFrame)",
            [CrmPaymentExperience.HostedByPciPal] = "Hosted by PCI Pal"
        };

        // ----- Technical environment -----

        public static readonly IReadOnlyDictionary<PaymentRegion, string> PaymentRegionLabels = new Dictionary<PaymentRegion, string>
        {
            [PaymentRegion.Emea] = "EMEA",
            [PaymentRegion.NorthAmerica] = "North America",
            [PaymentRegion.AsiaPacific] = "Asia-Pacific"
        };

        public static readonly IReadOnlyDictionary<PciPalRegion, string> PciPalRegionLabels = new Dictionary<PciPalRegion, string>
        {
            [PciPalRegion.EuWest1] = "EU-West-1 (United Kingdom)",
            [PciPalRegion.EuWest2] = "EU-West-2 (United Kingdom)",
            [PciPalRegion.EuCentral1] = "EU-Central-1 (Germany)",
            [PciPalRegion.UsEast1] = "US-East-1 (N. Virginia)",
            [PciPalRegion.UsWest2] = "US-West-2 (Oregon)",
            [PciPalRegion.CaCentral1] = "CA-Central-1 (Canada)",
            [PciPalRegion.ApSoutheast2] = "AP-Southeast-2 (Sydney)"
        };

        /// <summary>The regions a customer can pick for staging (EU-West-1, CA-Central-1, AP-Southeast-2).</summary>
        public static readonly IReadOnlyDictionary<PciPalRegion, string> StagingRegionLabels = new Dictionary<PciPalRegion, string>
        {
            [PciPalRegion.EuWest1] = PciPalRegionLabels[PciPalRegion.EuWest1],
            [PciPalRegion.CaCentral1] = PciPalRegionLabels[PciPalRegion.CaCentral1],
            [PciPalRegion.ApSoutheast2] = PciPalRegionLabels[PciPalRegion.ApSoutheast2]
        };

        /// <summary>
        /// The regions a customer can pick for production (EU-West-2, EU-Central-1, US-East-1, CA-Central-1,
        /// AP-Southeast-2).
        /// </summary>
        public static readonly IReadOnlyDictionary<PciPalRegion, string> ProductionRegionLabels = new Dictionary<PciPalRegion, string>
        {
            [PciPalRegion.EuWest2] = PciPalRegionLabels[PciPalRegion.EuWest2],
            [PciPalRegion.EuCentral1] = PciPalRegionLabels[PciPalRegion.EuCentral1],
            [PciPalRegion.UsEast1] = PciPalRegionLabels[PciPalRegion.UsEast1],
            [PciPalRegion.CaCentral1] = PciPalRegionLabels[PciPalRegion.CaCentral1],
            [PciPalRegion.ApSoutheast2] = PciPalRegionLabels[PciPalRegion.ApSoutheast2]
        };

        /// <summary>The production regions that are ticked by default when a payment region is chosen.</summary>
        public static readonly IReadOnlyDictionary<PaymentRegion, IReadOnlyList<PciPalRegion>> DefaultProductionRegionsFor =
            new Dictionary<PaymentRegion, IReadOnlyList<PciPalRegion>>
            {
                [PaymentRegion.Emea] = new[] { PciPalRegion.EuWest2 },
                [PaymentRegion.NorthAmerica] = new[] { PciPalRegion.UsEast1 },
                [PaymentRegion.AsiaPacific] = new[] { PciPalRegion.ApSoutheast2 }
            };

        /// <summary>The staging region that is ticked by default when a payment region is chosen.</summary>
        public static readonly IReadOnlyDictionary<PaymentRegion, PciPalRegion> DefaultStagingRegionFor = new Dictionary<PaymentRegion, PciPalRegion>
        {
            [PaymentRegion.Emea] = PciPalRegion.EuWest1,
            [PaymentRegion.NorthAmerica] = PciPalRegion.CaCentral1,
            [PaymentRegion.AsiaPacific] = PciPalRegion.ApSoutheast2
        };

        public static readonly IReadOnlyDictionary<OutboundAccess, string> OutboundAccessLabels = new Dictionary<OutboundAccess, string>
        {
            [OutboundAccess.Permitted] = "Yes — outbound connections are permitted",
            [OutboundAccess.RequiresAllowList] = "Yes — but only to allow-listed addresses",
            [OutboundAccess.NotPermitted] = "No — outbound connections are blocked",
            [OutboundAccess.NotSure] = "Not sure"
        };

        // ----- Payment configuration -----

        /// <summary>Stored in <see cref="Entities.ProjectPaymentConfiguration.MaxPaymentAttempts"/> for no per-session cap.</summary>
        public const int UnlimitedPaymentAttempts = 0;

        /// <summary>The choices for the number of payment attempts, in the order they are listed: 1 to 5, then Unlimited.</summary>
        public static readonly IReadOnlyList<int> PaymentAttemptOptions = new[] { 1, 2, 3, 4, 5, UnlimitedPaymentAttempts };

        public static bool IsAllowedMaxPaymentAttempts(int value) => PaymentAttemptOptions.Contains(value);

        public static string FormatMaxPaymentAttempts(int? value) => value switch
        {
            null => "—",
            UnlimitedPaymentAttempts => "Unlimited",
            _ => value.Value.ToString()
        };

        public static readonly IReadOnlyDictionary<CardScheme, string> CardSchemeLabels = new Dictionary<CardScheme, string>
        {
            [CardScheme.Visa] = "Visa",
            [CardScheme.Mastercard] = "Mastercard",
            [CardScheme.AmericanExpress] = "American Express",
            [CardScheme.Discover] = "Discover",
            [CardScheme.Maestro] = "Maestro",
            [CardScheme.DinersClub] = "Diners Club",
            [CardScheme.Jcb] = "JCB",
            [CardScheme.UnionPay] = "UnionPay"
        };

        public static readonly IReadOnlyDictionary<PaymentType, string> PaymentTypeLabels = new Dictionary<PaymentType, string>
        {
            [PaymentType.Authorization] = "Authorization",
            [PaymentType.CaptureSale] = "Capture / Sale",
            [PaymentType.Tokenization] = "Tokenization"
        };

        public static readonly IReadOnlyDictionary<CurrencyCode, string> CurrencyLabels = new Dictionary<CurrencyCode, string>
        {
            [CurrencyCode.GBP] = "GBP — British pound",
            [CurrencyCode.USD] = "USD — US dollar",
            [CurrencyCode.EUR] = "EUR — Euro",
            [CurrencyCode.AUD] = "AUD — Australian dollar",
            [CurrencyCode.CAD] = "CAD — Canadian dollar",
            [CurrencyCode.CHF] = "CHF — Swiss franc",
            [CurrencyCode.SEK] = "SEK — Swedish krona",
            [CurrencyCode.NOK] = "NOK — Norwegian krone",
            [CurrencyCode.DKK] = "DKK — Danish krone",
            [CurrencyCode.JPY] = "JPY — Japanese yen",
            [CurrencyCode.SGD] = "SGD — Singapore dollar",
            [CurrencyCode.AED] = "AED — UAE dirham"
        };

        public static readonly IReadOnlyDictionary<CardNumberMasking, string> CardNumberMaskingLabels = new Dictionary<CardNumberMasking, string>
        {
            [CardNumberMasking.FullyMasked] = "Fully Masked",
            [CardNumberMasking.PartiallyMasked] = "Partially Masked",
            [CardNumberMasking.Last4] = "Last 4"
        };

        public static readonly IReadOnlyDictionary<MerchantIdentification, string> MerchantIdentificationLabels = new Dictionary<MerchantIdentification, string>
        {
            [MerchantIdentification.Currency] = "Based on currency",
            [MerchantIdentification.Country] = "Based on country",
            [MerchantIdentification.Other] = "Others"
        };

        public static readonly IReadOnlyDictionary<SmsServiceProvider, string> SmsServiceProviderLabels = new Dictionary<SmsServiceProvider, string>
        {
            [SmsServiceProvider.TwilioPciPalAccount] = "Twilio (PCI Pal Account)",
            [SmsServiceProvider.Other] = "Other",
            [SmsServiceProvider.None] = "None"
        };

        public static readonly IReadOnlyDictionary<OtherSmsProvider, string> OtherSmsProviderLabels = new Dictionary<OtherSmsProvider, string>
        {
            [OtherSmsProvider.Twilio] = "Twilio",
            [OtherSmsProvider.Vonage] = "Vonage",
            [OtherSmsProvider.MessageBird] = "MessageBird",
            [OtherSmsProvider.Sinch] = "Sinch",
            [OtherSmsProvider.Infobip] = "Infobip",
            [OtherSmsProvider.Other] = "Other"
        };

        public static readonly IReadOnlyDictionary<EmailServiceProvider, string> EmailServiceProviderLabels = new Dictionary<EmailServiceProvider, string>
        {
            [EmailServiceProvider.SendGridPciPalAccount] = "SendGrid (PCI Pal Account)",
            [EmailServiceProvider.Other] = "Other",
            [EmailServiceProvider.None] = "None"
        };

        public static readonly IReadOnlyDictionary<OtherEmailProvider, string> OtherEmailProviderLabels = new Dictionary<OtherEmailProvider, string>
        {
            [OtherEmailProvider.SendGrid] = "SendGrid",
            [OtherEmailProvider.Mailgun] = "Mailgun",
            [OtherEmailProvider.AmazonSes] = "Amazon SES",
            [OtherEmailProvider.Microsoft365] = "Microsoft 365",
            [OtherEmailProvider.Postmark] = "Postmark",
            [OtherEmailProvider.SparkPost] = "SparkPost",
            [OtherEmailProvider.Other] = "Other"
        };

        // ----- Gateway configuration -----

        public static readonly IReadOnlyDictionary<PostTransactionMethod, string> PostTransactionMethodLabels = new Dictionary<PostTransactionMethod, string>
        {
            [PostTransactionMethod.WebCallback] = "Web Callback",
            [PostTransactionMethod.Polling] = "Polling",
            [PostTransactionMethod.PostMessage] = "Post Message",
            [PostTransactionMethod.Redirect] = "Redirect",
            [PostTransactionMethod.WebSocket] = "Web Socket"
        };

        /// <summary>Post-transaction methods offered on Adaptive Configuration (Redirect is not used for embedded CRM).</summary>
        public static IReadOnlyDictionary<PostTransactionMethod, string> PostTransactionMethodLabelsFor(CrmPaymentExperience? paymentExperience)
        {
            if (paymentExperience == CrmPaymentExperience.EmbeddedIframe)
            {
                return PostTransactionMethodLabels
                    .Where(kv => kv.Key != PostTransactionMethod.Redirect)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            return PostTransactionMethodLabels;
        }

        public static readonly IReadOnlyDictionary<ContactCentrePlatform, string> ContactCentrePlatformLabels = new Dictionary<ContactCentrePlatform, string>
        {
            [ContactCentrePlatform.Genesys] = "Genesys",
            [ContactCentrePlatform.Five9] = "Five9",
            [ContactCentrePlatform.Avaya] = "Avaya",
            [ContactCentrePlatform.NiceCxOne] = "NICE CXone",
            [ContactCentrePlatform.CiscoWebex] = "Cisco Webex Contact Center",
            [ContactCentrePlatform.TwilioFlex] = "Twilio Flex",
            [ContactCentrePlatform.Other] = "Other"
        };

        public static readonly IReadOnlyDictionary<ContactCentreConnectionType, string> ContactCentreConnectionTypeLabels = new Dictionary<ContactCentreConnectionType, string>
        {
            [ContactCentreConnectionType.QuickConnect] = "Quick Connect",
            [ContactCentreConnectionType.SipTrunk] = "SIP trunk",
            [ContactCentreConnectionType.Api] = "API",
            [ContactCentreConnectionType.Other] = "Other"
        };

    }
}
