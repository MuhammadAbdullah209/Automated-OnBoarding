using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    public class UserAuth
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(320)]
        public string NormalizedEmail { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        public UserType UserType { get; set; }

        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// True while the password is a system-generated temporary one (internal users created by the
        /// super admin). The user can only reach the change-password page until they choose their own.
        /// </summary>
        public bool MustChangePassword { get; set; }

        public int AccessFailedCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public InternalUser? InternalUser { get; set; }
        public Partner? Partner { get; set; }
        public Customer? Customer { get; set; }
    }
}
