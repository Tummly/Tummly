using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Compares a phone number against a restaurant-scoped Master Guest by
    /// E.164 value. Shared by guest-initiated STOP withdraw and Tummly Guest
    /// identity matching.
    /// </summary>
    public static class GuestPhoneMatch
    {
        public static bool MasterGuestMatchesE164(
            MasterGuest master,
            string inputE164
        )
        {
            if (
                TryResolveComparableE164(master.Mobile, out var fromMobile)
                && string.Equals(
                    fromMobile,
                    inputE164,
                    StringComparison.Ordinal
                )
            )
            {
                return true;
            }

            return TryResolveComparableE164(
                    master.NormalizedPhone,
                    out var fromNormalized
                )
                && string.Equals(
                    fromNormalized,
                    inputE164,
                    StringComparison.Ordinal
                );
        }

        /// <summary>
        /// Prefer <see cref="PhoneNumberHelper.TryNormalizeToE164"/>; accept
        /// Twilio-style <c>+digits</c> when libphonenumber rejects (test ranges).
        /// Digits-only values that start with 44 are treated as E.164 without +.
        /// </summary>
        public static bool TryResolveComparableE164(
            string? raw,
            out string e164
        )
        {
            e164 = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    raw,
                    PhoneNumberHelper.DefaultRegion,
                    out var normalized
                )
                && !string.IsNullOrEmpty(normalized)
            )
            {
                e164 = normalized;
                return true;
            }

            var trimmed = raw.Trim();
            if (
                trimmed.StartsWith('+')
                && trimmed.Length >= 10
                && trimmed[1..].All(char.IsDigit)
            )
            {
                e164 = trimmed;
                return true;
            }

            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (
                digits.Length >= 10
                && digits.StartsWith("44", StringComparison.Ordinal)
            )
            {
                e164 = "+" + digits;
                return true;
            }

            return false;
        }
    }
}
