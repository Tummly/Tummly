using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// First paid conversion Hosted Payment Page session (ticket 14).
    /// Creates Revolut customer + pending subscription setup order only.
    /// Does not activate entitlements.
    /// </summary>
    public interface IFirstPaidConversionPaySession
    {
        Task<PlanChangeResultDto> StartAsync(
            BillingAccount billingAccount,
            User owner,
            string restaurantAccountType,
            int locationId,
            string targetPlan,
            string targetCadenceApi,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        );
    }
}
