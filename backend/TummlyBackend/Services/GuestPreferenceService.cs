using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.GuestPreferences;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Guest preference centre read model and consent write rules (spec §8).
    /// Restaurant-side writes go through the permission ledger in the same unit
    /// of work as the Tummly stream write. Never creates restaurant consent the
    /// guest did not give: restores only after a guest-made withdraw.
    /// </summary>
    public class GuestPreferenceService : IGuestPreferenceService
    {
        /// <summary>
        /// Wording version stamped on restaurant grant rows written by a
        /// preference-centre tick (the checkbox text in <see cref="ChannelSpec"/>).
        /// </summary>
        public const string WordingVersion = "preference-centre-v1";

        private const int FirstNameMaxLength = 100;
        private const string ActivityActor = "Guest";
        private const string ActivityDescription =
            "Guest withdrew marketing permission at this location.";

        private sealed record ChannelSpec(
            string Channel,
            LocationGuestPermissionKind Kind,
            string WordingSnapshot
        );

        private static readonly ChannelSpec EmailChannel = new(
            TummlyGuestChannels.Email,
            LocationGuestPermissionKind.EmailMarketing,
            "Email marketing — Receive dining news and offers by Email."
        );

        private static readonly ChannelSpec SmsChannel = new(
            TummlyGuestChannels.Sms,
            LocationGuestPermissionKind.SmsMarketing,
            "SMS marketing — Receive dining news and offers by text."
        );

        private static readonly ChannelSpec[] Channels = [EmailChannel, SmsChannel];

        /// <summary>
        /// State read once per call, before any change is staged.
        /// </summary>
        private sealed record GuestSnapshot(
            TummlyGuest Guest,
            IReadOnlyList<LocationGuest> Matched,
            HashSet<int> MutedLocationIds,
            IReadOnlyDictionary<
                int,
                IReadOnlyDictionary<LocationGuestPermissionKind, LocationGuestPermissionState>
            > States,
            Dictionary<string, string> LatestTummlyEventKinds
        );

        private readonly ApplicationDbContext _context;
        private readonly ITummlyGuestIdentityService _identity;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IConfiguration _configuration;

        public GuestPreferenceService(
            ApplicationDbContext context,
            ITummlyGuestIdentityService identity,
            ILocationGuestPermissionLedgerService permissions,
            IConfiguration configuration
        )
        {
            _context = context;
            _identity = identity;
            _permissions = permissions;
            _configuration = configuration;
        }

        public async Task<GuestPreferencesOverviewDto> GetOverviewAsync(
            int tummlyGuestId,
            CancellationToken ct
        )
        {
            var snapshot = await LoadSnapshotAsync(tummlyGuestId, ct);

            var contacts = snapshot
                .Guest.Contacts.OrderBy(c => c.Id)
                .Select(c => new GuestPreferenceContactDto(
                    c.Id,
                    c.ContactType,
                    GuestPreferenceContact.Mask(c.ContactType, c.NormalizedValue)
                ))
                .ToList();

            // Restaurants where at least one location is still on.
            var restaurantCount = snapshot
                .Matched.Where(lg => !snapshot.MutedLocationIds.Contains(lg.RestaurantLocationId))
                .Select(lg => lg.RestaurantLocation!.RestaurantId)
                .Distinct()
                .Count();

            return new GuestPreferencesOverviewDto(
                snapshot.Guest.FirstName,
                contacts,
                IsChannelOn(snapshot, EmailChannel),
                IsChannelOn(snapshot, SmsChannel),
                restaurantCount
            );
        }

        public async Task SetFirstNameAsync(
            int tummlyGuestId,
            string firstName,
            CancellationToken ct
        )
        {
            var trimmed = (firstName ?? string.Empty).Trim();
            if (trimmed.Length is 0 or > FirstNameMaxLength)
            {
                throw new ArgumentException(
                    $"First name must be 1 to {FirstNameMaxLength} characters.",
                    nameof(firstName)
                );
            }

            var guest = await _context.TummlyGuests.FirstOrDefaultAsync(
                g => g.Id == tummlyGuestId,
                ct
            ) ?? throw new KeyNotFoundException("Tummly Guest not found.");

            // First name lives on the Tummly Guest only (A1). Location Guest names are unchanged.
            guest.FirstName = trimmed;
            guest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        public async Task SetChannelsAsync(
            int tummlyGuestId,
            GuestPreferenceChannelsRequest req,
            CancellationToken ct
        )
        {
            ArgumentNullException.ThrowIfNull(req);

            var snapshot = await LoadSnapshotAsync(tummlyGuestId, ct);
            if (req.Email is null && req.Sms is null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (req.Email is { } email)
            {
                await ApplyChannelAsync(snapshot, EmailChannel, email, now, ct);
            }

            if (req.Sms is { } sms)
            {
                await ApplyChannelAsync(snapshot, SmsChannel, sms, now, ct);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task StopAllAsync(int tummlyGuestId, CancellationToken ct) =>
            await SetChannelsAsync(
                tummlyGuestId,
                new GuestPreferenceChannelsRequest(Email: false, Sms: false),
                ct
            );

        public async Task<IReadOnlyList<GuestPreferenceRestaurantDto>> ListRestaurantsAsync(
            int tummlyGuestId,
            string? q,
            CancellationToken ct
        )
        {
            await EnsureGuestExistsAsync(tummlyGuestId, ct);

            var matched = await _identity.FindLocationGuestsAsync(tummlyGuestId, ct);
            var muted = await LoadMutedLocationIdsAsync(tummlyGuestId, ct);
            var term = q?.Trim();

            var locations = matched
                .Select(lg => lg.RestaurantLocation!)
                .DistinctBy(l => l.Id)
                .Where(l =>
                    string.IsNullOrEmpty(term)
                    || ContainsIgnoreCase(l.Restaurant!.Name, term)
                    || ContainsIgnoreCase(l.LocationName, term)
                )
                .ToList();

            return locations
                .GroupBy(l => l.RestaurantId)
                .Select(g =>
                {
                    var restaurant = g.First().Restaurant!;
                    var items = g.OrderBy(l => l.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(l => l.Id)
                        .Select(l => new GuestPreferenceLocationDto(
                            l.Id,
                            l.LocationName,
                            muted.Contains(l.Id)
                        ))
                        .ToList();

                    return new GuestPreferenceRestaurantDto(
                        restaurant.Id,
                        restaurant.Name,
                        BrandLogoRules.BuildAbsolutePublicUrl(
                            restaurant.BrandLogoObjectKey,
                            _configuration
                        ),
                        items
                    );
                })
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.RestaurantId)
                .ToList();
        }

        public async Task SetLocationMuteAsync(
            int tummlyGuestId,
            int locationId,
            bool muted,
            CancellationToken ct
        )
        {
            var snapshot = await LoadSnapshotAsync(tummlyGuestId, ct);
            if (!snapshot.Matched.Any(lg => lg.RestaurantLocationId == locationId))
            {
                throw new KeyNotFoundException(
                    "Location is not on this guest's preference list."
                );
            }

            await ApplyMuteAsync(snapshot, new[] { locationId }, muted, ct);
            await SaveMuteAsync(tummlyGuestId, new[] { locationId }, muted, ct);
        }

        public async Task SetRestaurantMuteAsync(
            int tummlyGuestId,
            int restaurantId,
            bool muted,
            CancellationToken ct
        )
        {
            var snapshot = await LoadSnapshotAsync(tummlyGuestId, ct);
            var locationIds = snapshot
                .Matched.Where(lg => lg.RestaurantLocation!.RestaurantId == restaurantId)
                .Select(lg => lg.RestaurantLocationId)
                .Distinct()
                .ToList();
            if (locationIds.Count == 0)
            {
                throw new KeyNotFoundException(
                    "Restaurant is not on this guest's preference list."
                );
            }

            await ApplyMuteAsync(snapshot, locationIds, muted, ct);
            await SaveMuteAsync(tummlyGuestId, locationIds, muted, ct);
        }

        /// <summary>
        /// Saves a mute change. Two mute requests can both pass the in-memory check. The second
        /// insert then fails on the unique (guest, location) index. When the other request has
        /// muted every location, this request is done. Any other save error is rethrown.
        /// </summary>
        private async Task SaveMuteAsync(
            int tummlyGuestId,
            IReadOnlyCollection<int> locationIds,
            bool muted,
            CancellationToken ct
        )
        {
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException) when (muted)
            {
                _context.ChangeTracker.Clear();

                var ids = locationIds.ToList();
                var mutedNow = await _context.TummlyGuestLocationMutes
                    .AsNoTracking()
                    .Where(m =>
                        m.TummlyGuestId == tummlyGuestId
                        && ids.Contains(m.RestaurantLocationId)
                    )
                    .Select(m => m.RestaurantLocationId)
                    .ToListAsync(ct);

                if (ids.Any(id => !mutedNow.Contains(id)))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Tick or untick one channel. Writes the Tummly event, then restaurant
        /// withdraws (untick) or D5 restores (tick). Caller saves.
        /// </summary>
        private async Task ApplyChannelAsync(
            GuestSnapshot snapshot,
            ChannelSpec channel,
            bool on,
            DateTime now,
            CancellationToken ct
        )
        {
            _context.TummlyGuestMarketingEvents.Add(
                new TummlyGuestMarketingEvent
                {
                    TummlyGuestId = snapshot.Guest.Id,
                    Channel = channel.Channel,
                    EventKind = on
                        ? LocationGuestPermissionLedgerEventKinds.Grant
                        : LocationGuestPermissionLedgerEventKinds.Withdraw,
                    Source = LocationGuestPermissionLedgerSources.PreferenceCentre,
                    WordingSnapshot = on ? channel.WordingSnapshot : null,
                    OccurredAt = now,
                    CreatedAt = now,
                }
            );

            if (on)
            {
                var unmutedGuests = snapshot
                    .Matched.Where(lg => !snapshot.MutedLocationIds.Contains(lg.RestaurantLocationId))
                    .ToList();
                await RestoreChannelAsync(channel, unmutedGuests, now, ct);
                return;
            }

            foreach (var lg in snapshot.Matched)
            {
                await WithdrawIfGrantedAsync(snapshot, lg, channel.Kind, now, ct);
            }
        }

        /// <summary>
        /// Mute withdraws Email and SMS at the locations. Unmute removes the mute
        /// rows and restores only the channels whose checkbox is on (D6), judged
        /// before this change. Caller saves.
        /// </summary>
        private async Task ApplyMuteAsync(
            GuestSnapshot snapshot,
            IReadOnlyCollection<int> locationIds,
            bool muted,
            CancellationToken ct
        )
        {
            var now = DateTime.UtcNow;

            if (muted)
            {
                foreach (var locationId in locationIds)
                {
                    if (snapshot.MutedLocationIds.Add(locationId))
                    {
                        _context.TummlyGuestLocationMutes.Add(
                            new TummlyGuestLocationMute
                            {
                                TummlyGuestId = snapshot.Guest.Id,
                                RestaurantLocationId = locationId,
                                MutedAt = now,
                            }
                        );
                    }
                }

                var scope = snapshot.Matched.Where(lg => locationIds.Contains(lg.RestaurantLocationId));
                foreach (var lg in scope)
                {
                    foreach (var channel in Channels)
                    {
                        await WithdrawIfGrantedAsync(snapshot, lg, channel.Kind, now, ct);
                    }
                }

                return;
            }

            var unmuting = locationIds
                .Where(id => snapshot.MutedLocationIds.Contains(id))
                .ToList();
            if (unmuting.Count == 0)
            {
                return;
            }

            var channelsOn = Channels.Where(c => IsChannelOn(snapshot, c)).ToList();

            var muteRows = await _context.TummlyGuestLocationMutes
                .Where(m =>
                    m.TummlyGuestId == snapshot.Guest.Id
                    && unmuting.Contains(m.RestaurantLocationId)
                )
                .ToListAsync(ct);
            _context.TummlyGuestLocationMutes.RemoveRange(muteRows);
            snapshot.MutedLocationIds.ExceptWith(unmuting);

            var restoreTargets = snapshot
                .Matched.Where(lg => unmuting.Contains(lg.RestaurantLocationId))
                .ToList();
            foreach (var channel in channelsOn)
            {
                await RestoreChannelAsync(channel, restoreTargets, now, ct);
            }
        }

        /// <summary>
        /// Withdraws one kind at one Location Guest unless already withdrawn.
        /// Writes the rollup and a location activity when the guest flips to OptedOut.
        /// </summary>
        private async Task WithdrawIfGrantedAsync(
            GuestSnapshot snapshot,
            LocationGuest lg,
            LocationGuestPermissionKind kind,
            DateTime now,
            CancellationToken ct
        )
        {
            if (StateOf(snapshot, lg.Id, kind) == LocationGuestPermissionState.Withdrawn)
            {
                return;
            }

            var wasOptedOut = lg.MarketingPreference == LocationGuestMarketingPreference.OptedOut;

            _permissions.RecordEvent(
                lg,
                lg.RestaurantLocationId,
                kind,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.PreferenceCentre,
                now
            );

            var rollup = await _permissions.SyncMarketingPreferenceRollupAsync(lg, ct);
            if (rollup == LocationGuestMarketingPreference.OptedOut && !wasOptedOut)
            {
                _context.LocationActivities.Add(
                    new LocationActivity
                    {
                        RestaurantId = lg.RestaurantLocation!.RestaurantId,
                        LocationId = lg.RestaurantLocationId,
                        ActorUserId = 0,
                        ActorDisplayName = ActivityActor,
                        Kind = LocationActivityKinds.GuestMarketingUnsubscribed,
                        Description = ActivityDescription,
                        OccurredAt = now,
                    }
                );
            }
        }

        /// <summary>
        /// D5: grants one kind again only where the guest's own withdraw is the
        /// latest ledger row. One history query for all targets.
        /// </summary>
        private async Task RestoreChannelAsync(
            ChannelSpec channel,
            IReadOnlyList<LocationGuest> targets,
            DateTime now,
            CancellationToken ct
        )
        {
            if (targets.Count == 0)
            {
                return;
            }

            var history = await LoadHistoryAsync(
                targets.Select(lg => lg.Id).ToList(),
                channel.Kind,
                ct
            );

            foreach (var lg in targets)
            {
                if (
                    !history.TryGetValue(lg.Id, out var rows)
                    || !GuestPreferenceRestoreRule.CanRestore(rows)
                )
                {
                    continue;
                }

                // The row must prove the consent terms: the guest's tick on the
                // preference centre shows the same checkbox text as the Tummly grant.
                _permissions.RecordEvent(
                    lg,
                    lg.RestaurantLocationId,
                    channel.Kind,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.PreferenceCentre,
                    now,
                    new PermissionLedgerEvidence(
                        Basis: LocationGuestPermissionBases.Consent,
                        GuestFormVersion: null,
                        WordingVersion: WordingVersion,
                        PrivacyNoticeVersion: null,
                        WordingSnapshot: channel.WordingSnapshot
                    )
                );
                await _permissions.SyncMarketingPreferenceRollupAsync(lg, ct);
            }
        }

        /// <summary>
        /// Channel shows on when the latest Tummly event is a grant, or any
        /// un-muted matched Location Guest has the kind granted (D6).
        /// </summary>
        private static bool IsChannelOn(GuestSnapshot snapshot, ChannelSpec channel)
        {
            if (
                snapshot.LatestTummlyEventKinds.TryGetValue(channel.Channel, out var kind)
                && kind == LocationGuestPermissionLedgerEventKinds.Grant
            )
            {
                return true;
            }

            return snapshot.Matched.Any(lg =>
                !snapshot.MutedLocationIds.Contains(lg.RestaurantLocationId)
                && StateOf(snapshot, lg.Id, channel.Kind) == LocationGuestPermissionState.Granted
            );
        }

        private static LocationGuestPermissionState StateOf(
            GuestSnapshot snapshot,
            int locationGuestId,
            LocationGuestPermissionKind kind
        )
        {
            if (
                snapshot.States.TryGetValue(locationGuestId, out var byKind)
                && byKind.TryGetValue(kind, out var state)
            )
            {
                return state;
            }

            return LocationGuestPermissionState.NotRecorded;
        }

        private async Task<GuestSnapshot> LoadSnapshotAsync(
            int tummlyGuestId,
            CancellationToken ct
        )
        {
            var guest = await _context
                .TummlyGuests.Include(g => g.Contacts)
                .FirstOrDefaultAsync(g => g.Id == tummlyGuestId, ct)
                ?? throw new KeyNotFoundException("Tummly Guest not found.");

            // FindLocationGuestsAsync is read-only. Its change-tracker reset is
            // only in the identity service's find-or-create and link paths. Read
            // state before any change is staged anyway.
            var matched = await _identity.FindLocationGuestsAsync(tummlyGuestId, ct);
            var muted = await LoadMutedLocationIdsAsync(tummlyGuestId, ct);
            var states = await _permissions.GetCurrentStatesBatchAsync(
                matched.Select(lg => lg.Id).ToList(),
                ct
            );

            var latest = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var channel in Channels)
            {
                var kind = await LatestTummlyEventKindAsync(tummlyGuestId, channel.Channel, ct);
                if (kind is not null)
                {
                    latest[channel.Channel] = kind;
                }
            }

            return new GuestSnapshot(guest, matched, muted, states, latest);
        }

        private async Task EnsureGuestExistsAsync(int tummlyGuestId, CancellationToken ct)
        {
            if (!await _context.TummlyGuests.AnyAsync(g => g.Id == tummlyGuestId, ct))
            {
                throw new KeyNotFoundException("Tummly Guest not found.");
            }
        }

        private async Task<HashSet<int>> LoadMutedLocationIdsAsync(
            int tummlyGuestId,
            CancellationToken ct
        ) =>
            (
                await _context.TummlyGuestLocationMutes
                    .AsNoTracking()
                    .Where(m => m.TummlyGuestId == tummlyGuestId)
                    .Select(m => m.RestaurantLocationId)
                    .ToListAsync(ct)
            ).ToHashSet();

        private async Task<string?> LatestTummlyEventKindAsync(
            int tummlyGuestId,
            string channel,
            CancellationToken ct
        ) =>
            await _context.TummlyGuestMarketingEvents
                .AsNoTracking()
                .Where(e => e.TummlyGuestId == tummlyGuestId && e.Channel == channel)
                .OrderByDescending(e => e.OccurredAt)
                .ThenByDescending(e => e.Id)
                .Select(e => e.EventKind)
                .FirstOrDefaultAsync(ct);

        private async Task<
            Dictionary<int, IReadOnlyList<LocationGuestPermissionLedgerEntry>>
        > LoadHistoryAsync(
            IReadOnlyList<int> locationGuestIds,
            LocationGuestPermissionKind kind,
            CancellationToken ct
        )
        {
            if (locationGuestIds.Count == 0)
            {
                return new Dictionary<int, IReadOnlyList<LocationGuestPermissionLedgerEntry>>();
            }

            var rows = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(e =>
                    locationGuestIds.Contains(e.LocationGuestId)
                    && e.PermissionKind == kind
                )
                .ToListAsync(ct);

            return rows
                .GroupBy(e => e.LocationGuestId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<LocationGuestPermissionLedgerEntry>)g.ToList()
                );
        }

        private static bool ContainsIgnoreCase(string? value, string term) =>
            value?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
