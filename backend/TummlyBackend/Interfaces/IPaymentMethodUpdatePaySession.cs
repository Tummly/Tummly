using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Paid-operator HPP session that collects or updates the default card only.
    /// Requires an existing <see cref="BillingAccount.RevolutCustomerId"/>.
    /// </summary>
    public interface IPaymentMethodUpdatePaySession
    {
        Task<PaymentMethodUpdateSessionDto> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
