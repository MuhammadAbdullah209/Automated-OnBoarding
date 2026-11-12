using AOB.BAL.Common.Enums;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Data access for verification tokens, OTPs and reset grants.</summary>
    public class OtpTokenRepository(OnboardingDbContext context) : IOtpTokenRepository
    {
        public async Task<OtpToken?> GetByHashAsync(OtpPurpose purpose, string tokenHash)
            => await context.OtpTokens.FirstOrDefaultAsync(t => t.Purpose == purpose && t.TokenHash == tokenHash);

        public async Task<OtpToken?> GetLatestActiveAsync(Guid userAuthId, OtpPurpose purpose)
            => await context.OtpTokens
                .Where(t => t.UserAuthId == userAuthId
                            && t.Purpose == purpose
                            && t.ConsumedAt == null
                            && t.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

        public async Task InvalidateActiveAsync(Guid userAuthId, OtpPurpose purpose)
        {
            var active = await context.OtpTokens
                .Where(t => t.UserAuthId == userAuthId && t.Purpose == purpose && t.ConsumedAt == null)
                .ToListAsync();

            foreach (var token in active)
            {
                token.ConsumedAt = DateTimeOffset.UtcNow;
            }
        }

        public Task<int> CountCreatedSinceAsync(Guid userAuthId, OtpPurpose purpose, DateTimeOffset since)
            => context.OtpTokens.CountAsync(t => t.UserAuthId == userAuthId && t.Purpose == purpose && t.CreatedAt >= since);

        public async Task AddAsync(OtpToken token)
            => await context.OtpTokens.AddAsync(token);
    }
}
