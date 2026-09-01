using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestUpsertService : IGuestUpsertService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityRecorder _recorder;

        public GuestUpsertService(
            ApplicationDbContext context,
            ILocationGuestActivityRecorder recorder
        )
        {
            _context = context;
            _recorder = recorder;
        }

        public async Task<LocationGuest> ResolveOrCreateAsync(
            int restaurantId,
            int restaurantLocationId,
            string guestName,
            string guestContact,
            ContactType contactType,
            bool offersOptOut,
            DateTime? eventAt = null,
            CancellationToken cancellationToken = default
        )
        {
            var trimmedName = guestName.Trim();
            var trimmedContact = guestContact.Trim();
            var createdAt = eventAt ?? DateTime.UtcNow;

            var masterGuest = await ResolveOrCreateMasterGuestAsync(
                restaurantId,
                trimmedContact,
                contactType,
                createdAt,
                cancellationToken
            );

            var locationGuest = await ResolveOrCreateLocationGuestAsync(
                masterGuest,
                restaurantLocationId,
                trimmedName,
                offersOptOut,
                createdAt,
                cancellationToken
            );

            return locationGuest;
        }

        private async Task<MasterGuest> ResolveOrCreateMasterGuestAsync(
            int restaurantId,
            string trimmedContact,
            ContactType contactType,
            DateTime createdAt,
            CancellationToken cancellationToken
        )
        {
            MasterGuest? masterGuest = null;

            if (contactType == ContactType.Email)
            {
                var normalizedEmail = NormalizeEmail(trimmedContact);
                masterGuest = await _context.MasterGuests
                    .FirstOrDefaultAsync(
                        g =>
                            g.RestaurantId == restaurantId
                            && g.NormalizedEmail == normalizedEmail,
                        cancellationToken
                    );

                if (masterGuest == null)
                {
                    masterGuest = new MasterGuest
                    {
                        RestaurantId = restaurantId,
                        Email = trimmedContact,
                        NormalizedEmail = normalizedEmail,
                        CreatedAt = createdAt,
                    };
                    _context.MasterGuests.Add(masterGuest);
                }
                else
                {
                    masterGuest.Email = trimmedContact;
                    masterGuest.NormalizedEmail = normalizedEmail;
                }
            }
            else if (contactType == ContactType.Phone)
            {
                var normalizedPhone = NormalizePhone(trimmedContact);
                masterGuest = await _context.MasterGuests
                    .FirstOrDefaultAsync(
                        g =>
                            g.RestaurantId == restaurantId
                            && g.NormalizedPhone == normalizedPhone,
                        cancellationToken
                    );

                if (masterGuest == null)
                {
                    masterGuest = new MasterGuest
                    {
                        RestaurantId = restaurantId,
                        Mobile = trimmedContact,
                        NormalizedPhone = normalizedPhone,
                        CreatedAt = createdAt,
                    };
                    _context.MasterGuests.Add(masterGuest);
                }
                else
                {
                    masterGuest.Mobile = trimmedContact;
                    masterGuest.NormalizedPhone = normalizedPhone;
                }
            }
            else
            {
                // Unknown: create a Master Guest with neither channel set.
                // No identity key — cannot match on later submits (no email↔phone merge).
                masterGuest = new MasterGuest
                {
                    RestaurantId = restaurantId,
                    CreatedAt = createdAt,
                };
                _context.MasterGuests.Add(masterGuest);
            }

            return masterGuest;
        }

        private async Task<LocationGuest> ResolveOrCreateLocationGuestAsync(
            MasterGuest masterGuest,
            int restaurantLocationId,
            string trimmedName,
            bool offersOptOut,
            DateTime createdAt,
            CancellationToken cancellationToken
        )
        {
            LocationGuest? locationGuest = null;

            if (masterGuest.Id != 0)
            {
                locationGuest = await _context.LocationGuests
                    .FirstOrDefaultAsync(
                        lg =>
                            lg.MasterGuestId == masterGuest.Id
                            && lg.RestaurantLocationId == restaurantLocationId,
                        cancellationToken
                    );
            }
            else
            {
                locationGuest = _context.LocationGuests.Local
                    .FirstOrDefault(lg =>
                        ReferenceEquals(lg.MasterGuest, masterGuest)
                        && lg.RestaurantLocationId == restaurantLocationId
                    );
            }

            if (locationGuest == null)
            {
                locationGuest = new LocationGuest
                {
                    MasterGuest = masterGuest,
                    RestaurantLocationId = restaurantLocationId,
                    Name = trimmedName,
                    MarketingPreference =
                        LocationGuestMarketingPreference.NotRecorded,
                    CreatedAt = createdAt,
                };
                _context.LocationGuests.Add(locationGuest);
                _recorder.RecordGuestJoined(locationGuest, createdAt);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(trimmedName))
                {
                    locationGuest.Name = trimmedName;
                }
            }

            return locationGuest;
        }

        internal static string NormalizeEmail(string contact)
            => contact.Trim().ToLowerInvariant();

        internal static string NormalizePhone(string contact)
            => new string(contact.Where(char.IsDigit).ToArray());
    }
}
