using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class GuestRetentionEligibility
    {
        public const int GuestRetentionDays = 90;

        public static bool IsEligible(BillingAccount account, DateTime nowUtc)
        {
            if (!string.Equals(
                account.BillingStatus,
                BillingStatuses.Dormant,
                StringComparison.Ordinal))
            {
                return false;
            }

            if (account.DormantEnteredAt is not DateTime dormantAt)
            {
                return false;
            }

            if (account.GuestRetentionPurgedAtUtc != null)
            {
                return false;
            }

            return nowUtc >= dormantAt.AddDays(GuestRetentionDays);
        }
    }
}
