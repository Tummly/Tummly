namespace TummlyBackend.Helpers
{
    public static class OperatorAreaIds
    {
        public const string AccountWorkspace = "account-workspace";
        public const string Locations = "locations";
        public const string TeamPermissions = "team-permissions";
        public const string Capture = "capture";
        public const string Feedback = "feedback";
        public const string Guests = "guests";
        public const string Campaigns = "campaigns";
        public const string Offers = "offers";
        public const string Reports = "reports";
        public const string TummlyShop = "tummly-shop";
        public const string BillingCredits = "billing-credits";
        public const string PrivacyConsent = "privacy-consent";
        public const string AiAssistant = "ai-assistant";

        public static readonly IReadOnlyList<string> All =
        [
            AccountWorkspace,
            Locations,
            TeamPermissions,
            Capture,
            Feedback,
            Guests,
            Campaigns,
            Offers,
            Reports,
            TummlyShop,
            BillingCredits,
            PrivacyConsent,
            AiAssistant,
        ];
    }

    public enum PermissionLevel
    {
        NoAccess = 0,
        View = 1,
        Scoped = 2,
        Manage = 3,
    }
}
