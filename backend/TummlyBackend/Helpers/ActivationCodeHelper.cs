using System.Security.Cryptography;

namespace TummlyBackend.Helpers
{
    public static class ActivationCodeHelper
    {
        public const string Charset = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

        public const int CodeLength = 8;

        public const int ActivationPeriodDays = 30;

        public static string GeneratePlainCode()
        {
            var bytes = new byte[CodeLength];
            RandomNumberGenerator.Fill(bytes);

            var chars = new char[CodeLength];

            for (var i = 0; i < CodeLength; i++)
            {
                chars[i] = Charset[bytes[i] % Charset.Length];
            }

            return new string(chars);
        }

        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return input.Trim().Replace("-", "", StringComparison.Ordinal)
                .ToUpperInvariant();
        }

        public static bool IsValidFormat(string normalizedCode)
        {
            if (normalizedCode.Length != CodeLength)
            {
                return false;
            }

            foreach (var character in normalizedCode)
            {
                if (!Charset.Contains(character))
                {
                    return false;
                }
            }

            return true;
        }

        public static string HashCode(string normalizedCode)
        {
            return BCrypt.Net.BCrypt.HashPassword(normalizedCode);
        }

        public static bool VerifyCode(
            string normalizedCode,
            string activationCodeHash
        )
        {
            return BCrypt.Net.BCrypt.Verify(
                normalizedCode,
                activationCodeHash
            );
        }

        public static string FormatCodeForDisplay(string normalizedCode)
        {
            if (normalizedCode.Length != CodeLength)
            {
                return normalizedCode;
            }

            return $"{normalizedCode[..4]}-{normalizedCode[4..]}";
        }

        public static DateTime ComputeActivationExpiresAt(
            DateTime activatedAt
        )
        {
            return activatedAt.AddDays(ActivationPeriodDays);
        }

        public static DateTime ComputeDefaultExtensionExpiresAt()
        {
            return DateTime.UtcNow.AddDays(ActivationPeriodDays);
        }
    }
}
