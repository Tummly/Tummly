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

        private static readonly List<string> PrivacyActivityKinds =
        [
            LocationActivityKinds.ConsentCopyChanged,
            LocationActivityKinds.PrivacyReviewCompleted,
            LocationActivityKinds.GuestMarketingUnsubscribed,
            LocationActivityKinds.GuestPermissionToggleChanged,
        ];

        private readonly ApplicationDbContext _context;

        public PrivacyConsentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsWorkspaceOwnerAsync(int restaurantId, int userId)
        {
            var membership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            if (membership != null)
            {
                return membership.PermissionRole == PermissionRoles.Owner;
            }

            return await _context.Restaurants
                .AsNoTracking()
                .AnyAsync(row =>
                    row.Id == restaurantId && row.OwnerUserId == userId
                );
        }

        public async Task<PrivacyConsentGetResult> GetAsync(
            int restaurantId,
            bool actorCanManage,
            bool canViewGuests,
            bool actorIsWorkspaceOwner
        )
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
                    privacyReady = restaurant.PrivacyConsentReadyAt != null,
                    privacyConsentReadyAt =
                        restaurant.PrivacyConsentReadyAt?.ToUniversalTime()
                            .ToString("O"),
                    actorCanManage,
                    canViewGuests,
                    actorIsWorkspaceOwner,
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

        public async Task<object> GetActivityAsync(
            int restaurantId,
            IReadOnlyCollection<int> accessibleLocationIds
        )
        {
            var liveLocationIds = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.LifecycleStatus != LocationLifecycleStatus.Archived
                )
                .Select(row => row.Id)
                .ToListAsync();
            var visibleLocationIds = accessibleLocationIds
                .Intersect(liveLocationIds)
                .ToList();

            var items = await _context.LocationActivities
                .AsNoTracking()
                .Where(a =>
                    a.RestaurantId == restaurantId
                    && (
                        a.LocationId == null
                        || visibleLocationIds.Contains(a.LocationId.Value)
                    )
                    && PrivacyActivityKinds.Contains(a.Kind)
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

        public async Task<object> GetArchivedActivityAsync(int restaurantId)
        {
            var archivedLocationNames = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.LifecycleStatus == LocationLifecycleStatus.Archived
                )
                .ToDictionaryAsync(row => row.Id, row => row.LocationName);
            if (archivedLocationNames.Count == 0)
            {
                return new
                {
                    success = true,
                    items = Array.Empty<object>(),
                };
            }

            var archivedLocationIds = archivedLocationNames.Keys.ToList();
            var rows = await _context.LocationActivities
                .AsNoTracking()
                .Where(a =>
                    a.RestaurantId == restaurantId
                    && a.LocationId != null
                    && archivedLocationIds.Contains(a.LocationId.Value)
                    && PrivacyActivityKinds.Contains(a.Kind)
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

            var items = rows
                .Select(row => new
                {
                    row.id,
                    row.locationId,
                    locationName = archivedLocationNames[row.locationId!.Value],
                    locationArchived = true,
                    row.kind,
                    row.description,
                    row.occurredAt,
                })
                .ToList();

            return new
            {
                success = true,
                items,
            };
        }
    }
}
