using AOB.BAL.Common.Enums;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Data access for verification tokens, OTPs and reset grants.</summary>
    public interface IOtpTokenRepository
    {
        /// <summary>Finds a token by purpose and hash (tracked).</summary>
        Task<OtpToken?> GetByHashAsync(OtpPurpose purpose, string tokenHash);

        /// <summary>Latest token for the user/purpose that is neither consumed nor expired (tracked).</summary>
        Task<OtpToken?> GetLatestActiveAsync(Guid userAuthId, OtpPurpose purpose);

        /// <summary>Marks every active token for the user/purpose as consumed, so only one is valid at a time.</summary>
        Task InvalidateActiveAsync(Guid userAuthId, OtpPurpose purpose);

        /// <summary>How many tokens of this purpose were issued to the user since the given time (consumed or not).</summary>
        Task<int> CountCreatedSinceAsync(Guid userAuthId, OtpPurpose purpose, DateTimeOffset since);

        /// <summary>Adds the token. Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddAsync(OtpToken token);
    }
}
