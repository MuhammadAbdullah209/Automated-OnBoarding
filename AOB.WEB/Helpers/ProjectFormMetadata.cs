using AOB.BAL.Common.Enums;

namespace AOB.WEB.Helpers
{
    /// <summary>Labels, hints and icon keys for card-style project form options.</summary>
    public static class ProjectFormMetadata
    {
        public static readonly IReadOnlyDictionary<PciPalService, (string Hint, string Icon)> ServiceCards =
            new Dictionary<PciPalService, (string, string)>
            {
                [PciPalService.ClickToPay] = (
                    "Customers start a payment by selecting a button embedded in your digital experience.",
                    "link"),
                [PciPalService.SpeakToPay] = (
                    "Customers pay during a phone call with an agent, using PCI Pal's secure voice solution.",
                    "phone"),
                [PciPalService.KeyToPay] = (
                    "Customers enter card details on their phone keypad while on a call with an agent.",
                    "keypad")
            };

        public static readonly IReadOnlyDictionary<CurrentPaymentMethod, (string Hint, string Icon)> PaymentMethodCards =
            new Dictionary<CurrentPaymentMethod, (string, string)>
            {
                [CurrentPaymentMethod.PhoneCallWithAgent] = (
                    "A customer calls and an employee or agent is present while the payment is made",
                    "phone"),
                [CurrentPaymentMethod.WebsiteOrOnlineCheckout] = (
                    "Customers pay on your website, in an app, or through an online form.",
                    "card"),
                [CurrentPaymentMethod.PaymentLinkOrDigitalMessage] = (
                    "Customers receive a payment link by email, SMS or similar and pay independently.",
                    "link"),
                [CurrentPaymentMethod.AnotherWay] = (
                    "Payments happen in a different way. Describe it in the payment journey field.",
                    "help")
            };

        public static readonly IReadOnlyDictionary<CrmPaymentExperience, (string Hint, string Icon)> CrmExperienceCards =
            new Dictionary<CrmPaymentExperience, (string, string)>
            {
                [CrmPaymentExperience.EmbeddedIframe] = (
                    "The payment form appears inside your CRM.",
                    "layout"),
                [CrmPaymentExperience.HostedByPciPal] = (
                    "Customers use the PCI Pal payment page directly.",
                    "external")
            };

        public static readonly IReadOnlyDictionary<CardNumberMasking, string> CardMaskingPreviews =
            new Dictionary<CardNumberMasking, string>
            {
                [CardNumberMasking.FullyMasked] = "**** **** **** ****",
                [CardNumberMasking.PartiallyMasked] = "1234 56** **** 1234",
                [CardNumberMasking.Last4] = "**** **** **** 1234"
            };
    }
}
