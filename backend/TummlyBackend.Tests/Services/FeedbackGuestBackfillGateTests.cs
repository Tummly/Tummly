using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class FeedbackGuestBackfillGateTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CountingGuestUpsert _upsert;
        private readonly FeedbackGuestBackfillService _sut;

        public FeedbackGuestBackfillGateTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _upsert = new CountingGuestUpsert(_context);
            _sut = new FeedbackGuestBackfillService(_context, _upsert);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task BackfillAsync_WhenNoUnlinkedFeedback_SkipsHeavyWork()
        {
            var seeded = await SeedLinkedFeedbackAsync();

            await _sut.BackfillAsync();

            Assert.Equal(0, _upsert.CallCount);
            Assert.Equal(
                seeded.LocationGuestId,
                await _context.Feedbacks
                    .Where(f => f.Id == seeded.FeedbackId)
                    .Select(f => f.LocationGuestId)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task BackfillAsync_WhenUnlinkedExists_RunsCatchUp()
        {
            await SeedUnlinkedFeedbackAsync();

            await _sut.BackfillAsync();

            Assert.True(_upsert.CallCount >= 1);
            Assert.Equal(
                0,
                await _context.Feedbacks.CountAsync(f => f.LocationGuestId == null)
            );
        }

        [Fact]
        public async Task BackfillAsync_WhenOnlyErasureShapedUnlinked_SkipsUpsert()
        {
            await SeedErasureShapedUnlinkedFeedbackAsync(guestContact: "");
            await SeedErasureShapedUnlinkedFeedbackAsync(guestContact: "   ");

            await _sut.BackfillAsync();

            Assert.Equal(0, _upsert.CallCount);
            Assert.Equal(
                2,
                await _context.Feedbacks.CountAsync(f => f.LocationGuestId == null)
            );
            Assert.Equal(0, await _context.MasterGuests.CountAsync());
            Assert.Equal(0, await _context.LocationGuests.CountAsync());
        }

        [Fact]
        public async Task BackfillAsync_AfterGuestDelete_KeepsFeedbackUnlinked_NoPhantomGuests()
        {
            var hasher = new GuestContactHasher("test-key");
            var ownedLocation = new OwnedLocationService(_context);
            var audit = new AdminAuditService(_context, TimeProvider.System);
            var suppressions = new GuestContactSuppressionService(
                _context,
                hasher
            );
            var delete = new LocationGuestDeleteService(
                _context,
                ownedLocation,
                audit,
                suppressions
            );
            var recorder = new LocationGuestActivityRecorder(_context);
            var ledger = new LocationGuestPermissionLedgerService(_context);
            var guestUpsert = new GuestUpsertService(
                _context,
                recorder,
                suppressions,
                ledger
            );
            var backfill = new FeedbackGuestBackfillService(
                _context,
                guestUpsert
            );

            var seeded = await SeedOwnedGuestWithFeedbackAsync();
            var outcome = await delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );
            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var mastersAfterDelete = await _context.MasterGuests.CountAsync(
                g => g.RestaurantId == seeded.RestaurantId
            );
            var guestsAfterDelete = await _context.LocationGuests.CountAsync(
                lg => lg.RestaurantLocationId == seeded.LocationId
            );
            Assert.Equal(0, mastersAfterDelete);
            Assert.Equal(0, guestsAfterDelete);

            await backfill.BackfillAsync();

            var feedback = await _context.Feedbacks.SingleAsync(
                f => f.Id == seeded.FeedbackId
            );
            Assert.Null(feedback.LocationGuestId);
            Assert.Equal(DeletedGuestDisplay.Name, feedback.GuestName);
            Assert.Equal(string.Empty, feedback.GuestContact);
            Assert.Equal(ContactType.Unknown, feedback.ContactType);
            Assert.Equal(
                0,
                await _context.MasterGuests.CountAsync(
                    g => g.RestaurantId == seeded.RestaurantId
                )
            );
            Assert.Equal(
                0,
                await _context.LocationGuests.CountAsync(
                    lg => lg.RestaurantLocationId == seeded.LocationId
                )
            );
        }

        [Fact]
        public async Task BackfillAsync_SecondRun_IsIdempotentAndSkipsUpsert()
        {
            await SeedUnlinkedFeedbackAsync();

            await _sut.BackfillAsync();
            var callsAfterFirst = _upsert.CallCount;
            Assert.True(callsAfterFirst >= 1);

            await _sut.BackfillAsync();

            Assert.Equal(callsAfterFirst, _upsert.CallCount);
        }

        private async Task<(int FeedbackId, int LocationGuestId)> SeedLinkedFeedbackAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Gate Bistro",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "linked@example.com",
                NormalizedEmail = "linked@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Linked",
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = guest.Id,
                GuestName = "Linked",
                GuestContact = "linked@example.com",
                ContactType = ContactType.Email,
                Comment = "Already linked",
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (feedback.Id, guest.Id);
        }

        private async Task SeedUnlinkedFeedbackAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Unlinked Bistro",
                AccountType = "Single",
                OwnerUserId = 2,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "2 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    LocationGuestId = null,
                    GuestName = "Unlinked",
                    GuestContact = "unlinked@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Needs link",
                    ClassificationStatus = ClassificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task SeedErasureShapedUnlinkedFeedbackAsync(
            string guestContact
        )
        {
            var restaurant = new Restaurant
            {
                Name = $"Erasure Bistro {Guid.NewGuid():N}",
                AccountType = "Single",
                OwnerUserId = 3,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "3 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    LocationGuestId = null,
                    GuestName = DeletedGuestDisplay.Name,
                    GuestContact = guestContact,
                    ContactType = ContactType.Unknown,
                    Comment = "Kept after erasure",
                    ClassificationStatus = ClassificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<(
            int OwnerUserId,
            int RestaurantId,
            int LocationId,
            int LocationGuestId,
            int FeedbackId
        )> SeedOwnedGuestWithFeedbackAsync()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-backfill-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Delete Then Backfill Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "4 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "pat-backfill@example.com",
                NormalizedEmail = "pat-backfill@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Pat",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = guest.Id,
                GuestName = "Pat",
                GuestContact = "pat-backfill@example.com",
                ContactType = ContactType.Email,
                Comment = "Will be anonymised",
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (
                user.Id,
                restaurant.Id,
                location.Id,
                guest.Id,
                feedback.Id
            );
        }

        private sealed class CountingGuestUpsert : IGuestUpsertService
        {
            private readonly ApplicationDbContext _context;

            public CountingGuestUpsert(ApplicationDbContext context)
            {
                _context = context;
            }

            public int CallCount { get; private set; }

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
                CallCount++;

                var master = new MasterGuest
                {
                    RestaurantId = restaurantId,
                    Email = guestContact,
                    NormalizedEmail = guestContact.ToLowerInvariant(),
                    CreatedAt = eventAt ?? DateTime.UtcNow,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync(cancellationToken);

                var guest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = restaurantLocationId,
                    Name = guestName,
                    MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(offersOptOut),

                    CreatedAt = eventAt ?? DateTime.UtcNow,
                };
                _context.LocationGuests.Add(guest);
                await _context.SaveChangesAsync(cancellationToken);
                return guest;
            }
        }
    }
}
