using AOB.BAL.Entities;

namespace AOB.BAL.Common.Helpers
{
    public static class CustomerStatusRules
    {
        /// <summary>
        /// A partner-created customer is onboarded straight away: only its name is captured, nothing more is
        /// asked of it. A direct customer (signed up on its own) is onboarded once the profile is complete.
        /// Documents never matter. Does not consider the stored <see cref="Customer.Status"/> disabled flag.
        /// </summary>
        public static bool IsOnboarded(Customer customer)
            => !customer.IsDirect || IsProfileComplete(customer);

        /// <summary>
        /// The only fields that decide onboarding for a direct customer: company name, registration number,
        /// primary contact name and email, technical contact name and email.
        /// </summary>
        public static bool IsProfileComplete(Customer customer)
            => Filled(customer.CompanyName)
               && Filled(customer.CompanyRegistrationNumber)
               && Filled(customer.PrimaryContactFullName)
               && Filled(customer.PrimaryContactEmail)
               && Filled(customer.TechnicalContactFullName)
               && Filled(customer.TechnicalContactEmail);

        private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value);
    }
}
