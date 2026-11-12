using System.Text.RegularExpressions;
using AOB.BAL.Common.Validation;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Documents;

namespace AOB.BAL.Common.Helpers
{
    public static class CustomerProfileCompletionRules
    {
        private static bool IsStrictEmail(string? email)
            => !string.IsNullOrWhiteSpace(email)
               && Regex.IsMatch(email.Trim(), StrictEmailValidation.Pattern);
        public static bool HasRequiredCompanyFields(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.CompanyName)
               && !string.IsNullOrWhiteSpace(profile.RegisteredCountryCode);

        /// <summary>Minimum profile fields before showing the read-only overview.</summary>
        public static bool ShouldForceProfileEdit(CustomerProfileDto profile)
            => !HasRequiredCompanyFields(profile);

        public static int RequiredDocumentsMissingCount(IReadOnlyList<DocumentChecklistItemDto> documents)
            => documents.Count(d => d.IsRequired && !d.IsUploaded);

        public static bool AreRequiredDocumentsComplete(IReadOnlyList<DocumentChecklistItemDto> documents)
            => RequiredDocumentsMissingCount(documents) == 0;

        public static bool HasAnyContactDetails(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.PrimaryContactFullName)
               || !string.IsNullOrWhiteSpace(profile.PrimaryContactEmail)
               || !string.IsNullOrWhiteSpace(profile.TechnicalContactFullName)
               || !string.IsNullOrWhiteSpace(profile.TechnicalContactEmail);

        public static bool IsCompanySectionComplete(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.CompanyName)
               && !string.IsNullOrWhiteSpace(profile.CompanyRegistrationNumber)
               && !string.IsNullOrWhiteSpace(profile.RegisteredCountryCode);

        public static bool IsPrimaryContactSectionComplete(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.PrimaryContactFullName)
               && IsStrictEmail(profile.PrimaryContactEmail);

        public static bool IsTechnicalContactSectionComplete(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.TechnicalContactFullName)
               && IsStrictEmail(profile.TechnicalContactEmail);

        public static bool IsOperatingHoursSectionComplete(CustomerProfileDto profile)
            => !string.IsNullOrWhiteSpace(profile.OperatingPattern)
               && !string.IsNullOrWhiteSpace(profile.PrimaryTimeZone);

        /// <summary>Optional partner block: complete only when a partner is involved; otherwise not applicable.</summary>
        public static bool IsTechnologyPartnerSectionNotApplicable(CustomerProfileDto profile)
            => !profile.HasTechnologyPartner;

        public static bool IsTechnologyPartnerSectionComplete(CustomerProfileDto profile)
            => profile.HasTechnologyPartner;

        /// <summary>Prototype org profile: company, primary contact, technical contact, operating hours.</summary>
        public static int RequiredProfileSectionsComplete(CustomerProfileDto profile)
        {
            var n = 0;
            if (IsCompanySectionComplete(profile)) n++;
            if (IsPrimaryContactSectionComplete(profile)) n++;
            if (IsTechnicalContactSectionComplete(profile)) n++;
            if (IsOperatingHoursSectionComplete(profile)) n++;
            return n;
        }

        public static bool IsOrganisationSectionComplete(string sectionKey, CustomerProfileDto profile)
            => sectionKey switch
            {
                "company" => IsCompanySectionComplete(profile),
                "pc" => IsPrimaryContactSectionComplete(profile),
                "tc" => IsTechnicalContactSectionComplete(profile),
                "hours" => IsOperatingHoursSectionComplete(profile),
                "partner" => IsTechnologyPartnerSectionComplete(profile),
                _ => false
            };
    }
}
