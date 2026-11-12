using AOB.BAL.Common.Enums;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Data access for the single authentication table.</summary>
    public interface IUserAuthRepository
    {
        Task<UserAuth?> GetByIdAsync(Guid id);

        /// <summary>
        /// Finds a user by normalized email, including the Internal/Partner/Customer profile.
        /// The returned entity is tracked so login bookkeeping can be saved.
        /// </summary>
        Task<UserAuth?> GetByNormalizedEmailAsync(string normalizedEmail);

        /// <summary>
        /// True if any account, of any type, already uses the normalized email. Sign-in looks accounts up
        /// by email alone, so an email must be unique across the whole auth table.
        /// </summary>
        Task<bool> EmailExistsAsync(string normalizedEmail);

        /// <summary>Admin and internal-user accounts with their profile, by name.</summary>
        Task<IReadOnlyList<UserAuth>> GetInternalAccountsAsync();

        /// <summary>One internal-user account (not the super admin) with its profile, tracked for updates; null if none.</summary>
        Task<UserAuth?> GetInternalUserAccountAsync(Guid id);

        /// <summary>Adds the user (and any attached profile). Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddAsync(UserAuth userAuth);
    }
}
