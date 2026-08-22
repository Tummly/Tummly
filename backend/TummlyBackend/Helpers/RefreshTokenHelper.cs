using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class RefreshTokenHelper
    {
        public const int ExpiryDays = 14;

        public static string GeneratePlainToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );
        }

        public static string HashToken(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token.Trim())
            );

            return Convert.ToHexString(bytes);
        }

        public static async Task<string> IssueAsync(
            ApplicationDbContext context,
            int userId
        )
        {
            var plainToken = GeneratePlainToken();

            await context.RefreshTokens.AddAsync(
                new RefreshToken
                {
                    UserId = userId,
                    Token = HashToken(plainToken),
                    ExpiryDate = DateTime.UtcNow.AddDays(ExpiryDays),
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow,
                }
            );

            return plainToken;
        }

        public static async Task<RefreshToken?> FindValidAsync(
            ApplicationDbContext context,
            string? plainToken
        )
        {
            if (string.IsNullOrWhiteSpace(plainToken))
            {
                return null;
            }

            var tokenHash = HashToken(plainToken);

            return await context.RefreshTokens
                .Include(row => row.User)
                .FirstOrDefaultAsync(row =>
                    row.Token == tokenHash &&
                    row.IsRevoked == false &&
                    row.ExpiryDate > DateTime.UtcNow
                );
        }
    }
}
