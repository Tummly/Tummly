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

            if (!TryParseValidUk(phoneNumber, defaultRegion, out var parsed))
            {
                return false;
            }

            e164 = Util.Format(parsed!, PhoneNumberFormat.E164);
            return true;
        }

        /// <summary>
        /// Guest contact only: valid UK mobile (libphonenumber MOBILE).
        /// Shared <see cref="TryNormalizeToE164"/> stays unchanged.
        /// </summary>
        public static bool TryNormalizeUkMobileToE164(
            string phoneNumber,
            string defaultRegion,
            out string? e164
        )
        {
            e164 = null;

            if (!TryParseUk(phoneNumber, defaultRegion, out var parsed))
            {
                return false;
            }

            if (!IsUkMobile(parsed!))
            {
                return false;
            }

            e164 = Util.Format(parsed!, PhoneNumberFormat.E164);
            return true;
        }

        /// <summary>
        /// Valid UK mobile (libphonenumber MOBILE). The Ofcom drama range
        /// 07700 900000–900999 that libphonenumber rejects is accepted only
        /// when <see cref="ReservedTestNumberPolicy.AllowReservedTestNumbers"/>
        /// is true. Guest contact only.
        /// </summary>
        private static bool IsUkMobile(PhoneNumber parsed)
        {
            if (Util.IsValidNumber(parsed))
            {
                return Util.GetNumberType(parsed) == PhoneNumberType.MOBILE;
            }

            return ReservedTestNumberPolicy.AllowReservedTestNumbers
                && IsReservedTestNationalNumber(
                    Util.GetNationalSignificantNumber(parsed)
                );
        }

        private static bool IsReservedTestNationalNumber(string nationalNumber) =>
            nationalNumber.Length == 10
            && nationalNumber.StartsWith("7700900", StringComparison.Ordinal);

        /// <summary>
        /// True for a UK number in the Ofcom reserved drama range 07700 900xxx,
        /// in any common spelling (national, +44, 0044). Checks digits only, so
        /// it works on raw input before libphonenumber normalisation. Ignores the
        /// policy. Used by the provider send guard.
        /// </summary>
        public static bool IsReservedTestRange(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0044", StringComparison.Ordinal))
            {
                digits = "44" + digits[4..];
            }

            if (digits.StartsWith("44", StringComparison.Ordinal))
            {
                digits = "0" + digits[2..];
            }

            return digits.Length == 11
                && digits.StartsWith("07700900", StringComparison.Ordinal);
        }

        private static bool TryParseValidUk(
            string phoneNumber,
            string defaultRegion,
            out PhoneNumber? parsed
        )
        {
            return TryParseUk(phoneNumber, defaultRegion, out parsed)
                && Util.IsValidNumber(parsed!);
        }

        private static bool TryParseUk(
            string phoneNumber,
            string defaultRegion,
            out PhoneNumber? parsed
        )
        {
            parsed = null;

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            var region = ResolveRegion(defaultRegion);

            try
            {
                var trimmed = phoneNumber.Trim();
                var candidate = trimmed.StartsWith('+')
                    ? Util.Parse(trimmed, null)
                    : Util.Parse(trimmed, region);

                if (!IsUkNumber(candidate))
                {
                    return false;
                }

                parsed = candidate;
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
