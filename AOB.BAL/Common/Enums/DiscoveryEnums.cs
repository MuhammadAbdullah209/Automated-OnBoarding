namespace AOB.BAL.Common.Enums
{
    // Enums for "Business & Use-Case Discovery".

    /// <summary>
    /// "How do your customers currently make payments?" (multiple choice).
    /// Stored comma-separated in ProjectDiscovery.CurrentPaymentMethods.
    /// </summary>
    public enum CurrentPaymentMethod
    {
        PhoneCallWithAgent = 1,
        WebsiteOrOnlineCheckout = 2,
        PaymentLinkOrDigitalMessage = 3,
        AnotherWay = 4
    }

    /// <summary>"How would you like the payment experience to work in your CRM?" (single choice).</summary>
    public enum CrmPaymentExperience
    {
        EmbeddedIframe = 1,
        HostedByPciPal = 2
    }
}
