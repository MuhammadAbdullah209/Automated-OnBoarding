using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Customer document data access.</summary>
    public interface ICustomerDocumentRepository
    {
        /// <summary>All documents a customer has uploaded.</summary>
        Task<IReadOnlyList<CustomerDocument>> GetByCustomerAsync(Guid customerId);

        /// <summary>The customer's document for a type, if any (tracked, so it can be replaced).</summary>
        Task<CustomerDocument?> GetAsync(Guid customerId, Guid documentTypeId);

        Task AddAsync(CustomerDocument document);

        void Remove(CustomerDocument document);
    }
}
