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

                var existsInDatabase = await _context.QrCodes
                    .AnyAsync(q => q.Token == token);

                var existsInChangeTracker = _context.QrCodes.Local
                    .Any(q => q.Token == token);

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

            var qrCode = await _context.QrCodes
                .AsNoTracking()
                .Include(q => q.RestaurantLocation)
                    .ThenInclude(l => l!.Restaurant)
                .FirstOrDefaultAsync(q =>
                    q.Token == normalizedToken
                    && q.Status == QrCodeStatus.Active
                );

            var location = qrCode?.RestaurantLocation;

            if (qrCode == null || location == null)
            {
                return null;
            }

            return new GuestLinkLocationInfo
            {
                LocationId = location.Id,
                RestaurantName = location.Restaurant?.Name ?? "",
                LocationName = location.LocationName,
                Address = location.Address ?? "",
                QrCodeId = qrCode.Id,
                QrType = qrCode.QrType
            };
        }

        public async Task<QrLinkWriteResolution?> ResolveLocationForWriteAsync(
            string token
        )
        {
            var normalizedToken = NormalizeToken(token);

            if (normalizedToken == null)
            {
                return null;
            }

            var qrCode = await _context.QrCodes
                .Include(q => q.RestaurantLocation)
                .FirstOrDefaultAsync(q =>
                    q.Token == normalizedToken
                    && q.Status == QrCodeStatus.Active
                );

            if (qrCode?.RestaurantLocation == null)
            {
                return null;
            }

            return new QrLinkWriteResolution
            {
                Location = qrCode.RestaurantLocation,
                QrCodeId = qrCode.Id,
                QrType = qrCode.QrType
            };
        }

        public string BuildGuestUrl(string token)
        {
            var frontendBaseUrl = GetFrontendBaseUrl();

            return $"{frontendBaseUrl}/scan/{token}";
        }

        public async Task<string?> GetActiveSmartGuestTokenAsync(
            int restaurantLocationId
        )
        {
            return await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == restaurantLocationId
                    && q.QrType == QrType.SmartGuest
                    && q.Status == QrCodeStatus.Active
                )
                .Select(q => q.Token)
                .FirstOrDefaultAsync();
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
