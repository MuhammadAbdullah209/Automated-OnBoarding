namespace AOB.BAL.Common.Validation
{
    /// <summary>Shared pattern for organisation contact emails (requires a TLD such as .com).</summary>
    public static class StrictEmailValidation
    {
        public const string Pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        public const string ErrorMessage = "Enter a valid email address.";
    }
}
