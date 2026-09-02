using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Shop materials order one-time Revolut order HPP (ticket 16).
    /// </summary>
    public interface IShopMaterialsOrderPaySession
    {
        Task<string> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            ShopOrder order,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        );
    }
}
