using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared bulk recovery body/subject tokens. Preview keeps the token;
    /// send substitutes per guest.
    /// </summary>
    public static partial class BulkRecoveryPersonalise
    {
        public const string GuestFirstNameToken = "{{guest_first_name}}";

        public const string FallbackFirstName = "there";

        /// <summary>
        /// Replace a leading "Hi {Name}," greeting with the shared token so the
        /// first eligible guest is not baked into the batch template.
        /// </summary>
        public static string EnsureGuestFirstNameToken(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return $"Hi {GuestFirstNameToken},";
            }

            if (body.Contains(GuestFirstNameToken, StringComparison.Ordinal))
            {
                return body;
            }

            var trimmed = body.TrimStart();
            if (LeadingHiName().IsMatch(trimmed))
            {
                return LeadingHiName().Replace(
                    trimmed,
                    $"Hi {GuestFirstNameToken},"
                );
            }

            return $"Hi {GuestFirstNameToken}, {trimmed}";
        }

        public static string SubstituteGuestFirstName(
            string template,
            string? guestName
        )
        {
            if (string.IsNullOrEmpty(template)
                || !template.Contains(GuestFirstNameToken, StringComparison.Ordinal))
            {
                return template;
            }

            var first = FirstName(guestName);
            return template.Replace(
                GuestFirstNameToken,
                first,
                StringComparison.Ordinal
            );
        }

        public static string FirstName(string? guestName)
        {
            if (string.IsNullOrWhiteSpace(guestName))
            {
                return FallbackFirstName;
            }

            var trimmed = guestName.Trim();
            var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
            if (space <= 0)
            {
                return trimmed;
            }

            return trimmed[..space];
        }

        [GeneratedRegex(
            @"^Hi\s+[^,\n]{1,80},",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex LeadingHiName();
    }
}
