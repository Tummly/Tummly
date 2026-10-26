using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Billing row for tests that expect live operator writes to succeed.
    /// A Free account is refused by the plan gate (WS3b), so such seeds use Pilot.
    /// </summary>
    public static class PilotBillingAccountSeed
    {
        public static BillingAccount Create(int restaurantId, string pricebookId)
        {
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurantId,
                pricebookId
            );
            account.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
            account.BillingStatus = BillingStatuses.Pilot;
            return account;
        }
    }
}
