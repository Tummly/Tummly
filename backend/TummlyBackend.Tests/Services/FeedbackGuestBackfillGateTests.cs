using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
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
                LinkToken = $"gate-linked-{Guid.NewGuid():N}"[..32],
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
                LinkToken = $"gate-unlnk-{Guid.NewGuid():N}"[..32],
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
                    OffersOptOut = offersOptOut,
                    CreatedAt = eventAt ?? DateTime.UtcNow,
                };
                _context.LocationGuests.Add(guest);
                await _context.SaveChangesAsync(cancellationToken);
                return guest;
            }
        }
    }
}
