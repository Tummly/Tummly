using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Exceptions;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SmartGuestLinkService : ISmartGuestLinkService
    {
        private const int TokenLength = 32;
        private const int MaxGenerationAttempts = 5;

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SmartGuestLinkService(
            ApplicationDbContext context,
            IConfiguration configuration
        )
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GenerateTokenAsync()
        {
            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                var token = CreateRandomToken();

                var existsInDatabase = await _context.RestaurantLocations
                    .AnyAsync(l => l.LinkToken == token);

                var existsInChangeTracker = _context.RestaurantLocations.Local
                    .Any(l => l.LinkToken == token);

                if (!existsInDatabase && !existsInChangeTracker)
                {
                    return token;
                }
            }

            throw new LinkTokenGenerationException();
        }

        public async Task<GuestLinkLocationInfo?> ResolveForGuestAsync(string token)
        {
            var normalizedToken = NormalizeToken(token);

            if (normalizedToken == null)
            {
                return null;
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(l => l.Restaurant)
                .FirstOrDefaultAsync(l => l.LinkToken == normalizedToken);

            if (location == null)
            {
                return null;
            }

            return new GuestLinkLocationInfo
            {
                RestaurantName = location.Restaurant?.Name ?? "",
                LocationName = location.LocationName
            };
        }

        public async Task<RestaurantLocation?> ResolveLocationForWriteAsync(
            string token
        )
        {
            var normalizedToken = NormalizeToken(token);

            if (normalizedToken == null)
            {
                return null;
            }

            return await _context.RestaurantLocations
                .FirstOrDefaultAsync(l => l.LinkToken == normalizedToken);
        }

        public string BuildGuestUrl(string linkToken)
        {
            var frontendBaseUrl = GetFrontendBaseUrl();

            return $"{frontendBaseUrl}/scan/{linkToken}";
        }

        private static string? NormalizeToken(string? token)
        {
            var normalizedToken = token?.Trim();

            return string.IsNullOrWhiteSpace(normalizedToken)
                ? null
                : normalizedToken;
        }

        protected virtual string CreateRandomToken()
        {
            const string chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var bytes = new byte[TokenLength];

            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(bytes);

            var result = new char[TokenLength];

            for (var i = 0; i < TokenLength; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }

            return new string(result);
        }

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]
                    ?.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            if (
                !Uri.TryCreate(
                    frontendBaseUrl,
                    UriKind.Absolute,
                    out var uri
                ) ||
                (
                    uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeHttp
                )
            )
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl must be an absolute http(s) URL."
                );
            }

            return frontendBaseUrl;
        }
    }
}
