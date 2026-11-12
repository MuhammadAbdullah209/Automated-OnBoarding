using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>
    /// Saves the changes tracked by the request-scoped DbContext, which every repository shares.
    /// The DbContext is disposed by the DI container, not here.
    /// </summary>
    public class UnitOfWork(OnboardingDbContext context) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync() => await context.SaveChangesAsync();
    }
}
