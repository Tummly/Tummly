using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.PrivacyConsent;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PrivacyConsentSaveService : IPrivacyConsentSaveService
    {
        private const int MaxWordingLength = 2000;

        private readonly ApplicationDbContext _context;

        public PrivacyConsentSaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PrivacyConsentSaveResult> SaveAsync(
            int restaurantId,
            int actorUserId,
            SavePrivacyConsentRequest request
        )
        {
            var sms = NormalizeWording(request.SmsConsentWording);
            var email = NormalizeWording(request.EmailConsentWording);

            if (sms.Length > MaxWordingLength)
            {
                return new PrivacyConsentSaveResult.InvalidRequest(
                    $"SMS consent wording must be at most {MaxWordingLength} characters."
                );
            }

            if (email.Length > MaxWordingLength)
            {
                return new PrivacyConsentSaveResult.InvalidRequest(
                    $"Email consent wording must be at most {MaxWordingLength} characters."
                );
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return new PrivacyConsentSaveResult.NotFound();
            }

            var actorDisplayName = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();
            var actorLabel = string.IsNullOrWhiteSpace(actorDisplayName)
                ? "An operator"
                : actorDisplayName.Trim();

            var wordingChanged =
                !string.Equals(
                    restaurant.SmsConsentWording ?? string.Empty,
                    sms,
                    StringComparison.Ordinal
                )
                || !string.Equals(
                    restaurant.EmailConsentWording ?? string.Empty,
                    email,
                    StringComparison.Ordinal
                );

            restaurant.SmsConsentWording = sms.Length == 0 ? null : sms;
            restaurant.EmailConsentWording = email.Length == 0 ? null : email;

            var becameReady = restaurant.PrivacyConsentReadyAt == null;
            if (becameReady)
            {
                restaurant.PrivacyConsentReadyAt = DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;

            if (wordingChanged)
            {
                _context.LocationActivities.Add(
                    new LocationActivity
                    {
                        RestaurantId = restaurantId,
                        LocationId = null,
                        ActorUserId = actorUserId,
                        ActorDisplayName = actorLabel,
                        Kind = LocationActivityKinds.ConsentCopyChanged,
                        Description =
                            $"{actorLabel} updated SMS/email consent wording.",
                        OccurredAt = now,
                    }
                );
            }

            if (becameReady)
            {
                _context.LocationActivities.Add(
                    new LocationActivity
                    {
                        RestaurantId = restaurantId,
                        LocationId = null,
                        ActorUserId = actorUserId,
                        ActorDisplayName = actorLabel,
                        Kind = LocationActivityKinds.PrivacyReviewCompleted,
                        Description =
                            $"{actorLabel} completed the privacy review.",
                        OccurredAt = now,
                    }
                );
            }

            await _context.SaveChangesAsync();

            return new PrivacyConsentSaveResult.Ok(
                restaurant.PrivacyConsentReadyAt != null
            );
        }

        private static string NormalizeWording(string? raw) =>
            (raw ?? string.Empty).Trim();
    }
}
