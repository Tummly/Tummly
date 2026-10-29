namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable Location Guest permission kind for one Owned location.
    /// Wire values align with Operator Privacy &amp; consent product ids.
    /// </summary>
    public enum LocationGuestPermissionKind
    {
        EmailMarketing,
        SmsMarketing,
        FeedbackFollowUp,

        /// <summary>
        /// Suppression-only kind for a confirmed permanent Email hard bounce.
        /// It is a delivery fact, not a guest consent. It is never a ledger
        /// permission and is not in <see cref="LocationGuestPermissionKindExtensions.All"/>.
        /// </summary>
        EmailDelivery,
    }

    public static class LocationGuestPermissionKindExtensions
    {
        public const string EmailMarketingWire = "email-marketing";
        public const string SmsMarketingWire = "sms-marketing";
        public const string FeedbackFollowUpWire = "feedback-follow-up";
        public const string EmailDeliveryWire = "email-delivery";

        public static string ToWireString(this LocationGuestPermissionKind kind) =>
            kind switch
            {
                LocationGuestPermissionKind.EmailMarketing => EmailMarketingWire,
                LocationGuestPermissionKind.SmsMarketing => SmsMarketingWire,
                LocationGuestPermissionKind.FeedbackFollowUp =>
                    FeedbackFollowUpWire,
                LocationGuestPermissionKind.EmailDelivery => EmailDeliveryWire,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Unknown Location Guest permission kind."
                ),
            };

        public static LocationGuestPermissionKind FromWireString(string stored)
        {
            if (!TryFromWireString(stored, out var kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stored),
                    stored,
                    "Unknown Location Guest permission kind."
                );
            }

            return kind;
        }

        public static bool TryFromWireString(
            string? stored,
            out LocationGuestPermissionKind kind
        )
        {
            kind = default;
            if (string.IsNullOrWhiteSpace(stored))
            {
                return false;
            }

            switch (stored.Trim())
            {
                case EmailMarketingWire:
                    kind = LocationGuestPermissionKind.EmailMarketing;
                    return true;
                case SmsMarketingWire:
                    kind = LocationGuestPermissionKind.SmsMarketing;
                    return true;
                case FeedbackFollowUpWire:
                    kind = LocationGuestPermissionKind.FeedbackFollowUp;
                    return true;
                case EmailDeliveryWire:
                    kind = LocationGuestPermissionKind.EmailDelivery;
                    return true;
                default:
                    return false;
            }
        }

        public static IReadOnlyList<LocationGuestPermissionKind> All { get; } =
            new[]
            {
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionKind.FeedbackFollowUp,
            };
    }
}
