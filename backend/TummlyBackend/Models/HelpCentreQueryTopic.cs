namespace TummlyBackend.Models
{
    public enum HelpCentreQueryTopic
    {
        Setup,
        QrNotWorking,
        MaterialsDamaged,
        ReorderMaterials,
        GuestFeedback,
        OfferRedemption,
        Campaign,
        Billing,
        PrivacyData,
        RequestDemo,
        SomethingElse,
    }

    public static class HelpCentreQueryTopicExtensions
    {
        public static string ToSlug(this HelpCentreQueryTopic topic) =>
            topic switch
            {
                HelpCentreQueryTopic.Setup => "setup",
                HelpCentreQueryTopic.QrNotWorking => "qr-not-working",
                HelpCentreQueryTopic.MaterialsDamaged => "materials-damaged",
                HelpCentreQueryTopic.ReorderMaterials => "reorder-materials",
                HelpCentreQueryTopic.GuestFeedback => "guest-feedback",
                HelpCentreQueryTopic.OfferRedemption => "offer-redemption",
                HelpCentreQueryTopic.Campaign => "campaign",
                HelpCentreQueryTopic.Billing => "billing",
                HelpCentreQueryTopic.PrivacyData => "privacy-data",
                HelpCentreQueryTopic.RequestDemo => "request-demo",
                HelpCentreQueryTopic.SomethingElse => "something-else",
                _ => topic.ToString(),
            };

        public static string ToDisplayLabel(this HelpCentreQueryTopic topic) =>
            topic switch
            {
                HelpCentreQueryTopic.Setup =>
                    "I need help setting up Tummly",
                HelpCentreQueryTopic.QrNotWorking =>
                    "My QR code is not working",
                HelpCentreQueryTopic.MaterialsDamaged =>
                    "My printed materials are damaged or missing",
                HelpCentreQueryTopic.ReorderMaterials =>
                    "I need to reorder QR materials",
                HelpCentreQueryTopic.GuestFeedback =>
                    "I need help with guest feedback",
                HelpCentreQueryTopic.OfferRedemption =>
                    "I need help with an offer or redemption",
                HelpCentreQueryTopic.Campaign =>
                    "I need help with a campaign",
                HelpCentreQueryTopic.Billing =>
                    "I have a billing or credits question",
                HelpCentreQueryTopic.PrivacyData =>
                    "I need help with consent, privacy or data",
                HelpCentreQueryTopic.RequestDemo =>
                    "I want to request a demo",
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
                case "setup":
                    topic = HelpCentreQueryTopic.Setup;
                    return true;
                case "qr-not-working":
                    topic = HelpCentreQueryTopic.QrNotWorking;
                    return true;
                case "materials-damaged":
                    topic = HelpCentreQueryTopic.MaterialsDamaged;
                    return true;
                case "reorder-materials":
                    topic = HelpCentreQueryTopic.ReorderMaterials;
                    return true;
                case "guest-feedback":
                    topic = HelpCentreQueryTopic.GuestFeedback;
                    return true;
                case "offer-redemption":
                    topic = HelpCentreQueryTopic.OfferRedemption;
                    return true;
                case "campaign":
                    topic = HelpCentreQueryTopic.Campaign;
                    return true;
                case "billing":
                    topic = HelpCentreQueryTopic.Billing;
                    return true;
                case "privacy-data":
                    topic = HelpCentreQueryTopic.PrivacyData;
                    return true;
                case "request-demo":
                    topic = HelpCentreQueryTopic.RequestDemo;
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
