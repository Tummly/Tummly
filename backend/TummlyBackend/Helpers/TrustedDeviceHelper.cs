using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class TrustedDeviceHelper
    {
        public const int TrustDurationDays = 30;

        public static string GenerateDeviceToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );
        }

        public static string HashDeviceToken(string deviceToken)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(deviceToken)
            );

            return Convert.ToHexString(bytes);
        }

        public static async Task<bool> IsTrustedAsync(
            ApplicationDbContext context,
            int userId,
            string? deviceToken
        )
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                return false;
            }

            var tokenHash = HashDeviceToken(deviceToken.Trim());

            return await context.TrustedDevices
                .AsNoTracking()
                .AnyAsync(t =>
                    t.UserId == userId &&
                    t.TokenHash == tokenHash &&
                    t.ExpiresAt > DateTime.UtcNow
                );
        }

        public static async Task<string?> IssueTrustedDeviceAsync(
            ApplicationDbContext context,
            int userId
        )
        {
            var deviceToken = GenerateDeviceToken();
            var tokenHash = HashDeviceToken(deviceToken);
            var now = DateTime.UtcNow;

            var trustedDevice = new TrustedDevice
            {
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(TrustDurationDays),
            };

            await context.TrustedDevices.AddAsync(trustedDevice);

            return deviceToken;
        }
    }
}
