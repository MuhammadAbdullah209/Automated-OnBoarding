namespace AOB.BAL.Common.Enums
{
    // Enums for "Technical Environment".

    /// <summary>
    /// Geographic area where customers make payments (multiple choice).
    /// Stored comma-separated in ProjectTechnicalEnvironment.PaymentRegions.
    /// </summary>
    public enum PaymentRegion
    {
        Emea = 1,
        NorthAmerica = 2,
        AsiaPacific = 3
    }

    /// <summary>
    /// A PCI Pal hosting region used for staging or production (multiple choice).
    /// Stored comma-separated in ProjectTechnicalEnvironment.StagingRegions / ProductionRegions.
    /// </summary>
    public enum PciPalRegion
    {
        EuWest1 = 1,
        EuWest2 = 2,
        EuCentral1 = 3,
        UsEast1 = 4,
        UsWest2 = 5,
        CaCentral1 = 6,
        ApSoutheast2 = 7
    }

    /// <summary>"Can your systems reach external service endpoints on the internet?" (single choice).</summary>
    public enum OutboundAccess
    {
        Permitted = 1,
        RequiresAllowList = 2,
        NotPermitted = 3,
        NotSure = 4
    }
}
