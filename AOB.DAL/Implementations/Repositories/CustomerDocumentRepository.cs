using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Customer document data access.</summary>
    public class CustomerDocumentRepository(OnboardingDbContext context) : ICustomerDocumentRepository
    {
        public async Task<IReadOnlyList<CustomerDocument>> GetByCustomerAsync(Guid customerId)
            => await context.CustomerDocuments.AsNoTracking()
                .Where(d => d.CustomerId == customerId)
                .ToListAsync();

        public async Task<CustomerDocument?> GetAsync(Guid customerId, Guid documentTypeId)
            => await context.CustomerDocuments
                .FirstOrDefaultAsync(d => d.CustomerId == customerId && d.DocumentTypeId == documentTypeId);

        public async Task AddAsync(CustomerDocument document)
            => await context.CustomerDocuments.AddAsync(document);

        public void Remove(CustomerDocument document)
            => context.CustomerDocuments.Remove(document);
    }
}
