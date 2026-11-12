namespace AOB.BAL.Common.Constants
{
    /// <summary>
    /// Role names for [Authorize(Roles = ...)]. A role is the UserType name.
    ///  - SuperAdmin: sees everything in the portal (read-only) and is the only role that creates internal users.
    ///  - InternalUser: sees everything and does the day-to-day management (creates partners, manages document types).
    ///  - Partner / Customer: see only their own data.
    /// </summary>
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string InternalUser = "InternalUser";

        /// <summary>Anyone on the internal side: may view partners, customers, projects and configuration.</summary>
        public const string InternalAccess = SuperAdmin + "," + InternalUser;
    }
}
