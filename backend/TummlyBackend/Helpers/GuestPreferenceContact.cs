using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Parses and masks the email or UK phone a guest types into the
    /// guest preference centre. Phones are UK only (+44).
    /// </summary>
    public static class GuestPreferenceContact
    {
        private const string MaskedLocalSuffix = "•••••";

        private const string MaskedPhonePrefix = "•••• ••• ";

        private static readonly Regex EmailPattern = new(
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
            RegexOptions.CultureInvariant
        );

        public static bool TryParse(
            string? raw,
            out string contactType,
            out string normalizedValue
        )
        {
            contactType = string.Empty;
            normalizedValue = string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var trimmed = raw.Trim();

            var email = trimmed.ToLowerInvariant();
            if (EmailPattern.IsMatch(email))
            {
                contactType = TummlyGuestContactTypes.Email;
                normalizedValue = email;
                return true;
            }

            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    trimmed,
                    PhoneNumberHelper.DefaultRegion,
                    out var e164
                )
                && e164 != null
                && e164.StartsWith("+44", StringComparison.Ordinal)
            )
            {
                contactType = TummlyGuestContactTypes.Phone;
                normalizedValue = e164;
                return true;
            }

            return false;
        }

        public static string Mask(string contactType, string normalizedValue)
        {
            return contactType switch
            {
                TummlyGuestContactTypes.Email => MaskEmail(normalizedValue),
                TummlyGuestContactTypes.Phone => MaskPhone(normalizedValue),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(contactType),
                    contactType,
                    "Unknown guest contact type."
                ),
            };
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at < 0)
            {
                return MaskedLocalSuffix;
            }

            var local = email[..at];
            var visible = local[..Math.Min(2, local.Length)];
            return visible + MaskedLocalSuffix + email[at..];
        }

        private static string MaskPhone(string e164)
        {
            var last = e164.Length <= 3 ? e164 : e164[^3..];
            return MaskedPhonePrefix + last;
        }
    }
}
