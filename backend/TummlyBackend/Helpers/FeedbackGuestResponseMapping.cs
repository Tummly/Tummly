using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackGuestResponseMapping
    {
        public static string ToWireChannel(FeedbackGuestResponseChannel channel)
            => channel switch
            {
                FeedbackGuestResponseChannel.Email => "email",
                FeedbackGuestResponseChannel.Sms => "sms",
                _ => "email",
            };

        public static bool TryParseChannel(
            string? wire,
            out FeedbackGuestResponseChannel channel
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "email":
                    channel = FeedbackGuestResponseChannel.Email;
                    return true;
                case "sms":
                    channel = FeedbackGuestResponseChannel.Sms;
                    return true;
                default:
                    channel = default;
                    return false;
            }
        }

        public static string ToWireIntent(FeedbackRecoveryIntent intent)
            => FeedbackInternalActionMapping.ToWireIntent(intent);

        public static bool TryParseIntent(
            string? wire,
            out FeedbackRecoveryIntent intent
        )
            => FeedbackInternalActionMapping.TryParseIntent(wire, out intent);
    }
}
