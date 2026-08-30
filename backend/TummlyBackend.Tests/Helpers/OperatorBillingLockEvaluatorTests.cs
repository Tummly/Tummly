using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class OperatorBillingLockEvaluatorTests
    {
        private static readonly DateTime Now = new(
            2026,
            8,
            28,
            12,
            0,
            0,
            DateTimeKind.Utc
        );

        [Fact]
        public void SendOrReserve_PastDueDay7_ReturnsPastDueSendsBlocked()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.PastDue,
                ChargebackRestricted: false,
                DunningEpisodeStartedAt: Now.AddDays(-7)
            );

            Assert.Equal(
                OperatorBillingLockEvaluator.PastDueSendsBlocked,
                OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(account, Now)
            );
        }

        [Fact]
        public void SendOrReserve_PastDueDay6_Allows()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.PastDue,
                ChargebackRestricted: false,
                DunningEpisodeStartedAt: Now.AddDays(-6).AddHours(-23)
            );

            Assert.Null(
                OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(account, Now)
            );
        }

        [Fact]
        public void PaidWrite_SoftLock_ReturnsSoftLock()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.SoftLock,
                ChargebackRestricted: false,
                DunningEpisodeStartedAt: null
            );

            Assert.Equal(
                OperatorBillingLockEvaluator.SoftLock,
                OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(account)
            );
        }

        [Fact]
        public void Restoration_Chargeback_Wins()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.SoftLock,
                ChargebackRestricted: true,
                DunningEpisodeStartedAt: null
            );

            Assert.Equal(
                OperatorBillingLockEvaluator.ChargebackRestricted,
                OperatorBillingLockEvaluator.EvaluateRestorationDeny(account)
            );
            Assert.Null(
                OperatorBillingLockEvaluator.EvaluateRestorationDeny(
                    account with
                    {
                        ChargebackRestricted = false,
                    }
                )
            );
        }

        [Fact]
        public void SendOrReserve_Chargeback_WinsOverPastDue()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.PastDue,
                ChargebackRestricted: true,
                DunningEpisodeStartedAt: Now.AddDays(-8)
            );

            Assert.Equal(
                OperatorBillingLockEvaluator.ChargebackRestricted,
                OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(account, Now)
            );
        }

        [Fact]
        public void PaidWrite_PastDue_Allows()
        {
            var account = new OperatorBillingLockEvaluator.AccountLockState(
                BillingStatuses.PastDue,
                ChargebackRestricted: false,
                DunningEpisodeStartedAt: Now.AddDays(-8)
            );

            Assert.Null(OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(account));
        }
    }
}
