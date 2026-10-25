using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IGuestInitiatedMarketingWithdrawService"/> — guest-initiated
    /// email unsubscribe / SMS STOP withdraw via permission ledger + rollup.
    /// </summary>
    public class GuestInitiatedMarketingWithdrawServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IGuestContactHasher _hasher;
        private readonly IGuestInitiatedMarketingWithdrawService _sut;

        public GuestInitiatedMarketingWithdrawServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            _hasher = new GuestContactHasher(
                GuestContactHasher.TestingFallbackKey
            );
            var suppressions = new GuestContactSuppressionService(
                _context,
                _hasher
            );
            _sut = new GuestInitiatedMarketingWithdrawService(
                _context,
                _permissions,
                suppressions
            );
        }

        [Fact]
        public async Task WithdrawForLocationGuest_WritesEmailMarketingWithdraw_AndOptsOut()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "guest@example.com",
                restaurantName: "Venue A"
            );

            var result = await _sut.WithdrawForLocationGuestAsync(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);
            Assert.True(result.ActivityEmitted);

            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.EmailMarketing]
            );

            var lg = await _context.LocationGuests.SingleAsync();
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                lg.MarketingPreference
            );
            Assert.Equal(
                1,
                await _context.LocationActivities.CountAsync(a =>
                    a.Kind == LocationActivityKinds.GuestMarketingUnsubscribed
                )
            );

            var entry = await _context.LocationGuestPermissionLedgerEntries
                .SingleAsync(e =>
                    e.EventKind
                    == LocationGuestPermissionLedgerEventKinds.Withdraw
                );
            Assert.Equal(
                LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                entry.Source
            );
            Assert.Null(entry.ActorUserId);
        }

        [Fact]
        public async Task WithdrawForLocationGuest_IsIdempotent_WhenAlreadyWithdrawn()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "idempotent@example.com",
                restaurantName: "Venue Idem"
            );

            var first = await _sut.WithdrawForLocationGuestAsync(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );
            Assert.Equal(1, first.WithdrawalsWritten);
            Assert.True(first.ActivityEmitted);

            var second = await _sut.WithdrawForLocationGuestAsync(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );

            Assert.Equal(1, second.GuestsTouched);
            Assert.Equal(0, second.WithdrawalsWritten);
            Assert.False(second.ActivityEmitted);
            Assert.Equal(
                1,
                await _context.LocationGuestPermissionLedgerEntries.CountAsync(
                    e =>
                        e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Withdraw
                )
            );
            Assert.Equal(
                1,
                await _context.LocationActivities.CountAsync(a =>
                    a.Kind == LocationActivityKinds.GuestMarketingUnsubscribed
                )
            );
        }

        [Fact]
        public async Task WithdrawForRestaurantContact_Email_OnlyTouchesThatRestaurant()
        {
            const string sharedEmail = "shared@example.com";
            var normalized = sharedEmail.Trim().ToLowerInvariant();

            var restaurantA = await SeedGrantedEmailGuestAsync(
                email: sharedEmail,
                restaurantName: "Restaurant A"
            );
            var restaurantB = await SeedGrantedEmailGuestAsync(
                email: sharedEmail,
                restaurantName: "Restaurant B"
            );

            var result = await _sut.WithdrawForRestaurantContactAsync(
                restaurantA.RestaurantId,
                normalized,
                isEmail: true,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);
            Assert.True(result.ActivityEmitted);

            var statesA = await _permissions.GetCurrentStatesAsync(
                restaurantA.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                statesA[LocationGuestPermissionKind.EmailMarketing]
            );

            var statesB = await _permissions.GetCurrentStatesAsync(
                restaurantB.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                statesB[LocationGuestPermissionKind.EmailMarketing]
            );

            var guestB = await _context.LocationGuests.SingleAsync(lg =>
                lg.Id == restaurantB.LocationGuestId
            );
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                guestB.MarketingPreference
            );
        }

        [Fact]
        public async Task WithdrawForRestaurantContact_Sms_MatchesDigitsOnlyNormalizedPhone()
        {
            const string e164 = "+447123456789";
            var digits = new string(e164.Where(char.IsDigit).ToArray());

            var seeded = await SeedGrantedSmsGuestAsync(
                mobile: e164,
                normalizedPhone: digits,
                restaurantName: "SMS Venue"
            );

            var result = await _sut.WithdrawForRestaurantContactAsync(
                seeded.RestaurantId,
                e164,
                isEmail: false,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerSources.SmsStop
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);

            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        [Fact]
        public async Task WithdrawForRestaurantContact_MatchesNationalFormatGuest()
        {
            // Guest typed national 07…; Twilio STOP From is E.164 +44…
            const string national = "07123456789";
            const string stopFrom = "+447123456789";

            var seeded = await SeedGrantedSmsGuestAsync(
                mobile: national,
                normalizedPhone: national,
                restaurantName: "National Format Venue"
            );

            var result = await _sut.WithdrawForRestaurantContactAsync(
                seeded.RestaurantId,
                stopFrom,
                isEmail: false,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerSources.SmsStop
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);

            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
            Assert.Empty(_context.GuestContactSuppressions);
        }

        [Fact]
        public async Task WithdrawForRestaurantContact_NoGuest_WritesSuppressionRow()
        {
            var restaurant = await SeedRestaurantAsync("No Guest Venue");

            var emailResult = await _sut.WithdrawForRestaurantContactAsync(
                restaurant.Id,
                "unknown@example.com",
                isEmail: true,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );
            Assert.Equal(0, emailResult.GuestsTouched);

            var smsResult = await _sut.WithdrawForRestaurantContactAsync(
                restaurant.Id,
                "+447123456789",
                isEmail: false,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerSources.SmsStop
            );
            Assert.Equal(0, smsResult.GuestsTouched);

            var rows = await _context.GuestContactSuppressions
                .OrderBy(r => r.PermissionKind)
                .ToListAsync();
            Assert.Equal(2, rows.Count);

            var emailRow = Assert.Single(
                rows,
                r =>
                    r.PermissionKind
                    == LocationGuestPermissionKind.EmailMarketing
            );
            Assert.Equal(
                GuestContactSuppressionSources.EmailUnsubscribe,
                emailRow.Source
            );
            Assert.Equal(
                _hasher.Hash("email:unknown@example.com"),
                emailRow.ContactHash
            );
            Assert.DoesNotContain("@", emailRow.ContactHash);

            var smsRow = Assert.Single(
                rows,
                r =>
                    r.PermissionKind
                    == LocationGuestPermissionKind.SmsMarketing
            );
            Assert.Equal(
                GuestContactSuppressionSources.SmsStop,
                smsRow.Source
            );
            Assert.Equal(
                _hasher.Hash("phone:+447123456789"),
                smsRow.ContactHash
            );
            Assert.DoesNotContain("447123456789", smsRow.ContactHash);
        }

        [Fact]
        public async Task WithdrawForRestaurantContact_GuestFound_WritesNoSuppressionRow()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "known@example.com",
                restaurantName: "Known Guest Venue"
            );

            var result = await _sut.WithdrawForRestaurantContactAsync(
                seeded.RestaurantId,
                "known@example.com",
                isEmail: true,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);
            Assert.Empty(_context.GuestContactSuppressions);
        }

        private async Task<Restaurant> SeedRestaurantAsync(string name)
        {
            var restaurant = new Restaurant
            {
                Name = name,
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
            return restaurant;
        }

        private async Task<SeededGuest> SeedGrantedSmsGuestAsync(
            string mobile,
            string normalizedPhone,
            string restaurantName
        )
        {
            var restaurant = await SeedRestaurantAsync(restaurantName);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = $"{restaurantName} Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = mobile,
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

            _permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)
            );
            await _context.SaveChangesAsync();

            return new SeededGuest(
                restaurant.Id,
                location.Id,
                guest.Id
            );
        }

        private async Task<SeededGuest> SeedGrantedEmailGuestAsync(
            string email,
            string restaurantName
        )
        {
            var restaurant = new Restaurant
            {
                Name = restaurantName,
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
                LocationName = $"{restaurantName} Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = email,
                NormalizedEmail = normalizedEmail,
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

            var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            _permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await _context.SaveChangesAsync();

            return new SeededGuest(
                restaurant.Id,
                location.Id,
                guest.Id
            );
        }

        private sealed record SeededGuest(
            int RestaurantId,
            int LocationId,
            int LocationGuestId
        );

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
