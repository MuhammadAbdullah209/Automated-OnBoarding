using AOB.BAL.DTOs.Projects;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>The configured payment gateways customers can choose from.</summary>
    public class PaymentGatewayService(IPaymentGatewayRepository gatewayRepository) : IPaymentGatewayService
    {
        public async Task<IReadOnlyList<PaymentGatewayDto>> GetGatewaysAsync()
        {
            var gateways = await gatewayRepository.GetAllAsync();
            return gateways.Select(g => new PaymentGatewayDto { Id = g.Id, Name = g.Name, Description = g.Description }).ToList();
        }
    }
}
