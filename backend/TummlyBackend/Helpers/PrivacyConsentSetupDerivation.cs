using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class PrivacyConsentSetupDerivation
    {
        public const string StatusConfigured = "Configured";
        public const string StatusEnabled = "Enabled";
        public const string StatusNotUsed = "Not used";

        public static IReadOnlyList<PrivacySetupStatusRow> BuildSetupRows(
            Restaurant restaurant
        )
        {
            return
            [
                new PrivacySetupStatusRow(
                    "privacy-notice",
                    "Privacy notice",
                    StatusConfigured
                ),
                new PrivacySetupStatusRow(
                    "email-marketing",
                    "Email marketing",
                    restaurant.EmailMarketingPermissionEnabled
                        ? StatusEnabled
                        : StatusNotUsed
                ),
                new PrivacySetupStatusRow(
                    "sms-marketing",
                    "SMS marketing",
                    restaurant.SmsMarketingPermissionEnabled
                        ? StatusEnabled
                        : StatusNotUsed
                ),
                new PrivacySetupStatusRow(
                    "feedback-follow-up",
                    "Feedback follow-up",
                    restaurant.FeedbackFollowUpPermissionEnabled
                        ? StatusEnabled
                        : StatusNotUsed
                ),
            ];
        }
    }

    public sealed record PrivacySetupStatusRow(
        string Id,
        string Requirement,
        string Status
    );
}
