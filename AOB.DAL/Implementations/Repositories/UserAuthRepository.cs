using AOB.BAL.Common.Enums;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Data access for the single authentication table.</summary>
    public class UserAuthRepository(OnboardingDbContext context) : IUserAuthRepository
    {
        public async Task<UserAuth?> GetByIdAsync(Guid id)
            => await context.UserAuths
                .Include(u => u.InternalUser)
                .Include(u => u.Partner)
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<UserAuth?> GetByNormalizedEmailAsync(string normalizedEmail)
            => await context.UserAuths
                .Include(u => u.InternalUser)
                .Include(u => u.Partner)
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        public async Task<bool> EmailExistsAsync(string normalizedEmail)
            => await context.UserAuths.AnyAsync(u => u.NormalizedEmail == normalizedEmail);

        public async Task<IReadOnlyList<UserAuth>> GetInternalAccountsAsync()
            => await context.UserAuths.AsNoTracking()
                .Include(u => u.InternalUser)
                .Where(u => u.UserType == UserType.SuperAdmin || u.UserType == UserType.InternalUser)
                .OrderBy(u => u.InternalUser!.FullName)
                .ToListAsync();

        public async Task<UserAuth?> GetInternalUserAccountAsync(Guid id)
            => await context.UserAuths
                .Include(u => u.InternalUser)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType == UserType.InternalUser && u.InternalUser != null);

        public async Task AddAsync(UserAuth userAuth)
            => await context.UserAuths.AddAsync(userAuth);
    }
}
