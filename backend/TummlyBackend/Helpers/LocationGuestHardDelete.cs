using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stages hard-delete for one Location Guest (no SaveChanges).
    /// Writes suppressions for withdrawn kinds; unlinks Claims / Campaign rows;
    /// anonymises Feedback; removes notes/tags/activity/ledger; removes guest;
    /// removes orphan Master Guest.
    /// </summary>
    internal static class LocationGuestHardDelete
    {
        public static async Task<LocationGuestHardDeleteCounts> ApplyAsync(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            LocationGuest locationGuest,
            string suppressionSource,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(suppressions);
            ArgumentNullException.ThrowIfNull(locationGuest);
            ArgumentException.ThrowIfNullOrEmpty(suppressionSource);

            var locationGuestId = locationGuest.Id;
            var masterGuestId = locationGuest.MasterGuestId;
            var now = GuestsDateWindows.EnsureUtc(nowUtc);

            var restaurantId = await ResolveRestaurantIdAsync(
                context,
                locationGuest,
                cancellationToken
            );

            var master = await context.MasterGuests
                .FirstOrDefaultAsync(
                    m => m.Id == masterGuestId,
                    cancellationToken
                );

            // Contact key before MasterGuest may be removed (last LocationGuest).
            var contactKey = master is null
                ? null
                : GuestContactKey.FromMasterGuest(master);

            await StageWithdrawnSuppressionsAsync(
                context,
                suppressions,
                locationGuest,
                restaurantId,
                contactKey,
                suppressionSource,
                now,
                cancellationToken
            );

            var claims = await context.OfferIssues
                .Where(o => o.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            foreach (var claim in claims)
            {
                claim.LocationGuestId = null;
            }

            var frozenRecipients = await context.CampaignFrozenRecipients
                .Where(r => r.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            foreach (var frozen in frozenRecipients)
            {
                frozen.LocationGuestId = null;
            }

            var deliveries = await context.CampaignRecipientDeliveries
                .Where(d => d.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            foreach (var delivery in deliveries)
            {
                delivery.LocationGuestId = null;
            }

            // Feedback FK is NoAction — unlink + anonymise before remove.
            var feedbacks = await context.Feedbacks
                .Where(f => f.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            foreach (var feedback in feedbacks)
            {
                feedback.LocationGuestId = null;
                feedback.GuestName = DeletedGuestDisplay.Name;
                feedback.GuestContact = string.Empty;
                feedback.ContactType = ContactType.Unknown;
            }

            // Explicit removals: activity/tag FKs are NoAction (SQL Server
            // cascade-path limits); notes cascade in SQL. InMemory does not
            // enforce cascade — application policy deletes all.
            var activityEvents = await context.LocationGuestActivityEvents
                .Where(e => e.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var permissionLedgerEntries =
                await context.LocationGuestPermissionLedgerEntries
                    .Where(e => e.LocationGuestId == locationGuestId)
                    .ToListAsync(cancellationToken);
            var notes = await context.LocationGuestNotes
                .Where(n => n.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);
            var tagMemberships = await context.LocationGuestTags
                .Where(m => m.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            context.LocationGuestActivityEvents.RemoveRange(activityEvents);
            context.LocationGuestPermissionLedgerEntries.RemoveRange(
                permissionLedgerEntries
            );
            context.LocationGuestNotes.RemoveRange(notes);
            context.LocationGuestTags.RemoveRange(tagMemberships);
            context.LocationGuests.Remove(locationGuest);

            var otherLocationGuestsRemain = await context.LocationGuests
                .AnyAsync(
                    lg =>
                        lg.MasterGuestId == masterGuestId
                        && lg.Id != locationGuestId,
                    cancellationToken
                );

            if (!otherLocationGuestsRemain && master != null)
            {
                context.MasterGuests.Remove(master);
            }

            return new LocationGuestHardDeleteCounts(
                Feedbacks: feedbacks.Count,
                Claims: claims.Count,
                FrozenRecipients: frozenRecipients.Count,
                Deliveries: deliveries.Count
            );
        }

        private static async Task StageWithdrawnSuppressionsAsync(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            LocationGuest locationGuest,
            int restaurantId,
            string? contactKey,
            string suppressionSource,
            DateTime nowUtc,
            CancellationToken cancellationToken
        )
        {
            if (contactKey is null)
            {
                return;
            }

            var ledgerService = new LocationGuestPermissionLedgerService(context);
            var ledgerStates = await ledgerService.GetCurrentStatesAsync(
                locationGuest.Id,
                cancellationToken
            );
            var effective = LocationGuestChannelPermissionGate.ResolveEffectiveStates(
                locationGuest.MarketingPreference,
                ledgerStates
            );

            var ledgerAllNotRecorded = ledgerStates.Values.All(
                state => state == LocationGuestPermissionState.NotRecorded
            );
            var useDeleteTimeForWithdraw =
                ledgerAllNotRecorded
                && locationGuest.MarketingPreference
                    != LocationGuestMarketingPreference.NotRecorded;

            Dictionary<LocationGuestPermissionKind, DateTime>? latestWithdrawByKind =
                null;
            if (!useDeleteTimeForWithdraw)
            {
                latestWithdrawByKind = await context
                    .LocationGuestPermissionLedgerEntries
                    .AsNoTracking()
                    .Where(e =>
                        e.LocationGuestId == locationGuest.Id
                        && e.EventKind
                            == LocationGuestPermissionLedgerEventKinds.Withdraw
                    )
                    .GroupBy(e => e.PermissionKind)
                    .Select(g => new
                    {
                        Kind = g.Key,
                        OccurredAt = g.Max(e => e.OccurredAt),
                    })
                    .ToDictionaryAsync(
                        row => row.Kind,
                        row => GuestsDateWindows.EnsureUtc(row.OccurredAt),
                        cancellationToken
                    );
            }

            foreach (var kind in LocationGuestPermissionKindExtensions.All)
            {
                if (
                    !effective.TryGetValue(kind, out var state)
                    || state != LocationGuestPermissionState.Withdrawn
                )
                {
                    continue;
                }

                DateTime withdrawnAt;
                if (useDeleteTimeForWithdraw)
                {
                    withdrawnAt = nowUtc;
                }
                else if (
                    latestWithdrawByKind is not null
                    && latestWithdrawByKind.TryGetValue(kind, out var fromLedger)
                )
                {
                    withdrawnAt = fromLedger;
                }
                else
                {
                    withdrawnAt = nowUtc;
                }

                await suppressions.StageAsync(
                    restaurantId,
                    contactKey,
                    kind,
                    withdrawnAt,
                    suppressionSource,
                    cancellationToken
                );
            }
        }

        private static async Task<int> ResolveRestaurantIdAsync(
            ApplicationDbContext context,
            LocationGuest locationGuest,
            CancellationToken cancellationToken
        )
        {
            if (locationGuest.RestaurantLocation?.RestaurantId is int fromNav
                && fromNav != 0)
            {
                return fromNav;
            }

            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationGuest.RestaurantLocationId)
                .Select(l => l.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);

            if (restaurantId == 0)
            {
                throw new InvalidOperationException(
                    "Restaurant location not found for Location Guest hard-delete."
                );
            }

            return restaurantId;
        }
    }

    public sealed record LocationGuestHardDeleteCounts(
        int Feedbacks,
        int Claims,
        int FrozenRecipients,
        int Deliveries
    );
}
