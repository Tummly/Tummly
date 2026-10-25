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
        public async Task SyncMarketingPreferenceRollupAsync_IncludesPendingLedgerEvents()
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

            var rollup = await _ledger.SyncMarketingPreferenceRollupAsync(guest.Id);

            Assert.Equal(LocationGuestMarketingPreference.Allowed, rollup);
            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                guest.MarketingPreference
            );
        }

        [Fact]
        public async Task RecordEvent_UnsavedLocationGuest_WiresNavigationNotOrphanFk()
        {
            // SQL Server FK fails when ledger rows use LocationGuestId=0
            // before the guest insert (guest-form submit for a new guest).
            var (guest, restaurant) = await SeedUnsavedLocationGuestAsync();
            var apply = new GuestFormPermissionApplyService(_ledger);
            var at = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

            await apply.ApplyOnSubmitAsync(
                guest,
                restaurant,
                guest.RestaurantLocationId,
                locationName: "Camden",
                marketingConsentGranted: true,
                ContactType.Email,
                at
            );

            var pending = _context.ChangeTracker
                .Entries<LocationGuestPermissionLedgerEntry>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            Assert.NotEmpty(pending);
            Assert.All(
                pending,
                entry => Assert.Same(guest, entry.LocationGuest)
            );

            await _context.SaveChangesAsync();

            Assert.NotEqual(0, guest.Id);
            Assert.All(
                pending,
                entry => Assert.Equal(guest.Id, entry.LocationGuestId)
            );
        }

        [Fact]
        public async Task RecordEvent_WithEvidence_PersistsBasisVersionsAndSnapshot()
        {
            var guest = await SeedLocationGuestAsync(
                LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
            var evidence = new PermissionLedgerEvidence(
                Basis: LocationGuestPermissionBases.Consent,
                GuestFormVersion: "guest-form-v1",
                WordingVersion: "email-marketing-v1",
                PrivacyNoticeVersion: "privacy-notice-v1",
                WordingSnapshot: "Yes, email me occasional offers and updates from Cafe."
            );

            _ledger.RecordEvent(
                guest,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at,
                evidence
            );
            await _context.SaveChangesAsync();

            var row = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(LocationGuestPermissionBases.Consent, row.Basis);
            Assert.Equal("guest-form-v1", row.GuestFormVersion);
            Assert.Equal("email-marketing-v1", row.WordingVersion);
            Assert.Equal("privacy-notice-v1", row.PrivacyNoticeVersion);
            Assert.Equal(evidence.WordingSnapshot, row.WordingSnapshot);
        }

        [Fact]
        public async Task RecordEvent_WithoutEvidence_LeavesEvidenceColumnsNull()
        {
            var guest = await SeedLocationGuestAsync(
                LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

            _ledger.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await _context.SaveChangesAsync();

            var row = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .SingleAsync();
            Assert.Null(row.Basis);
            Assert.Null(row.GuestFormVersion);
            Assert.Null(row.WordingVersion);
            Assert.Null(row.PrivacyNoticeVersion);
            Assert.Null(row.WordingSnapshot);
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
            var (guest, _) = await SeedUnsavedLocationGuestAsync(
                marketingPreference,
                persistGuest: true
            );
            return guest;
        }

        private async Task<(LocationGuest Guest, Restaurant Restaurant)>
            SeedUnsavedLocationGuestAsync(
                LocationGuestMarketingPreference marketingPreference =
                    LocationGuestMarketingPreference.NotRecorded,
                bool persistGuest = false
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
            if (persistGuest)
            {
                await _context.SaveChangesAsync();
            }

            var guest = new LocationGuest
            {
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = marketingPreference,
                CreatedAt = DateTime.UtcNow,
            };
            if (persistGuest)
            {
                guest.MasterGuestId = masterGuest.Id;
            }
            _context.LocationGuests.Add(guest);
            if (persistGuest)
            {
                await _context.SaveChangesAsync();
            }

            return (guest, restaurant);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
