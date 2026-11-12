using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Data access for the configured payment gateways and their fields.</summary>
    public class PaymentGatewayRepository(OnboardingDbContext context) : IPaymentGatewayRepository
    {
        public async Task<IReadOnlyList<PaymentGateway>> GetAllAsync()
            => await context.PaymentGateways.AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

        public async Task<PaymentGateway?> GetByIdAsync(Guid id)
            => await context.PaymentGateways.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

        public async Task<IReadOnlyList<PaymentGatewayField>> GetFieldsAsync(Guid gatewayId)
            => await context.PaymentGatewayFields.AsNoTracking()
                .Where(f => f.PaymentGatewayId == gatewayId)
                .OrderBy(f => f.FieldType).ThenBy(f => f.Name)
                .ToListAsync();
    }
}
