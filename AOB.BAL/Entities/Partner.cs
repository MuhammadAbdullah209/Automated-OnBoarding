using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Partner organisation.
    /// PCIPAL is seeded at startup and owns all direct customers.
    /// </summary>
    public class Partner
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Auth))]
        public Guid UserAuthId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public UserAuth Auth { get; set; } = null!;

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
