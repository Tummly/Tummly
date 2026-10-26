using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Parsed guest search query. Contact fields match exactly, never by
    /// substring, so a masked contact cannot be found by trial.
    /// </summary>
    /// <param name="NameTerm">Lower-case text matched by substring on guest name.</param>
    /// <param name="ExactEmail">Lower-case email matched exactly, or null.</param>
    /// <param name="PhoneDigitForms">
    /// Stored NormalizedPhone forms matched exactly (international then
    /// national). Empty when the query is not a full UK mobile.
    /// </param>
    public sealed record GuestContactSearchTerm(
        string NameTerm,
        string? ExactEmail,
        IReadOnlyList<string> PhoneDigitForms
    )
    {
        public static GuestContactSearchTerm Parse(string query)
        {
            var trimmed = (query ?? string.Empty).Trim();
            var nameTerm = trimmed.ToLowerInvariant();

            string? exactEmail = null;
            if (
                GuestFormContactValidate.TryResolve(trimmed, out var contactType)
                && contactType == ContactType.Email
            )
            {
                exactEmail = trimmed.ToLowerInvariant();
            }

            var phoneForms = new List<string>();
            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    trimmed,
                    PhoneNumberHelper.DefaultRegion,
                    out var e164
                )
                && e164 != null
            )
            {
                var intlDigits = e164.TrimStart('+');
                var countryCode = PhoneNumberHelper.UkCountryCode.ToString();
                var nationalDigits =
                    "0" + intlDigits[countryCode.Length..];
                phoneForms.Add(intlDigits);
                phoneForms.Add(nationalDigits);
            }

            return new GuestContactSearchTerm(nameTerm, exactEmail, phoneForms);
        }
    }
}
