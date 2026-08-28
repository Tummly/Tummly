namespace TummlyBackend.Notifications
{
    public sealed record NotificationTypeDefinition(
        string Type,
        string Category,
        string? Capability
    );

    /// <summary>
    /// Closed catalog of Notification types. Category and capability are derived here;
    /// producers cannot override them.
    /// </summary>
    public static class NotificationTypeRegistry
    {
        private static readonly Dictionary<string, NotificationTypeDefinition> ByType =
            new(StringComparer.Ordinal)
            {
                ["tip-place-qr-materials"] = new(
                    "tip-place-qr-materials",
                    "tips-and-playbooks",
                    null
                ),
                ["tip-preview-guest-form"] = new(
                    "tip-preview-guest-form",
                    "tips-and-playbooks",
                    null
                ),
                ["product-operator-home-live"] = new(
                    "product-operator-home-live",
                    "product-updates",
                    null
                ),
                ["password-changed"] = new(
                    "password-changed",
                    "account-notices",
                    null
                ),
                ["new-sign-in"] = new(
                    "new-sign-in",
                    "account-notices",
                    null
                ),
                ["activation-ending-15-days"] = new(
                    "activation-ending-15-days",
                    "account-notices",
                    null
                ),
                ["activation-ending-5-days"] = new(
                    "activation-ending-5-days",
                    "account-notices",
                    null
                ),
                ["activation-expired"] = new(
                    "activation-expired",
                    "account-notices",
                    null
                ),
                ["weekly-brief-ready"] = new(
                    "weekly-brief-ready",
                    "weekly-brief-reminders",
                    null
                ),
                ["campaign-update"] = new(
                    "campaign-update",
                    "campaign-and-report-updates",
                    "campaigns:read"
                ),
                ["report-update"] = new(
                    "report-update",
                    "campaign-and-report-updates",
                    "reports:read"
                ),
                ["offer-update"] = new(
                    "offer-update",
                    "campaign-and-report-updates",
                    "offers:read"
                ),
                ["offer-void-request-pending"] = new(
                    "offer-void-request-pending",
                    "campaign-and-report-updates",
                    "offers:read"
                ),
                ["offer-void-request-outcome"] = new(
                    "offer-void-request-outcome",
                    "campaign-and-report-updates",
                    "offers:read"
                ),
                ["credit-warning-80"] = new(
                    "credit-warning-80",
                    "account-notices",
                    null
                ),
                ["credit-warning-90"] = new(
                    "credit-warning-90",
                    "account-notices",
                    null
                ),
                ["credit-warning-100"] = new(
                    "credit-warning-100",
                    "account-notices",
                    null
                ),
                ["payment-failure-day-0"] = new(
                    "payment-failure-day-0",
                    "account-notices",
                    null
                ),
                ["payment-failure-day-3"] = new(
                    "payment-failure-day-3",
                    "account-notices",
                    null
                ),
                ["payment-failure-day-7"] = new(
                    "payment-failure-day-7",
                    "account-notices",
                    null
                ),
                ["payment-failure-day-10"] = new(
                    "payment-failure-day-10",
                    "account-notices",
                    null
                ),
                ["payment-failure-day-24"] = new(
                    "payment-failure-day-24",
                    "account-notices",
                    null
                ),
                ["unpaid-pilot-lock"] = new(
                    "unpaid-pilot-lock",
                    "account-notices",
                    null
                ),
            };

        private static readonly HashSet<string> AllowedCapabilities =
            new(StringComparer.Ordinal)
            {
                "campaigns:read",
                "reports:read",
                "offers:read",
            };

        public static bool TryGet(
            string type,
            out NotificationTypeDefinition definition
        )
        {
            if (ByType.TryGetValue(type, out definition!))
            {
                if (definition.Capability != null
                    && !AllowedCapabilities.Contains(definition.Capability))
                {
                    definition = null!;
                    return false;
                }

                return true;
            }

            definition = null!;
            return false;
        }
    }
}
