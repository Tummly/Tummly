using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ILocationGuestPermissionLedgerService"/> — append-only
    /// permission ledger, current-state derivation, and marketing rollup sync.
    /// </summary>
    public class LocationGuestPermissionLedgerServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _ledger;

        public LocationGuestPermissionLedgerServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _ledger = new LocationGuestPermissionLedgerService(_context);
        }

        [Fact]
        public async Task GetCurrentStatesAsync_UsesLatestEventPerPermissionKind()
        {
            var guest = await SeedLocationGuestAsync(
                LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

            _ledger.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            _ledger.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.GuestForm,
                at.AddMinutes(1)
            );
            _ledger.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await _context.SaveChangesAsync();

            var states = await _ledger.GetCurrentStatesAsync(guest.Id);

            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.EmailMarketing]
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
            Assert.Equal(
                LocationGuestPermissionState.NotRecorded,
                states[LocationGuestPermissionKind.FeedbackFollowUp]
            );
        }

        [Fact]
        public async Task SyncMarketingPreferenceRollupAsync_WritesDerivedRollup()
        {
            var guest = await SeedLocationGuestAsync(
                LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

            _ledger.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await _context.SaveChangesAsync();

            var rollup = await _ledger.SyncMarketingPreferenceRollupAsync(guest.Id);
            await _context.SaveChangesAsync();

            Assert.Equal(LocationGuestMarketingPreference.Allowed, rollup);

            var reloaded = await _context.LocationGuests
                .AsNoTracking()
                .SingleAsync(lg => lg.Id == guest.Id);
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                reloaded.MarketingPreference
            );
        }

        [Fact]
        public async Task BackfillFromLegacyAllowed_MatchesMigrationMapping()
        {
            var guest = await SeedLocationGuestAsync(
                LocationGuestMarketingPreference.Allowed
            );
            var at = guest.CreatedAt;

            foreach (
                var (kind, eventKind) in
                LocationGuestPermissionMigrationMapping.LedgerEventsFromLegacyMarketingPreference(
                    LocationGuestMarketingPreference.Allowed
                )
            )
            {
                _ledger.RecordEvent(
                    guest.Id,
                    guest.RestaurantLocationId,
                    kind,
                    eventKind,
                    LocationGuestPermissionLedgerSources.LegacyMarketingPreference,
                    at
                );
            }

            await _context.SaveChangesAsync();

            var rollup = await _ledger.SyncMarketingPreferenceRollupAsync(guest.Id);
            var states = await _ledger.GetCurrentStatesAsync(guest.Id);

            Assert.Equal(LocationGuestMarketingPreference.Allowed, rollup);
            Assert.All(
                LocationGuestPermissionKindExtensions.All,
                kind => Assert.Equal(
                    LocationGuestPermissionState.Granted,
                    states[kind]
                )
            );
        }

        private async Task<LocationGuest> SeedLocationGuestAsync(
            LocationGuestMarketingPreference marketingPreference
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{Guid.NewGuid():N}@example.com",
                NormalizedEmail = $"guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(masterGuest);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = marketingPreference,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            return guest;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
