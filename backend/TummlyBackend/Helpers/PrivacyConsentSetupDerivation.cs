using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class PrivacyConsentSetupDerivation
    {
        public const string StatusConfigured = "Configured";
        public const string StatusEnabled = "Enabled";
        public const string StatusNotUsed = "Not used";

        public static bool IsGuestPermissionWordingConfigured(Restaurant restaurant)
        {
            if (
                restaurant.EmailMarketingPermissionEnabled
                && string.IsNullOrWhiteSpace(restaurant.EmailConsentWording)
            )
            {
                return false;
            }

            if (
                restaurant.SmsMarketingPermissionEnabled
                && string.IsNullOrWhiteSpace(restaurant.SmsConsentWording)
            )
            {
                return false;
            }

            return true;
        }

        public static IReadOnlyList<PrivacySetupStatusRow> BuildSetupRows(
            Restaurant restaurant
        )
        {
            var wordingConfigured = IsGuestPermissionWordingConfigured(
                restaurant
            );
            var wordingStatus = wordingConfigured
                ? StatusConfigured
                : StatusNotUsed;

            return
            [
                new PrivacySetupStatusRow(
                    "privacy-notice",
                    "Privacy notice",
                    StatusConfigured
                ),
                new PrivacySetupStatusRow(
                    "guest-permission-wording",
                    "Guest permission wording",
                    wordingStatus
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
