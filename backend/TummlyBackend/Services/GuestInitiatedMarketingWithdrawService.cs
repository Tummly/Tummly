using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestInitiatedMarketingWithdrawService
        : IGuestInitiatedMarketingWithdrawService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IGuestContactSuppressionService _suppressions;
        private readonly ILogger<GuestInitiatedMarketingWithdrawService> _logger;

        public GuestInitiatedMarketingWithdrawService(
            ApplicationDbContext context,
            ILocationGuestPermissionLedgerService permissions,
            IGuestContactSuppressionService suppressions,
            ILogger<GuestInitiatedMarketingWithdrawService> logger
        )
        {
            _context = context;
            _permissions = permissions;
            _suppressions = suppressions;
            _logger = logger;
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
            var raw = (normalizedEmailOrE164 ?? string.Empty).Trim();
            if (raw.Length == 0)
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

            List<LocationGuest> guests;

            if (isEmail)
            {
                var normalized = raw.ToLowerInvariant();
                guests = await query
                    .Where(lg =>
                        lg.MasterGuest != null
                        && lg.MasterGuest.NormalizedEmail == normalized
                    )
                    .ToListAsync(cancellationToken);
            }
            else
            {
                if (!TryResolveComparableE164(raw, out var inputE164))
                {
                    return new GuestInitiatedWithdrawResult(
                        GuestsTouched: 0,
                        WithdrawalsWritten: 0,
                        ActivityEmitted: false
                    );
                }

                var inputDigits = new string(
                    inputE164.Where(char.IsDigit).ToArray()
                );
                var last9 =
                    inputDigits.Length >= 9
                        ? inputDigits[^9..]
                        : inputDigits;

                var candidates = await query
                    .Where(lg =>
                        lg.MasterGuest != null
                        && lg.MasterGuest.NormalizedPhone != null
                        && lg.MasterGuest.NormalizedPhone.EndsWith(last9)
                    )
                    .ToListAsync(cancellationToken);

                guests = candidates
                    .Where(lg =>
                        MasterGuestMatchesE164(lg.MasterGuest!, inputE164)
                    )
                    .ToList();
            }

            if (guests.Count == 0)
            {
                var contactType = isEmail
                    ? ContactType.Email
                    : ContactType.Phone;
                var source = isEmail
                    ? GuestContactSuppressionSources.EmailUnsubscribe
                    : GuestContactSuppressionSources.SmsStop;

                if (
                    GuestContactKey.TryCreate(
                        raw,
                        contactType,
                        out var contactKey
                    )
                )
                {
                    await _suppressions.StageAsync(
                        restaurantId,
                        contactKey,
                        kind,
                        DateTime.UtcNow,
                        source,
                        cancellationToken
                    );
                }
                else if (isEmail)
                {
                    _logger.LogWarning(
                        "Skipped guest contact suppression: contact is not keyable. RestaurantId={RestaurantId} MaskedEmail={MaskedEmail}",
                        restaurantId,
                        LogContactMask.Email(raw)
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Skipped guest contact suppression: contact is not keyable. RestaurantId={RestaurantId} MaskedPhone={MaskedPhone}",
                        restaurantId,
                        LogContactMask.Phone(raw)
                    );
                }
            }

            // SaveChanges here also persists a staged suppression when no guests
            // matched (WithdrawGuestsAsync always saves).
            return await WithdrawGuestsAsync(
                guests,
                restaurantId,
                kind,
                ledgerSource,
                cancellationToken
            );
        }

        private static bool MasterGuestMatchesE164(
            MasterGuest master,
            string inputE164
        )
        {
            if (
                TryResolveComparableE164(master.Mobile, out var fromMobile)
                && string.Equals(
                    fromMobile,
                    inputE164,
                    StringComparison.Ordinal
                )
            )
            {
                return true;
            }

            return TryResolveComparableE164(
                    master.NormalizedPhone,
                    out var fromNormalized
                )
                && string.Equals(
                    fromNormalized,
                    inputE164,
                    StringComparison.Ordinal
                );
        }

        /// <summary>
        /// Prefer <see cref="PhoneNumberHelper.TryNormalizeToE164"/>; accept
        /// Twilio-style <c>+digits</c> when libphonenumber rejects (test ranges).
        /// Digits-only values that start with 44 are treated as E.164 without +.
        /// </summary>
        private static bool TryResolveComparableE164(
            string? raw,
            out string e164
        )
        {
            e164 = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    raw,
                    PhoneNumberHelper.DefaultRegion,
                    out var normalized
                )
                && !string.IsNullOrEmpty(normalized)
            )
            {
                e164 = normalized;
                return true;
            }

            var trimmed = raw.Trim();
            if (
                trimmed.StartsWith('+')
                && trimmed.Length >= 10
                && trimmed[1..].All(char.IsDigit)
            )
            {
                e164 = trimmed;
                return true;
            }

            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (
                digits.Length >= 10
                && digits.StartsWith("44", StringComparison.Ordinal)
            )
            {
                e164 = "+" + digits;
                return true;
            }

            return false;
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
