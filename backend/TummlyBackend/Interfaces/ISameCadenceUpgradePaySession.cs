using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Same-cadence upgrade pay-now one-time Revolut order (ticket 20).
    /// Operator never changes plan on a Revolut-hosted page.
    /// </summary>
    public interface ISameCadenceUpgradePaySession
    {
        Task<PlanChangeResultDto> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            string targetPlan,
            string targetCadenceApi,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        );
    }
}
