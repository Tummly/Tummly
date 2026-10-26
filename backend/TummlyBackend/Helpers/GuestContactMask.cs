using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Masks guest contact for every ordinary operator response — never returns
    /// a raw address or phone number. Malformed non-blank input yields "••••",
    /// never an empty string.
    /// </summary>
    public static class GuestContactMask
    {
        public static string Mask(ContactType contactType, string guestContact)
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

        /// <summary>
        /// Feedback rows with no stored contact keep an empty string; any
        /// stored value is masked.
        /// </summary>
        public static string MaskOrEmpty(ContactType contactType, string? guestContact)
            => string.IsNullOrWhiteSpace(guestContact)
                ? string.Empty
                : Mask(contactType, guestContact);

        public static string? MaskEmail(string? email)
            => string.IsNullOrWhiteSpace(email)
                ? null
                : Mask(ContactType.Email, email);

        public static string? MaskPhone(string? phone)
            => string.IsNullOrWhiteSpace(phone)
                ? null
                : Mask(ContactType.Phone, phone);
    }
}
