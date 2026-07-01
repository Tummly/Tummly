using System.Security.Cryptography;
using TummlyBackend.Models;

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

        public static bool IsPendingActivation(User user)
        {
            return !string.IsNullOrEmpty(user.ActivationCodeHash)
                && user.ActivatedAt == null;
        }

        public static bool RequiresActivation(User user)
        {
            return user.ActivatedAt == null;
        }

        public static bool IsWithinActivationPeriod(User user)
        {
            return user.ActivatedAt != null
                && user.ActivationExpiresAt.HasValue
                && user.ActivationExpiresAt.Value > DateTime.UtcNow;
        }

        public static bool IsActivationExpired(User user)
        {
            return user.ActivatedAt != null
                && user.ActivationExpiresAt.HasValue
                && user.ActivationExpiresAt.Value <= DateTime.UtcNow;
        }

        public static bool IsOperatorApiAccessBlocked(User user)
        {
            return RequiresActivation(user) || IsActivationExpired(user);
        }

        public const string ActivationExpiredMessage =
            "Your 30 day free trial is over";

        public static string? GetActivationBadgeStatus(User? user)
        {
            if (user == null || !HasActivationState(user))
            {
                return null;
            }

            return IsWithinActivationPeriod(user)
                ? "activated"
                : "not_activated";
        }

        public static string? GetActivationStatusDetail(User? user)
        {
            if (user == null || !HasActivationState(user))
            {
                return null;
            }

            if (IsPendingActivation(user))
            {
                return "pending";
            }

            if (IsActivationExpired(user))
            {
                return "expired";
            }

            if (IsWithinActivationPeriod(user))
            {
                return "active";
            }

            return null;
        }

        public static string FormatCodeForDisplay(string normalizedCode)
        {
            if (normalizedCode.Length != CodeLength)
            {
                return normalizedCode;
            }

            return $"{normalizedCode[..4]}-{normalizedCode[4..]}";
        }

        public static bool HasActivationState(User user)
        {
            return !string.IsNullOrEmpty(user.ActivationCodeHash)
                || user.ActivatedAt != null;
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
