using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Internal (staff) accounts, managed by the super admin.</summary>
    public interface IInternalUserService
    {
        /// <summary>Lists the super admin and internal users by name.</summary>
        Task<IReadOnlyList<InternalUserSummaryDto>> GetAllAsync();

        /// <summary>One internal user (never the super admin), or null.</summary>
        Task<InternalUserSummaryDto?> GetAsync(Guid id);

        /// <summary>
        /// Creates an unverified internal user login (UserType.InternalUser) and emails a verification link.
        /// The temporary password is only generated and emailed once the link is used.
        /// Succeeds even if the email could not be sent; Data is then false so the caller can say so
        /// (the link can be re-sent with <see cref="ResendVerificationAsync"/>).
        /// </summary>
        Task<ServiceResult<bool>> CreateAsync(CreateInternalUserDto dto);

        /// <summary>Re-sends the verification link to an internal user who has not verified their email yet.</summary>
        Task<ServiceResult> ResendVerificationAsync(Guid id);

        /// <summary>
        /// Changes the name and login email of an internal user who has not verified their email yet
        /// (verified users cannot be edited), then emails a fresh verification link to the address now on
        /// the account; earlier links stop working. Data is false if that email could not be sent.
        /// </summary>
        Task<ServiceResult<bool>> UpdateAsync(Guid id, string fullName, string email);

        /// <summary>Activates or deactivates an internal user (an inactive user cannot sign in).</summary>
        Task<ServiceResult> SetActiveAsync(Guid id, bool isActive);
    }
}
