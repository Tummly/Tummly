using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class BillingAlertCtaResolverTests
    {
        private const int LocationId = 42;

        [Theory]
        [InlineData(PermissionLevel.Manage, PermissionRoles.Owner)]
        [InlineData(PermissionLevel.View, PermissionRoles.Admin)]
        public void Credit80Or90_ViewUsage_WhenViewOrManage(
            PermissionLevel level,
            string role
        )
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold80Or90,
                level,
                role,
                "Single",
                LocationId
            );

            Assert.Equal("View usage", cta.Label);
            Assert.Equal(
                "/single-dashboard/settings/billing-credits?location=42&tab=credits-usage",
                cta.Href
            );
        }

        [Fact]
        public void Credit100Paid_BuyChannelCredits_WhenManage()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold100Paid,
                PermissionLevel.Manage,
                PermissionRoles.Owner,
                "Multi",
                LocationId,
                "email"
            );

            Assert.Equal("Buy Email credits", cta.Label);
            Assert.Equal(
                "/multi-dashboard/settings/billing-credits/manage-plan?location=42&section=credit-top-ups&channel=email",
                cta.Href
            );
        }

        [Fact]
        public void Credit100Paid_ViewUsage_WhenViewOnly()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold100Paid,
                PermissionLevel.View,
                PermissionRoles.Marketing,
                "Single",
                LocationId,
                "sms"
            );

            Assert.Equal("View usage", cta.Label);
        }

        [Fact]
        public void Credit100Pilot_ChangePlan_WhenOwnerManage()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold100Pilot,
                PermissionLevel.Manage,
                PermissionRoles.Owner,
                "Single",
                LocationId
            );

            Assert.Equal("Change plan", cta.Label);
            Assert.Contains("manage-plan", cta.Href);
        }

        [Fact]
        public void Credit100Pilot_ViewUsage_WhenBillingAdminManage()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold100Pilot,
                PermissionLevel.Manage,
                PermissionRoles.BillingAdmin,
                "Single",
                LocationId
            );

            Assert.Equal("View usage", cta.Label);
        }

        [Fact]
        public void PaymentFailure_UpdatePaymentMethod_WhenManage()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.PaymentFailureDunning,
                PermissionLevel.Manage,
                PermissionRoles.Owner,
                "Single",
                LocationId
            );

            Assert.Equal("Update payment method", cta.Label);
            Assert.Contains("tab=payment-invoices", cta.Href);
        }

        [Fact]
        public void PaymentFailure_PaymentInvoices_WhenView()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.PaymentFailureDunning,
                PermissionLevel.View,
                PermissionRoles.Admin,
                "Single",
                LocationId
            );

            Assert.Equal("Payment & invoices", cta.Label);
        }

        [Fact]
        public void UnpaidPilotLock_ChoosePlan_WhenOwnerManage()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.UnpaidPilotLock,
                PermissionLevel.Manage,
                PermissionRoles.Owner,
                "Single",
                LocationId
            );

            Assert.Equal("Choose a plan", cta.Label);
            Assert.Contains("tab=plan-subscription", cta.Href);
        }

        [Fact]
        public void UnpaidPilotLock_PlanSubscription_WhenView()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.UnpaidPilotLock,
                PermissionLevel.View,
                PermissionRoles.Admin,
                "Single",
                LocationId
            );

            Assert.Equal("Plan & subscription", cta.Label);
            Assert.Contains("tab=plan-subscription", cta.Href);
        }

        [Fact]
        public void NoAccess_ReturnsNullCta()
        {
            var cta = BillingAlertCtaResolver.Resolve(
                BillingAlertEventKind.CreditThreshold80Or90,
                PermissionLevel.NoAccess,
                PermissionRoles.Staff,
                "Single",
                LocationId
            );

            Assert.Null(cta.Label);
            Assert.Null(cta.Href);
        }
    }
}
