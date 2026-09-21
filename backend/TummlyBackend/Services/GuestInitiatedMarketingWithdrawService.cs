using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestInitiatedMarketingWithdrawService
        : IGuestInitiatedMarketingWithdrawService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;

        public GuestInitiatedMarketingWithdrawService(
            ApplicationDbContext context,
            ILocationGuestPermissionLedgerService permissions
        )
        {
            _context = context;
            _permissions = permissions;
        }

        public async Task<GuestInitiatedWithdrawResult> WithdrawForLocationGuestAsync(
            int locationGuestId,
            int restaurantId,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken = default
        )
        {
            var guest = await _context.LocationGuests
                .Include(lg => lg.RestaurantLocation)
                .FirstOrDefaultAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocation != null
                        && lg.RestaurantLocation.RestaurantId == restaurantId,
                    cancellationToken
                );

            if (guest == null)
            {
                return new GuestInitiatedWithdrawResult(
                    GuestsTouched: 0,
                    WithdrawalsWritten: 0,
                    ActivityEmitted: false
                );
            }

            return await WithdrawGuestsAsync(
                new[] { guest },
                restaurantId,
                kind,
                ledgerSource,
                cancellationToken
            );
        }

        public async Task<GuestInitiatedWithdrawResult> WithdrawForRestaurantContactAsync(
            int restaurantId,
            string normalizedEmailOrE164,
            bool isEmail,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken = default
        )
        {
            var normalized = (normalizedEmailOrE164 ?? string.Empty).Trim();
            if (!isEmail)
            {
                // MasterGuest.NormalizedPhone is digits-only (GuestUpsertService).
                // Twilio STOP From is E.164 (+…). Strip to digits before match.
                normalized = new string(normalized.Where(char.IsDigit).ToArray());
            }
            else
            {
                normalized = normalized.ToLowerInvariant();
            }

            if (normalized.Length == 0)
            {
                return new GuestInitiatedWithdrawResult(
                    GuestsTouched: 0,
                    WithdrawalsWritten: 0,
                    ActivityEmitted: false
                );
            }

            IQueryable<LocationGuest> query = _context.LocationGuests
                .Include(lg => lg.RestaurantLocation)
                .Include(lg => lg.MasterGuest)
                .Where(lg =>
                    lg.RestaurantLocation != null
                    && lg.RestaurantLocation.RestaurantId == restaurantId
                );

            if (isEmail)
            {
                query = query.Where(lg =>
                    lg.MasterGuest != null
                    && lg.MasterGuest.NormalizedEmail == normalized
                );
            }
            else
            {
                query = query.Where(lg =>
                    lg.MasterGuest != null
                    && lg.MasterGuest.NormalizedPhone == normalized
                );
            }

            var guests = await query.ToListAsync(cancellationToken);

            return await WithdrawGuestsAsync(
                guests,
                restaurantId,
                kind,
                ledgerSource,
                cancellationToken
            );
        }

        private async Task<GuestInitiatedWithdrawResult> WithdrawGuestsAsync(
            IReadOnlyList<LocationGuest> guests,
            int restaurantId,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken
        )
        {
            var utcNow = DateTime.UtcNow;
            var withdrawalsWritten = 0;
            var activityEmitted = false;

            foreach (var guest in guests)
            {
                var wasOptedOut =
                    guest.MarketingPreference
                    == LocationGuestMarketingPreference.OptedOut;

                var states = await _permissions.GetCurrentStatesAsync(
                    guest.Id,
                    cancellationToken
                );

                if (
                    states[kind]
                    == LocationGuestPermissionState.Withdrawn
                )
                {
                    continue;
                }

                _permissions.RecordEvent(
                    guest,
                    guest.RestaurantLocationId,
                    kind,
                    LocationGuestPermissionLedgerEventKinds.Withdraw,
                    ledgerSource,
                    utcNow,
                    actorUserId: null
                );
                withdrawalsWritten++;

                var rollup = await _permissions.SyncMarketingPreferenceRollupAsync(
                    guest,
                    cancellationToken
                );

                if (
                    rollup == LocationGuestMarketingPreference.OptedOut
                    && !wasOptedOut
                )
                {
                    _context.LocationActivities.Add(
                        new LocationActivity
                        {
                            RestaurantId = restaurantId,
                            LocationId = guest.RestaurantLocationId,
                            ActorUserId = 0,
                            ActorDisplayName = "Guest",
                            Kind =
                                LocationActivityKinds.GuestMarketingUnsubscribed,
                            Description =
                                "Guest withdrew marketing permission at this location.",
                            OccurredAt = utcNow,
                        }
                    );
                    activityEmitted = true;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new GuestInitiatedWithdrawResult(
                GuestsTouched: guests.Count,
                WithdrawalsWritten: withdrawalsWritten,
                ActivityEmitted: activityEmitted
            );
        }
    }
}
