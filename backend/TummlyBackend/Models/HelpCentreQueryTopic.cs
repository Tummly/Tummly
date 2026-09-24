namespace TummlyBackend.Models
{
    public enum HelpCentreQueryTopic
    {
        StartingWithTummly,
        PlansAndPricing,
        MultiLocationSetup,
        QrMaterialsOrStarterKit,
        ExistingAccount,
        BillingOrSubscription,
        PrivacyOrDataRequest,
        PartnershipMediaOrCompany,
        SomethingElse,
    }

    public static class HelpCentreQueryTopicExtensions
    {
        public static string ToSlug(this HelpCentreQueryTopic topic) =>
            topic switch
            {
                HelpCentreQueryTopic.StartingWithTummly => "starting-with-tummly",
                HelpCentreQueryTopic.PlansAndPricing => "plans-and-pricing",
                HelpCentreQueryTopic.MultiLocationSetup => "multi-location-setup",
                HelpCentreQueryTopic.QrMaterialsOrStarterKit =>
                    "qr-materials-or-starter-kit",
                HelpCentreQueryTopic.ExistingAccount => "existing-account",
                HelpCentreQueryTopic.BillingOrSubscription =>
                    "billing-or-subscription",
                HelpCentreQueryTopic.PrivacyOrDataRequest =>
                    "privacy-or-data-request",
                HelpCentreQueryTopic.PartnershipMediaOrCompany =>
                    "partnership-media-or-company",
                HelpCentreQueryTopic.SomethingElse => "something-else",
                _ => topic.ToString(),
            };

        public static string ToDisplayLabel(this HelpCentreQueryTopic topic) =>
            topic switch
            {
                HelpCentreQueryTopic.StartingWithTummly =>
                    "Starting with Tummly",
                HelpCentreQueryTopic.PlansAndPricing =>
                    "Plans and pricing",
                HelpCentreQueryTopic.MultiLocationSetup =>
                    "Multi-Location setup",
                HelpCentreQueryTopic.QrMaterialsOrStarterKit =>
                    "QR materials or Starter Kit",
                HelpCentreQueryTopic.ExistingAccount =>
                    "Help with an existing Tummly account",
                HelpCentreQueryTopic.BillingOrSubscription =>
                    "Billing or subscription",
                HelpCentreQueryTopic.PrivacyOrDataRequest =>
                    "Privacy or data request",
                HelpCentreQueryTopic.PartnershipMediaOrCompany =>
                    "Partnership, media or company enquiry",
                HelpCentreQueryTopic.SomethingElse =>
                    "Something else",
                _ => topic.ToString(),
            };

        public static bool TryFromSlug(
            string? slug,
            out HelpCentreQueryTopic topic
        )
        {
            var normalized = slug?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (normalized)
            {
                case "starting-with-tummly":
                    topic = HelpCentreQueryTopic.StartingWithTummly;
                    return true;
                case "plans-and-pricing":
                    topic = HelpCentreQueryTopic.PlansAndPricing;
                    return true;
                case "multi-location-setup":
                    topic = HelpCentreQueryTopic.MultiLocationSetup;
                    return true;
                case "qr-materials-or-starter-kit":
                    topic = HelpCentreQueryTopic.QrMaterialsOrStarterKit;
                    return true;
                case "existing-account":
                    topic = HelpCentreQueryTopic.ExistingAccount;
                    return true;
                case "billing-or-subscription":
                    topic = HelpCentreQueryTopic.BillingOrSubscription;
                    return true;
                case "privacy-or-data-request":
                    topic = HelpCentreQueryTopic.PrivacyOrDataRequest;
                    return true;
                case "partnership-media-or-company":
                    topic = HelpCentreQueryTopic.PartnershipMediaOrCompany;
                    return true;
                case "something-else":
                    topic = HelpCentreQueryTopic.SomethingElse;
                    return true;
                default:
                    topic = default;
                    return false;
            }
        }

        public static HelpCentreQueryTopic FromSlug(string slug)
        {
            if (TryFromSlug(slug, out var topic))
            {
                return topic;
            }

            throw new ArgumentException("Invalid query topic.");
        }
    }
}
