using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>The configured payment gateways customers can choose from.</summary>
    public interface IPaymentGatewayService
    {
        Task<IReadOnlyList<PaymentGatewayDto>> GetGatewaysAsync();
    }
}
