namespace AOB.BAL.Common.Enums
{
    /// <summary>
    /// Discriminates which profile table (InternalUser / Partner / Customer)
    /// belongs to a UserAuth row. The name doubles as the role claim.
    /// SuperAdmin only creates internal users; InternalUser is staff who manage partners and configuration (both use the InternalUser profile table).
    /// </summary>
    public enum UserType
    {
        SuperAdmin = 1,
        Partner = 2,
        Customer = 3,
        InternalUser = 4
    }
}
