using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestRetentionEligibilityTests
    {
        private static BillingAccount Dormant(DateTime entered) =>
            new()
            {
                RestaurantId = 1,
                BillingStatus = BillingStatuses.Dormant,
                DormantEnteredAt = entered,
            };

        [Fact]
        public void IsEligible_False_WhenTooEarly()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    Dormant(entered),
                    entered.AddDays(89)
                )
            );
        }

        [Fact]
        public void IsEligible_True_WhenDue()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.True(
                GuestRetentionEligibility.IsEligible(
                    Dormant(entered),
                    entered.AddDays(90)
                )
            );
        }

        [Fact]
        public void IsEligible_False_WhenAlreadyStamped()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = Dormant(entered);
            account.GuestRetentionPurgedAtUtc = entered.AddDays(90);
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    account,
                    entered.AddDays(100)
                )
            );
        }

        [Fact]
        public void IsEligible_False_WhenNotDormant()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = Dormant(entered);
            account.BillingStatus = BillingStatuses.Active;
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    account,
                    entered.AddDays(100)
                )
            );
        }
    }
}
