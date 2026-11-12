namespace AOB.BAL.Common.Constants
{
    /// <summary>
    /// Well-known partner data. Direct customers are always owned by PCIPAL.
    /// The id must match the seed data in OnboardingDbContext.
    /// </summary>
    public static class PartnerConstants
    {
        public static readonly Guid PcipalPartnerId = new("6F1B2C3D-4E5F-4A6B-8C7D-9E0F1A2B3C4D");

        public const string PcipalPartnerName = "PCIPAL";
    }
}
