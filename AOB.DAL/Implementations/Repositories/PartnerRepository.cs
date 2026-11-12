using AOB.BAL.DTOs.Partners;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Partner data access.</summary>
    public class PartnerRepository(OnboardingDbContext context) : IPartnerRepository
    {
        public async Task<Partner?> GetByIdAsync(Guid id)
            => await context.Partners.FindAsync(id);

        public async Task<Partner?> GetWithAuthAsync(Guid id)
            => await context.Partners.Include(p => p.Auth).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<PartnerSummaryDto?> GetSummaryAsync(Guid id)
            => await Summaries().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IReadOnlyList<PartnerSummaryDto>> GetAllSummariesAsync()
            => await Summaries().OrderBy(p => p.Name).ToListAsync();

        private IQueryable<PartnerSummaryDto> Summaries()
            => context.Partners.AsNoTracking()
                .Select(p => new PartnerSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Email = p.Auth.Email,
                    IsActive = p.IsActive,
                    CustomerCount = p.Customers.Count,
                    CreatedAt = p.CreatedAt
                });
    }
}
