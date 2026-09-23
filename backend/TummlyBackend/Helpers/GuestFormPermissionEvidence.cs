using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Guest Form permission evidence — launch version ids and wording
    /// snapshots stamped on ledger grant/withdraw rows.
    /// </summary>
    public static class GuestFormPermissionEvidence
    {
        public const string GuestFormVersion = "guest-form-v1";
        public const string PrivacyNoticeVersion = "privacy-notice-v1";
        public const string EmailMarketingWordingVersion = "email-marketing-v1";
        public const string SmsMarketingWordingVersion = "sms-marketing-v1";
        public const string FeedbackFollowUpWordingVersion =
            "feedback-follow-up-v2";

        private const int MaxSnapshotLength = 512;

        public static PermissionLedgerEvidence ForFeedbackFollowUpGrant(
            string restaurantName,
            string locationName
        )
        {
            var restaurant = DisplayRestaurantName(restaurantName);
            var location = DisplayLocationName(locationName);
            var snapshot =
                $"Your feedback is shared privately with the team at {restaurant}, {location}. "
                + GuestFormConsentCopy.FeedbackFollowUpWording;

            return new PermissionLedgerEvidence(
                Basis: LocationGuestPermissionBases.ServiceFollowUpNotice,
                GuestFormVersion: GuestFormVersion,
                WordingVersion: FeedbackFollowUpWordingVersion,
                PrivacyNoticeVersion: PrivacyNoticeVersion,
                WordingSnapshot: TruncateSnapshot(snapshot)
            );
        }

        public static PermissionLedgerEvidence ForMarketingGrant(
            ContactType contactType,
            string restaurantName,
            string? restaurantCustomWording
        ) =>
            BuildMarketingEvidence(
                contactType,
                restaurantName,
                restaurantCustomWording,
                basis: LocationGuestPermissionBases.Consent
            );

        public static PermissionLedgerEvidence ForMarketingWithdraw(
            ContactType contactType,
            string restaurantName,
            string? restaurantCustomWording
        ) =>
            BuildMarketingEvidence(
                contactType,
                restaurantName,
                restaurantCustomWording,
                basis: null
            );

        private static PermissionLedgerEvidence BuildMarketingEvidence(
            ContactType contactType,
            string restaurantName,
            string? restaurantCustomWording,
            string? basis
        )
        {
            var wordingVersion = contactType switch
            {
                ContactType.Email => EmailMarketingWordingVersion,
                ContactType.Phone => SmsMarketingWordingVersion,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(contactType),
                    contactType,
                    "Marketing evidence requires Email or Phone contact type."
                ),
            };

            var snapshot = ResolveMarketingSnapshot(
                contactType,
                restaurantName,
                restaurantCustomWording
            );

            return new PermissionLedgerEvidence(
                Basis: basis,
                GuestFormVersion: GuestFormVersion,
                WordingVersion: wordingVersion,
                PrivacyNoticeVersion: PrivacyNoticeVersion,
                WordingSnapshot: TruncateSnapshot(snapshot)
            );
        }

        private static string ResolveMarketingSnapshot(
            ContactType contactType,
            string restaurantName,
            string? restaurantCustomWording
        )
        {
            if (!string.IsNullOrWhiteSpace(restaurantCustomWording))
            {
                return restaurantCustomWording.Trim();
            }

            var display = DisplayRestaurantName(restaurantName);
            return contactType switch
            {
                ContactType.Email =>
                    $"Yes, email me occasional offers and updates from {display}. "
                    + "You can unsubscribe at any time.",
                ContactType.Phone =>
                    $"Yes, text me occasional offers and updates from {display}. "
                    + "You can opt out at any time.",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(contactType),
                    contactType,
                    "Marketing evidence requires Email or Phone contact type."
                ),
            };
        }

        private static string DisplayRestaurantName(string restaurantName)
        {
            var trimmed = (restaurantName ?? string.Empty).Trim();
            return trimmed.Length == 0 ? "this restaurant" : trimmed;
        }

        private static string DisplayLocationName(string locationName)
        {
            var trimmed = (locationName ?? string.Empty).Trim();
            return trimmed.Length == 0 ? "this location" : trimmed;
        }

        private static string TruncateSnapshot(string snapshot)
        {
            if (snapshot.Length <= MaxSnapshotLength)
            {
                return snapshot;
            }

            return snapshot[..MaxSnapshotLength];
        }
    }
}
