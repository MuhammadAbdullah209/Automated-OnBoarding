using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    public class CustomerStatusService(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IActivityLogService activityLog) : ICustomerStatusService
    {
        public async Task RefreshDerivedStatusAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var customer = await customerRepository.GetByIdAsync(customerId);
            if (customer is null || customer.Status == CustomerStatus.Disabled)
            {
                return;
            }

            var complete = CustomerStatusRules.IsOnboarded(customer);
            var previous = customer.Status;
            customer.Status = complete ? CustomerStatus.Onboarded : CustomerStatus.Pending;
            customer.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            if (previous != customer.Status)
            {
                await activityLog.LogCustomerAsync(customerId, $"Status changed to {customer.Status}",
                    complete
                        ? (customer.IsDirect ? "Company name, registration number and both contacts are filled in." : "Customers added by a partner are onboarded straight away.")
                        : "Company name, registration number or a contact name/email is missing.");
            }
        }

        public async Task<ServiceResult> SetDisabledAsync(Guid customerId, bool disabled, CancellationToken cancellationToken = default)
        {
            var customer = await customerRepository.GetByIdAsync(customerId);
            if (customer is null)
            {
                return ServiceResult.Failure("Customer not found.");
            }

            if (disabled)
            {
                customer.Status = CustomerStatus.Disabled;
            }
            else
            {
                customer.Status = CustomerStatus.Pending;
            }

            customer.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogCustomerAsync(customerId, disabled ? "Customer disabled" : "Customer enabled",
                disabled ? "The customer can no longer sign in." : "The customer can sign in again.");

            if (!disabled)
            {
                await RefreshDerivedStatusAsync(customerId, cancellationToken);
            }

            return ServiceResult.Success();
        }
    }
}
