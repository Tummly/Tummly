using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Exceptions;
using TummlyBackend.Helpers;
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
        private readonly IBillingAccountLifecycle _lifecycle;

        public SmartGuestLinkService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IBillingAccountLifecycle lifecycle
        )
        {
            _context = context;
            _configuration = configuration;
            _lifecycle = lifecycle;
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

        public async Task<GuestQrResolveResult> ResolveForGuestAsync(string token)
        {
            var normalizedToken = NormalizeToken(token);

            if (normalizedToken == null)
            {
                return new GuestQrResolveResult.NotFound();
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

            if (qrCode == null || location == null || location.Restaurant == null)
            {
                return new GuestQrResolveResult.NotFound();
            }

            var restaurant = location.Restaurant;
            var access = await EvaluateGuestAccessAfterTickAsync(restaurant);

            if (access == GuestQrAccessKind.Dormant)
            {
                return new GuestQrResolveResult.Dormant(
                    restaurant.Name ?? "",
                    BuildBrandLogoPublicUrl(restaurant.BrandLogoObjectKey)
                );
            }

            if (access == GuestQrAccessKind.Denied)
            {
                return new GuestQrResolveResult.NotFound();
            }

            return new GuestQrResolveResult.Live(
                new GuestLinkLocationInfo
                {
                    LocationId = location.Id,
                    RestaurantName = restaurant.Name ?? "",
                    LocationName = location.LocationName,
                    Address = location.Address ?? "",
                    BrandLogoPublicUrl = BuildBrandLogoPublicUrl(
                        restaurant.BrandLogoObjectKey
                    ),
                    QrCodeId = qrCode.Id,
                    QrType = qrCode.QrType
                }
            );
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
                    .ThenInclude(l => l!.Restaurant)
                .FirstOrDefaultAsync(q =>
                    q.Token == normalizedToken
                    && q.Status == QrCodeStatus.Active
                );

            if (qrCode?.RestaurantLocation?.Restaurant == null)
            {
                return null;
            }

            var restaurant = qrCode.RestaurantLocation.Restaurant;
            var access = await EvaluateGuestAccessAfterTickAsync(restaurant);
            if (access != GuestQrAccessKind.Allowed)
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

        private async Task<GuestQrAccessKind> EvaluateGuestAccessAfterTickAsync(
            Restaurant restaurant
        )
        {
            await _lifecycle.TickAsync(restaurant.Id, DateTime.UtcNow);

            var billingStatus = await LoadBillingStatusAsync(restaurant.Id);
            if (billingStatus == BillingStatuses.Dormant)
            {
                return GuestQrAccessKind.Dormant;
            }

            if (restaurant.WorkspaceStatus == WorkspaceStatus.Paused)
            {
                return GuestQrAccessKind.Denied;
            }

            return GuestQrAccessKind.Allowed;
        }

        private async Task<string?> LoadBillingStatusAsync(int restaurantId)
        {
            return await _context.BillingAccounts
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.BillingStatus)
                .FirstOrDefaultAsync();
        }

        private static string? BuildBrandLogoPublicUrl(string? brandLogoObjectKey)
        {
            return string.IsNullOrWhiteSpace(brandLogoObjectKey)
                ? null
                : BrandLogoRules.BuildPublicUrl(brandLogoObjectKey);
        }

        private enum GuestQrAccessKind
        {
            Allowed,
            Denied,
            Dormant,
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
