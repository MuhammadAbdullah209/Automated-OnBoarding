using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Customer record. Two ways to get one:
    ///  - Direct: the customer registers itself (IsDirect = true), has a login (UserAuthId set)
    ///    and is owned by the default partner PCIPAL.
    ///  - Partner-created: a partner adds it with only a CompanyName (IsDirect = false).
    ///    It cannot log in, so UserAuthId is null and every other column is optional.
    /// </summary>
    public class Customer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Nullable 1:1 link to the single auth table; null for partner-created customers.
        [ForeignKey(nameof(Auth))]
        public Guid? UserAuthId { get; set; }

        public Guid PartnerId { get; set; }

        // True = self onboarded (owned by PCIPAL); false = created by a partner.
        public bool IsDirect { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Pending;

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(120)]
        public string? CompanyRegistrationNumber { get; set; }

        [MaxLength(2)]
        public string? RegisteredCountryCode { get; set; }

        [MaxLength(120)]
        public string? IndustrySector { get; set; }

        [MaxLength(500)]
        public string? CompanyWebsite { get; set; }

        // Primary Project Contact

        [MaxLength(200)]
        public string? PrimaryContactFullName { get; set; }

        [MaxLength(120)]
        public string? PrimaryContactJobTitle { get; set; }

        [EmailAddress, MaxLength(320)]
        public string? PrimaryContactEmail { get; set; }

        [Phone, MaxLength(40)]
        public string? PrimaryContactPhoneNumber { get; set; }

        // Technical / Support Contact

        [MaxLength(200)]
        public string? TechnicalContactFullName { get; set; }

        [MaxLength(120)]
        public string? TechnicalContactJobTitle { get; set; }

        [EmailAddress, MaxLength(320)]
        public string? TechnicalContactEmail { get; set; }

        [Phone, MaxLength(40)]
        public string? TechnicalContactPhoneNumber { get; set; }

        // Operating / Payment Hours

        // Store the selected option's stable code.
        [MaxLength(60)]
        public string? OperatingPattern { get; set; }

        // Store "Europe/London", not a fixed GMT/BST offset.
        [MaxLength(100)]
        public string? PrimaryTimeZone { get; set; }

        // Technology Partner

        // Separate from the owning PartnerId above.
        public bool HasTechnologyPartner { get; set; }

        // Record Management

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public UserAuth? Auth { get; set; }

        public Partner Partner { get; set; } = null!;
    }
}
