using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Documents;

namespace AOB.BAL.Common.Helpers
{
    public readonly record struct ReadinessRow(string Key, string Label, bool Complete);

    /// <summary>Account readiness rows aligned with the onboarding prototype home screen.</summary>
    public static class CustomerReadinessPresentation
    {
        public static IReadOnlyList<ReadinessRow> ProfileSections(CustomerProfileDto? profile)
        {
            var p = profile ?? new CustomerProfileDto();
            static bool EmailOk(string? email)
                => !string.IsNullOrWhiteSpace(email) && email.Contains('@');

            return
            [
                new("company", "Organisation details",
                    !string.IsNullOrWhiteSpace(p.CompanyName)
                    && !string.IsNullOrWhiteSpace(p.CompanyRegistrationNumber)
                    && !string.IsNullOrWhiteSpace(p.RegisteredCountryCode)),
                new("pc", "Primary project contact",
                    !string.IsNullOrWhiteSpace(p.PrimaryContactFullName) && EmailOk(p.PrimaryContactEmail)),
                new("tc", "Technical contact",
                    !string.IsNullOrWhiteSpace(p.TechnicalContactFullName) && EmailOk(p.TechnicalContactEmail)),
                new("hours", "Operating hours",
                    !string.IsNullOrWhiteSpace(p.OperatingPattern) && !string.IsNullOrWhiteSpace(p.PrimaryTimeZone))
            ];
        }

        public static (int Ready, int Total) Score(CustomerProfileDto? profile, IReadOnlyList<DocumentChecklistItemDto> documents)
        {
            var sections = ProfileSections(profile);
            var ready = sections.Count(s => s.Complete);
            var total = sections.Count;

            foreach (var doc in documents)
            {
                total++;
                if (doc.IsUploaded)
                {
                    ready++;
                }
            }

            return (ready, total);
        }
    }
}
