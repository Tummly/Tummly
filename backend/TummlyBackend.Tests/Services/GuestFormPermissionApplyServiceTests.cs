using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="GuestFormPermissionApplyService"/> — stamps permission
    /// evidence on every Guest Form ledger event.
    /// </summary>
    public class GuestFormPermissionApplyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _ledger;
        private readonly GuestFormPermissionApplyService _apply;

        public GuestFormPermissionApplyServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _ledger = new LocationGuestPermissionLedgerService(_context);
            _apply = new GuestFormPermissionApplyService(_ledger);
        }

        [Fact]
        public async Task ApplyOnSubmitAsync_MarketingGrant_StampsConsentEvidence()
        {
            var (guest, restaurant) = await SeedGuestAsync(
                restaurantName: "Cafe",
                marketingPreference: LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

            await _apply.ApplyOnSubmitAsync(
                guest,
                restaurant,
                guest.RestaurantLocationId,
                marketingConsentGranted: true,
                ContactType.Email,
                at
            );

            var pending = PendingLedgerEntries();

            var emailGrant = Assert.Single(
                pending,
                e =>
                    e.PermissionKind == LocationGuestPermissionKind.EmailMarketing
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Grant
            );
            Assert.Equal(LocationGuestPermissionBases.Consent, emailGrant.Basis);
            Assert.Equal(
                GuestFormPermissionEvidence.GuestFormVersion,
                emailGrant.GuestFormVersion
            );
            Assert.Equal(
                GuestFormPermissionEvidence.EmailMarketingWordingVersion,
                emailGrant.WordingVersion
            );
            Assert.Equal(
                GuestFormPermissionEvidence.PrivacyNoticeVersion,
                emailGrant.PrivacyNoticeVersion
            );
            Assert.Contains("email me", emailGrant.WordingSnapshot);
            Assert.Equal(
                LocationGuestPermissionLedgerSources.GuestForm,
                emailGrant.Source
            );

            var followUpGrant = Assert.Single(
                pending,
                e =>
                    e.PermissionKind
                        == LocationGuestPermissionKind.FeedbackFollowUp
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Grant
            );
            Assert.Equal(
                LocationGuestPermissionBases.ServiceFollowUpNotice,
                followUpGrant.Basis
            );
            Assert.Equal(
                GuestFormPermissionEvidence.FeedbackFollowUpWordingVersion,
                followUpGrant.WordingVersion
            );
            Assert.Contains("shared privately", followUpGrant.WordingSnapshot);
        }

        [Fact]
        public async Task ApplyOnSubmitAsync_MarketingWithdraw_StampsNullBasis()
        {
            var (guest, restaurant) = await SeedGuestAsync(
                restaurantName: "Cafe",
                marketingPreference: LocationGuestMarketingPreference.NotRecorded
            );
            var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

            _ledger.RecordEvent(
                guest,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at.AddMinutes(-5)
            );
            await _context.SaveChangesAsync();

            await _apply.ApplyOnSubmitAsync(
                guest,
                restaurant,
                guest.RestaurantLocationId,
                marketingConsentGranted: false,
                ContactType.Email,
                at
            );

            var withdraw = Assert.Single(
                PendingLedgerEntries(),
                e =>
                    e.PermissionKind == LocationGuestPermissionKind.EmailMarketing
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Withdraw
            );
            Assert.Null(withdraw.Basis);
            Assert.Equal(
                GuestFormPermissionEvidence.GuestFormVersion,
                withdraw.GuestFormVersion
            );
            Assert.Equal(
                GuestFormPermissionEvidence.EmailMarketingWordingVersion,
                withdraw.WordingVersion
            );
            Assert.Equal(
                GuestFormPermissionEvidence.PrivacyNoticeVersion,
                withdraw.PrivacyNoticeVersion
            );
            Assert.False(string.IsNullOrWhiteSpace(withdraw.WordingSnapshot));
            Assert.Contains("email me", withdraw.WordingSnapshot);
        }

        private List<LocationGuestPermissionLedgerEntry> PendingLedgerEntries() =>
            _context.ChangeTracker
                .Entries<LocationGuestPermissionLedgerEntry>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

        private async Task<(LocationGuest Guest, Restaurant Restaurant)>
            SeedGuestAsync(
                string restaurantName,
                LocationGuestMarketingPreference marketingPreference
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
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = marketingPreference,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            return (guest, restaurant);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
