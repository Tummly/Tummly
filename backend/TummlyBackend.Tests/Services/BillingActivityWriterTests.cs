using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class BillingActivityWriterTests
    {
        [Fact]
        public void TryAppend_OmitsUnknownKind()
        {
            using var context = BillingActivityTestDb.Create();
            var appended = BillingActivityWriter.TryAppend(
                context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = 1,
                    Kind = AccessActivityKinds.MemberRemoved,
                }
            );

            Assert.False(appended);
            Assert.Empty(context.RestaurantBillingActivities);
        }

        [Fact]
        public void TryAppend_OmitsWhenFromEqualsTo()
        {
            using var context = BillingActivityTestDb.Create();
            var appended = BillingActivityWriter.TryAppend(
                context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = 1,
                    Kind = BillingActivityKinds.SubscriptionChangeScheduled,
                    FromPlan = "Growth",
                    ToPlan = "Growth",
                    FromCadence = "Monthly",
                    ToCadence = "Monthly",
                }
            );

            Assert.False(appended);
            Assert.Empty(context.RestaurantBillingActivities);
        }
    }
}
