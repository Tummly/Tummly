using PhoneNumbers;

namespace TummlyBackend.Helpers
{
    public static class PhoneNumberHelper
    {
        private static readonly PhoneNumberUtil Util =
            PhoneNumberUtil.GetInstance();

        public const string DefaultRegion = "GB";

        public const int UkCountryCode = 44;

        public static string NormalizeToE164(
            string phoneNumber,
            string defaultRegion = DefaultRegion
        )
        {
            if (!TryNormalizeToE164(phoneNumber, defaultRegion, out var e164))
            {
                throw new ArgumentException(
                    "Please enter a valid UK phone number.",
                    nameof(phoneNumber)
                );
            }

            return e164!;
        }

        public static bool TryNormalizeToE164(
            string phoneNumber,
            string defaultRegion,
            out string? e164
        )
        {
            e164 = null;

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            var region = ResolveRegion(defaultRegion);

            try
            {
                var trimmed = phoneNumber.Trim();
                var parsed = trimmed.StartsWith('+')
                    ? Util.Parse(trimmed, null)
                    : Util.Parse(trimmed, region);

                if (!Util.IsValidNumber(parsed) || !IsUkNumber(parsed))
                {
                    return false;
                }

                e164 = Util.Format(parsed, PhoneNumberFormat.E164);
                return true;
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        private static bool IsUkNumber(PhoneNumber parsed) =>
            parsed.CountryCode == UkCountryCode;

        public static string? NormalizeOptional(
            string? phoneNumber,
            string defaultRegion = DefaultRegion
        )
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            return NormalizeToE164(phoneNumber, defaultRegion);
        }

        public static string FormatForDisplay(string e164PhoneNumber)
        {
            if (string.IsNullOrWhiteSpace(e164PhoneNumber))
            {
                return string.Empty;
            }

            try
            {
                var parsed = Util.Parse(e164PhoneNumber.Trim(), DefaultRegion);

                if (IsUkNumber(parsed))
                {
                    return Util.Format(parsed, PhoneNumberFormat.NATIONAL);
                }

                return e164PhoneNumber.Trim();
            }
            catch (NumberParseException)
            {
                return e164PhoneNumber.Trim();
            }
        }

        private static string ResolveRegion(string defaultRegion)
        {
            if (string.IsNullOrWhiteSpace(defaultRegion))
            {
                return DefaultRegion;
            }

            var trimmed = defaultRegion.Trim().ToUpperInvariant();

            if (trimmed.Length == 2 && char.IsLetter(trimmed[0]))
            {
                return trimmed;
            }

            return trimmed.TrimStart('+') switch
            {
                "44" => DefaultRegion,
                _ => DefaultRegion,
            };
        }
    }
}
