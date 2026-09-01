using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.PrivacyConsent;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PrivacyConsentService : IPrivacyConsentService
    {
        public const int MaxActivityItems = 50;

        private readonly ApplicationDbContext _context;

        public PrivacyConsentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PrivacyConsentGetResult> GetAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return new PrivacyConsentGetResult.NotFound();
            }

            var setupRows = PrivacyConsentSetupDerivation.BuildSetupRows(
                restaurant
            );

            return new PrivacyConsentGetResult.Ok(
                new
                {
                    success = true,
                    privacySetupRows = setupRows.Select(row => new
                    {
                        id = row.Id,
                        requirement = row.Requirement,
                        status = row.Status,
                    }),
                    emailMarketingPermissionEnabled =
                        restaurant.EmailMarketingPermissionEnabled,
                    smsMarketingPermissionEnabled =
                        restaurant.SmsMarketingPermissionEnabled,
                    feedbackFollowUpPermissionEnabled =
                        restaurant.FeedbackFollowUpPermissionEnabled,
                    smsConsentWording = restaurant.SmsConsentWording ?? string.Empty,
                    emailConsentWording =
                        restaurant.EmailConsentWording ?? string.Empty,
                    privacyReady = restaurant.PrivacyConsentReadyAt != null,
                    privacyConsentReadyAt =
                        restaurant.PrivacyConsentReadyAt?.ToUniversalTime()
                            .ToString("O"),
                }
            );
        }

        public async Task<PrivacyConsentPatchResult> PatchTogglesAsync(
            int restaurantId,
            int actorUserId,
            PatchPrivacyConsentTogglesRequest request
        )
        {
            if (
                request.EmailMarketingPermissionEnabled == null
                && request.SmsMarketingPermissionEnabled == null
                && request.FeedbackFollowUpPermissionEnabled == null
            )
            {
                return new PrivacyConsentPatchResult.InvalidRequest(
                    "At least one guest permission toggle is required."
                );
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return new PrivacyConsentPatchResult.NotFound();
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
            var changes = new List<(string Label, bool Enabled)>();

            if (
                request.EmailMarketingPermissionEnabled is bool emailEnabled
                && restaurant.EmailMarketingPermissionEnabled != emailEnabled
            )
            {
                restaurant.EmailMarketingPermissionEnabled = emailEnabled;
                changes.Add(("email marketing", emailEnabled));
            }

            if (
                request.SmsMarketingPermissionEnabled is bool smsEnabled
                && restaurant.SmsMarketingPermissionEnabled != smsEnabled
            )
            {
                restaurant.SmsMarketingPermissionEnabled = smsEnabled;
                changes.Add(("SMS marketing", smsEnabled));
            }

            if (
                request.FeedbackFollowUpPermissionEnabled
                    is bool feedbackEnabled
                && restaurant.FeedbackFollowUpPermissionEnabled
                    != feedbackEnabled
            )
            {
                restaurant.FeedbackFollowUpPermissionEnabled = feedbackEnabled;
                changes.Add(("feedback follow-up", feedbackEnabled));
            }

            foreach (var (label, enabled) in changes)
            {
                var verb = enabled ? "enabled" : "disabled";
                LocationActivityAppend.AppendRestaurantActivity(
                    _context,
                    restaurantId,
                    actorUserId,
                    actorLabel,
                    LocationActivityKinds.GuestPermissionToggleChanged,
                    $"{actorLabel} {verb} {label} for guests.",
                    now
                );
            }

            if (changes.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return new PrivacyConsentPatchResult.Ok();
        }

        public async Task<object> GetActivityAsync(int restaurantId)
        {
            var items = await _context.LocationActivities
                .AsNoTracking()
                .Where(a =>
                    a.RestaurantId == restaurantId
                    && (
                        a.Kind == LocationActivityKinds.ConsentCopyChanged
                        || a.Kind
                            == LocationActivityKinds.PrivacyReviewCompleted
                        || a.Kind
                            == LocationActivityKinds.GuestMarketingUnsubscribed
                        || a.Kind
                            == LocationActivityKinds.GuestPermissionToggleChanged
                    )
                )
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
                .Take(MaxActivityItems)
                .Select(a => new
                {
                    id = a.Id,
                    locationId = a.LocationId,
                    kind = a.Kind,
                    description = a.Description,
                    occurredAt = a.OccurredAt.ToUniversalTime().ToString("O"),
                })
                .ToListAsync();

            return new
            {
                success = true,
                items,
            };
        }
    }
}
