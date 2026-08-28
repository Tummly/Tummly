using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed record BillingAlertCta(string? Label, string? Href);

    public enum BillingAlertEventKind
    {
        CreditThreshold80Or90,
        CreditThreshold100Paid,
        CreditThreshold100Pilot,
        PaymentFailureDunning,
        UnpaidPilotLock,
    }

    /// <summary>
    /// Resolves per-recipient CTAs following ticket 11 / lock 10 permission table.
    /// </summary>
    public static class BillingAlertCtaResolver
    {
        public static BillingAlertCta Resolve(
            BillingAlertEventKind eventKind,
            PermissionLevel billingCreditsLevel,
            string permissionRole,
            string accountType,
            int locationId,
            string? channel = null
        )
        {
            if (billingCreditsLevel == PermissionLevel.NoAccess)
            {
                return new BillingAlertCta(null, null);
            }

            var canManage = billingCreditsLevel == PermissionLevel.Manage;
            var canView = DefaultPermissionMatrix.Meets(
                billingCreditsLevel,
                PermissionLevel.View
            );
            var isOwner = permissionRole == PermissionRoles.Owner;
            var root = string.Equals(accountType, "Multi", StringComparison.Ordinal)
                ? "/multi-dashboard"
                : "/single-dashboard";
            var location = locationId.ToString();

            return eventKind switch
            {
                BillingAlertEventKind.CreditThreshold80Or90 when canView =>
                    TabCta(root, location, "credits-usage", "View usage"),
                BillingAlertEventKind.CreditThreshold100Paid when canManage =>
                    ManagePlanCta(
                        root,
                        location,
                        "credit-top-ups",
                        channel,
                        channel == null
                            ? "Buy credits"
                            : $"Buy {BillingAlertChannelLabels.LabelFor(channel)}"
                    ),
                BillingAlertEventKind.CreditThreshold100Paid when canView =>
                    TabCta(root, location, "credits-usage", "View usage"),
                BillingAlertEventKind.CreditThreshold100Pilot
                    when canManage && isOwner =>
                    ManagePlanCta(root, location, null, null, "Change plan"),
                BillingAlertEventKind.CreditThreshold100Pilot when canView =>
                    TabCta(root, location, "credits-usage", "View usage"),
                BillingAlertEventKind.PaymentFailureDunning when canManage =>
                    TabCta(
                        root,
                        location,
                        "payment-invoices",
                        "Update payment method"
                    ),
                BillingAlertEventKind.PaymentFailureDunning when canView =>
                    TabCta(
                        root,
                        location,
                        "payment-invoices",
                        "Payment & invoices"
                    ),
                BillingAlertEventKind.UnpaidPilotLock when canManage && isOwner =>
                    TabCta(
                        root,
                        location,
                        "plan-subscription",
                        "Choose a plan"
                    ),
                BillingAlertEventKind.UnpaidPilotLock when canView =>
                    TabCta(
                        root,
                        location,
                        "plan-subscription",
                        "Plan & subscription"
                    ),
                _ => new BillingAlertCta(null, null),
            };
        }

        public static BillingAlertEventKind CreditThresholdEventKind(
            int thresholdBand,
            bool isPilot
        )
        {
            if (thresholdBand is 80 or 90)
            {
                return BillingAlertEventKind.CreditThreshold80Or90;
            }

            if (thresholdBand != 100)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdBand));
            }

            return isPilot
                ? BillingAlertEventKind.CreditThreshold100Pilot
                : BillingAlertEventKind.CreditThreshold100Paid;
        }

        private static BillingAlertCta TabCta(
            string root,
            string locationId,
            string tab,
            string label
        )
        {
            return new BillingAlertCta(
                label,
                $"{root}/settings/billing-credits?location={locationId}&tab={tab}"
            );
        }

        private static BillingAlertCta ManagePlanCta(
            string root,
            string locationId,
            string? section,
            string? channel,
            string label
        )
        {
            var query = new List<string> { $"location={locationId}" };
            if (section != null)
            {
                query.Add($"section={section}");
            }

            if (channel != null)
            {
                query.Add($"channel={channel}");
            }

            return new BillingAlertCta(
                label,
                $"{root}/settings/billing-credits/manage-plan?{string.Join("&", query)}"
            );
        }
    }
}
