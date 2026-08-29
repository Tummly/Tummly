using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Pure operator billing lock codes (ticket 33). Product locks 10 / 12 win.
    /// </summary>
    public static class OperatorBillingLockEvaluator
    {
        public const string PastDueSendsBlocked = "past_due_sends_blocked";

        public const string SoftLock = "soft_lock";

        public const string Dormant = "dormant";

        public const string ChargebackRestricted = "chargeback_restricted";

        public static readonly TimeSpan PastDueSendBlockAge = TimeSpan.FromDays(7);

        public sealed record AccountLockState(
            string BillingStatus,
            bool ChargebackRestricted,
            DateTime? DunningEpisodeStartedAt
        );

        public static AccountLockState FromBillingAccount(BillingAccount account)
        {
            return new AccountLockState(
                account.BillingStatus,
                account.ChargebackRestricted,
                account.DunningEpisodeStartedAt
            );
        }

        /// <summary>
        /// Deny new send / Reserve / commit / Resume / retry / fire / recovery send / new AI.
        /// </summary>
        public static string? EvaluateSendOrReserveDeny(
            AccountLockState account,
            DateTime nowUtc
        )
        {
            if (account.ChargebackRestricted)
            {
                return ChargebackRestricted;
            }

            if (IsStatus(account.BillingStatus, BillingStatuses.Dormant))
            {
                return Dormant;
            }

            if (IsStatus(account.BillingStatus, BillingStatuses.SoftLock))
            {
                return SoftLock;
            }

            if (IsPastDueSendBlocked(account, nowUtc))
            {
                return PastDueSendsBlocked;
            }

            return null;
        }

        /// <summary>
        /// Deny Shop / top-up / cancel / extra Location / paid plan-change (non-restoration).
        /// </summary>
        public static string? EvaluatePaidWriteDeny(AccountLockState account)
        {
            if (account.ChargebackRestricted)
            {
                return ChargebackRestricted;
            }

            if (IsStatus(account.BillingStatus, BillingStatuses.Dormant))
            {
                return Dormant;
            }

            if (IsStatus(account.BillingStatus, BillingStatuses.SoftLock))
            {
                return SoftLock;
            }

            return null;
        }

        /// <summary>
        /// Deny restoration checkouts (unpaid-Pilot plan-change pay, payment-method/update)
        /// until Support clears chargeback overlay.
        /// </summary>
        public static string? EvaluateRestorationDeny(AccountLockState account)
        {
            return account.ChargebackRestricted ? ChargebackRestricted : null;
        }

        public static bool IsPastDueSendBlocked(
            AccountLockState account,
            DateTime nowUtc
        )
        {
            if (!IsStatus(account.BillingStatus, BillingStatuses.PastDue))
            {
                return false;
            }

            if (account.DunningEpisodeStartedAt is not DateTime started)
            {
                return false;
            }

            return nowUtc - started >= PastDueSendBlockAge;
        }

        private static bool IsStatus(string billingStatus, string expected)
        {
            return string.Equals(billingStatus, expected, StringComparison.Ordinal);
        }

        public static bool IsLockCode(string? code)
        {
            return code
                is SoftLock
                    or Dormant
                    or ChargebackRestricted
                    or PastDueSendsBlocked;
        }
    }
}
