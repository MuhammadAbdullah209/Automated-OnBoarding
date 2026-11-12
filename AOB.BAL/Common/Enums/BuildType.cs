namespace AOB.BAL.Common.Enums
{
    /// <summary>
    /// Whether a project can be provisioned as PCI Pal's standard offering or needs custom work.
    /// Decided by the domain rules in AOB.BAL.Domain.BuildRules when the project is submitted.
    /// </summary>
    public enum BuildType
    {
        /// <summary>Everything the customer chose is supported as-is: the project goes straight to provisioning.</summary>
        Standard = 1,

        /// <summary>Something needs custom work: the project waits for internal approval first.</summary>
        Custom = 2
    }
}
