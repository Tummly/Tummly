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
        private readonly IGuestInitiatedMarketingWithdrawService _sut;

        public GuestInitiatedMarketingWithdrawServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            _sut = new GuestInitiatedMarketingWithdrawService(
                _context,
                _permissions
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
            const string e164 = "+447700900111";
            var digits = new string(e164.Where(char.IsDigit).ToArray());

            var restaurant = new Restaurant
            {
                Name = "SMS Venue",
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
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = e164,
                NormalizedPhone = digits,
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

            var result = await _sut.WithdrawForRestaurantContactAsync(
                restaurant.Id,
                e164,
                isEmail: false,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerSources.SmsStop
            );

            Assert.Equal(1, result.GuestsTouched);
            Assert.Equal(1, result.WithdrawalsWritten);

            var states = await _permissions.GetCurrentStatesAsync(guest.Id);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
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
