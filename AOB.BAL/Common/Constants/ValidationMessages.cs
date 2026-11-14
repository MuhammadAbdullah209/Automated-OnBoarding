namespace AOB.BAL.Common.Constants
{
    public static class ValidationMessages
    {
        public const string Required = "field is required";

        /// <summary>Sign-up email already used by any account (customer, partner, internal user or admin).</summary>
        public const string EmailInUse = "This email is already in use.";
        public const string EmailFormat = "Enter a valid email address, like name@company.com.";
        public const string EmailPattern = @"^[A-Za-z0-9._%+'-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$";
    }
}
