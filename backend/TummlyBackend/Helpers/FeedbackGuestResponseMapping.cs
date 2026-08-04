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

        /// <summary>
        /// Masks guest contact for UI/activity — never returns raw address/phone.
        /// </summary>
        public static string MaskDestination(
            ContactType contactType,
            string guestContact
        )
        {
            var trimmed = guestContact.Trim();
            if (contactType == ContactType.Email)
            {
                var at = trimmed.IndexOf('@');
                if (at <= 0 || at == trimmed.Length - 1)
                {
                    return "••••";
                }

                var local = trimmed[..at];
                var domain = trimmed[(at + 1)..];
                var maskedLocal =
                    local.Length <= 1 ? "•" : $"{local[0]}••••";
                return $"{maskedLocal}@{domain}";
            }

            if (contactType == ContactType.Phone)
            {
                var digits = new string(
                    trimmed.Where(char.IsDigit).ToArray()
                );
                if (digits.Length < 4)
                {
                    return "••••";
                }

                return $"••••{digits[^4..]}";
            }

            return "••••";
        }
    }
}
