using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Marks Privacy &amp; consent as reviewed for Locations setup readiness.
    /// Visiting Privacy &amp; consent (Manage) completes the review — consent
    /// wording is not required.
    /// </summary>
    public sealed class PrivacyConsentSaveService : IPrivacyConsentSaveService
    {
        private readonly ApplicationDbContext _context;

        public PrivacyConsentSaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PrivacyConsentSaveResult> SaveAsync(
            int restaurantId,
            int actorUserId
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return new PrivacyConsentSaveResult.NotFound();
            }

            if (restaurant.PrivacyConsentReadyAt != null)
            {
                return new PrivacyConsentSaveResult.Ok(true);
            }

            var actorDisplayName = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();
            var actorLabel = string.IsNullOrWhiteSpace(actorDisplayName)
                ? "An operator"
                : actorDisplayName.Trim();

            var now = DateTime.UtcNow;
            restaurant.PrivacyConsentReadyAt = now;

            LocationActivityAppend.AppendRestaurantActivity(
                _context,
                restaurantId,
                actorUserId,
                actorLabel,
                LocationActivityKinds.PrivacyReviewCompleted,
                $"{actorLabel} completed the privacy review.",
                now
            );

            await _context.SaveChangesAsync();

            return new PrivacyConsentSaveResult.Ok(true);
        }
    }
}
