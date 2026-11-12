using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A user the customer wants an account for, read from the CSV they upload (name, email, role).
    /// Each row belongs to a project, so we always know which project the users are for.
    /// Stored in the CustomerUsers table.
    /// </summary>
    public class CustomerUser
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        public CustomerUserRole Role { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Project Project { get; set; } = null!;
    }
}
