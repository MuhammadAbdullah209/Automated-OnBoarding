namespace AOB.BAL.Common.Enums
{
    // Enums for "Adaptive Configuration" (gateway configuration and gateway fields).

    /// <summary>
    /// How PCI Pal returns payment data to the customer's system after a transaction (multiple choice).
    /// Stored comma-separated in ProjectGatewayConfiguration.PostTransactionMethods.
    /// </summary>
    public enum PostTransactionMethod
    {
        WebCallback = 1,
        Polling = 2,
        PostMessage = 3,
        Redirect = 4,
        WebSocket = 5
    }

    /// <summary>Contact centre platform the agents use (single choice).</summary>
    public enum ContactCentrePlatform
    {
        Genesys = 1,
        Five9 = 2,
        Avaya = 3,
        NiceCxOne = 4,
        CiscoWebex = 5,
        TwilioFlex = 6,
        Other = 7
    }

    /// <summary>How the telephony is connected to PCI Pal (single choice).</summary>
    public enum ContactCentreConnectionType
    {
        QuickConnect = 1,
        SipTrunk = 2,
        Api = 3,
        Other = 4
    }

    /// <summary>
    /// What kind of gateway field a ProjectGatewayFields row is. The numbers are fixed
    /// (0, 1, 2) because they are stored in the database.
    /// </summary>
    public enum GatewayFieldType
    {
        /// <summary>Identifier PCI Pal needs to authenticate with the gateway.</summary>
        Authentication = 0,

        /// <summary>Always needed; the provider requires it.</summary>
        Required = 1,

        /// <summary>Needed only in some setups; included if the customer selects it.</summary>
        Conditional = 2,

        /// <summary>Not needed by the integration; may be included if the customer wants it.</summary>
        Optional = 3
    }
}
