using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Data access for the configured payment gateways and their fields.</summary>
    public interface IPaymentGatewayRepository
    {
        /// <summary>All gateways, by name.</summary>
        Task<IReadOnlyList<PaymentGateway>> GetAllAsync();

        Task<PaymentGateway?> GetByIdAsync(Guid id);

        /// <summary>A gateway's fields: authentication first, then required, conditional and optional, each by name.</summary>
        Task<IReadOnlyList<PaymentGatewayField>> GetFieldsAsync(Guid gatewayId);
    }
}
