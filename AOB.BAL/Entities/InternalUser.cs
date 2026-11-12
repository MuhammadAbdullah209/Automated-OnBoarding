using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Profile of an internal (staff) user.
    /// </summary>
    public class InternalUser
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Auth))]
        public Guid UserAuthId { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public UserAuth Auth { get; set; } = null!;
    }
}
