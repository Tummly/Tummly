using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Credit-pack top-up one-time Revolut order HPP (ticket 18).
    /// </summary>
    public interface ICreditTopUpPaySession
    {
        Task<string> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            PricebookTopUpPack pack,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        );
    }
}
