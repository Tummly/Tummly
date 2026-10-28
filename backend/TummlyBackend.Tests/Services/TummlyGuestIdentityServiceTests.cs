using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ITummlyGuestIdentityService"/> — find or create by contact,
    /// link a contact (merging a duplicate Tummly Guest), and match Location Guests
    /// across restaurants by normalized contact.
    /// </summary>
    public class TummlyGuestIdentityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly ITummlyGuestIdentityService _sut;

        public TummlyGuestIdentityServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            _sut = new TummlyGuestIdentityService(_context);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task FindOrCreate_CreatesOnce_ThenReturnsSame()
        {
            var first = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );
            var second = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            Assert.Equal(first.TummlyGuestId, second.TummlyGuestId);
            Assert.Equal(first.ContactId, second.ContactId);
            Assert.Equal(1, await _context.TummlyGuests.CountAsync());
            Assert.Equal(1, await _context.TummlyGuestContacts.CountAsync());
        }

        [Fact]
        public async Task LinkContact_AddsSecondContact()
        {
            var owner = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            var contactId = await _sut.LinkContactAsync(
                owner.TummlyGuestId,
                TummlyGuestContactTypes.Phone,
                "+447911123456",
                CancellationToken.None
            );

            var contacts = await _context
                .TummlyGuestContacts.Where(c =>
                    c.TummlyGuestId == owner.TummlyGuestId
                )
                .ToListAsync();
            Assert.Equal(2, contacts.Count);
            Assert.Contains(contacts, c =>
                c.Id == contactId
                && c.ContactType == TummlyGuestContactTypes.Phone
                && c.NormalizedValue == "+447911123456"
            );
            Assert.Equal(1, await _context.TummlyGuests.CountAsync());
        }

        [Fact]
        public async Task LinkContact_SameOwner_ReturnsExistingRow()
        {
            var owner = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            var contactId = await _sut.LinkContactAsync(
                owner.TummlyGuestId,
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            Assert.Equal(owner.ContactId, contactId);
            Assert.Equal(1, await _context.TummlyGuestContacts.CountAsync());
        }

        [Fact]
        public async Task LinkContact_MergesOtherGuest_WhenContactOwnedElsewhere()
        {
            var guestA = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            var now = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
            var guestB = new TummlyGuest { CreatedAt = now, UpdatedAt = now };
            guestB.Contacts.Add(
                new TummlyGuestContact
                {
                    ContactType = TummlyGuestContactTypes.Phone,
                    NormalizedValue = "+447911123456",
                    VerifiedAt = now,
                    CreatedAt = now,
                }
            );
            guestB.LocationMutes.Add(
                new TummlyGuestLocationMute
                {
                    RestaurantLocationId = 7,
                    MutedAt = now,
                }
            );
            guestB.MarketingEvents.Add(
                new TummlyGuestMarketingEvent
                {
                    Channel = TummlyGuestChannels.Sms,
                    EventKind = LocationGuestPermissionLedgerEventKinds.Grant,
                    Source = LocationGuestPermissionLedgerSources.PreferenceCentre,
                    OccurredAt = now,
                    CreatedAt = now,
                }
            );
            _context.TummlyGuests.Add(guestB);
            await _context.SaveChangesAsync();
            var phoneRowId = guestB.Contacts.Single().Id;
            var guestBId = guestB.Id;

            var contactId = await _sut.LinkContactAsync(
                guestA.TummlyGuestId,
                TummlyGuestContactTypes.Phone,
                "+447911123456",
                CancellationToken.None
            );

            _context.ChangeTracker.Clear();
            Assert.Null(await _context.TummlyGuests.FindAsync(guestBId));
            Assert.Equal(1, await _context.TummlyGuests.CountAsync());
            Assert.Equal(phoneRowId, contactId);

            var contacts = await _context
                .TummlyGuestContacts.Where(c =>
                    c.TummlyGuestId == guestA.TummlyGuestId
                )
                .ToListAsync();
            Assert.Equal(2, contacts.Count);
            Assert.Contains(contacts, c => c.Id == contactId);

            var mutes = await _context
                .TummlyGuestLocationMutes.Where(m =>
                    m.TummlyGuestId == guestA.TummlyGuestId
                )
                .ToListAsync();
            Assert.Equal(7, Assert.Single(mutes).RestaurantLocationId);

            var events = await _context
                .TummlyGuestMarketingEvents.Where(e =>
                    e.TummlyGuestId == guestA.TummlyGuestId
                )
                .ToListAsync();
            Assert.Single(events);
        }

        [Fact]
        public async Task LinkContact_Merge_KeepsOneMutePerLocation()
        {
            var guestA = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );
            var now = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
            _context.TummlyGuestLocationMutes.Add(
                new TummlyGuestLocationMute
                {
                    TummlyGuestId = guestA.TummlyGuestId,
                    RestaurantLocationId = 7,
                    MutedAt = now,
                }
            );

            var guestB = new TummlyGuest { CreatedAt = now, UpdatedAt = now };
            guestB.Contacts.Add(
                new TummlyGuestContact
                {
                    ContactType = TummlyGuestContactTypes.Phone,
                    NormalizedValue = "+447911123456",
                    VerifiedAt = now,
                    CreatedAt = now,
                }
            );
            guestB.LocationMutes.Add(
                new TummlyGuestLocationMute
                {
                    RestaurantLocationId = 7,
                    MutedAt = now,
                }
            );
            guestB.LocationMutes.Add(
                new TummlyGuestLocationMute
                {
                    RestaurantLocationId = 9,
                    MutedAt = now,
                }
            );
            _context.TummlyGuests.Add(guestB);
            await _context.SaveChangesAsync();

            await _sut.LinkContactAsync(
                guestA.TummlyGuestId,
                TummlyGuestContactTypes.Phone,
                "+447911123456",
                CancellationToken.None
            );

            _context.ChangeTracker.Clear();
            var locationIds = await _context
                .TummlyGuestLocationMutes.Where(m =>
                    m.TummlyGuestId == guestA.TummlyGuestId
                )
                .Select(m => m.RestaurantLocationId)
                .ToListAsync();
            Assert.Equal(new[] { 7, 9 }, locationIds.Order());
        }

        [Fact]
        public async Task FindLocationGuests_MatchesAcrossRestaurants_CaseInsensitive()
        {
            var venueA = await SeedLocationGuestAsync(
                "Venue A",
                normalizedEmail: "sophie@example.com",
                normalizedPhone: null,
                grantedKind: LocationGuestPermissionKind.EmailMarketing
            );
            var venueB = await SeedLocationGuestAsync(
                "Venue B",
                normalizedEmail: "sophie@example.com",
                normalizedPhone: null,
                grantedKind: LocationGuestPermissionKind.EmailMarketing
            );
            await SeedLocationGuestAsync(
                "Venue C",
                normalizedEmail: "someone-else@example.com",
                normalizedPhone: null,
                grantedKind: LocationGuestPermissionKind.EmailMarketing
            );

            Assert.True(
                GuestPreferenceContact.TryParse(
                    "  Sophie@Example.com ",
                    out var contactType,
                    out var normalizedValue
                )
            );
            var owner = await _sut.FindOrCreateByContactAsync(
                contactType,
                normalizedValue,
                CancellationToken.None
            );

            var results = await _sut.FindLocationGuestsAsync(
                owner.TummlyGuestId,
                CancellationToken.None
            );

            Assert.Equal(
                new[] { venueA.Id, venueB.Id }.Order(),
                results.Select(r => r.Id).Order()
            );
            Assert.All(
                results,
                r => Assert.NotNull(r.RestaurantLocation?.Restaurant)
            );
        }

        [Fact]
        public async Task FindLocationGuests_MatchesPhone_NationalFormat()
        {
            await SeedLocationGuestAsync(
                "Phone Venue",
                normalizedEmail: null,
                normalizedPhone: "+447911123456",
                grantedKind: LocationGuestPermissionKind.SmsMarketing
            );

            // 07700 900xxx is rejected by PhoneNumberHelper (R5), so use 07911.
            Assert.True(
                GuestPreferenceContact.TryParse(
                    "07911 123456",
                    out var contactType,
                    out var normalizedValue
                )
            );
            var owner = await _sut.FindOrCreateByContactAsync(
                contactType,
                normalizedValue,
                CancellationToken.None
            );

            var results = await _sut.FindLocationGuestsAsync(
                owner.TummlyGuestId,
                CancellationToken.None
            );

            Assert.Single(results);
        }

        [Fact]
        public async Task FindLocationGuests_ExcludesNeverGranted()
        {
            await SeedLocationGuestAsync(
                "Never Granted Venue",
                normalizedEmail: "sophie@example.com",
                normalizedPhone: null,
                grantedKind: null
            );
            var owner = await _sut.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                "sophie@example.com",
                CancellationToken.None
            );

            var results = await _sut.FindLocationGuestsAsync(
                owner.TummlyGuestId,
                CancellationToken.None
            );

            Assert.Empty(results);
        }

        private async Task<LocationGuest> SeedLocationGuestAsync(
            string venueName,
            string? normalizedEmail,
            string? normalizedPhone,
            LocationGuestPermissionKind? grantedKind
        )
        {
            var restaurant = new Restaurant
            {
                Name = venueName,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = $"{venueName} Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = normalizedEmail,
                NormalizedEmail = normalizedEmail,
                NormalizedPhone = normalizedPhone,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(masterGuest);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            if (grantedKind is { } kind)
            {
                _permissions.RecordEvent(
                    guest.Id,
                    guest.RestaurantLocationId,
                    kind,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)
                );
                await _context.SaveChangesAsync();
            }

            return guest;
        }
    }
}
