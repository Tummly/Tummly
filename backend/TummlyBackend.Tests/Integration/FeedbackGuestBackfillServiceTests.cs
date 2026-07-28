using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackGuestBackfillServiceTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public FeedbackGuestBackfillServiceTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task BackfillAsync_LinksUnlinkedFeedbacks_UsingHistoricalCreatedAtAndLatestWins()
        {
            var seeded = await SeedUnlinkedFeedbacksAsync(
                firstCreatedAt: new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc),
                secondCreatedAt: new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc)
            );

            await RunBackfillAsync();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var feedbacks = await context.Feedbacks
                .Where(f => f.RestaurantLocationId == seeded.LocationId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();

            Assert.All(feedbacks, f => Assert.NotNull(f.LocationGuestId));

            var locationGuest = await context.LocationGuests
                .Include(lg => lg.MasterGuest)
                .SingleAsync(lg => lg.RestaurantLocationId == seeded.LocationId);

            Assert.Equal(seeded.FirstCreatedAt, locationGuest.CreatedAt);
            Assert.Equal("Jane D.", locationGuest.Name);
            Assert.False(locationGuest.OffersOptOut);
            Assert.Equal("jane@example.com", locationGuest.MasterGuest!.Email);
        }

        [Fact]
        public async Task BackfillAsync_SkipsAlreadyLinkedFeedbacks()
        {
            var seeded = await SeedUnlinkedFeedbacksAsync(
                firstCreatedAt: new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                secondCreatedAt: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc)
            );

            int linkedGuestId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var guestUpsert = scope.ServiceProvider
                    .GetRequiredService<IGuestUpsertService>();

                var firstFeedback = await context.Feedbacks
                    .SingleAsync(f => f.Id == seeded.FirstFeedbackId);
                var secondFeedback = await context.Feedbacks
                    .SingleAsync(f => f.Id == seeded.SecondFeedbackId);

                var linkedGuest = await guestUpsert.ResolveOrCreateAsync(
                    seeded.RestaurantId,
                    seeded.LocationId,
                    firstFeedback.GuestName,
                    firstFeedback.GuestContact,
                    firstFeedback.ContactType,
                    firstFeedback.OffersOptOut,
                    firstFeedback.CreatedAt
                );
                linkedGuest.Name = "Pre-linked name";
                linkedGuest.OffersOptOut = true;
                firstFeedback.LocationGuest = linkedGuest;
                secondFeedback.LocationGuest = linkedGuest;
                await context.SaveChangesAsync();
                linkedGuestId = linkedGuest.Id;
            }

            await RunBackfillAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var locationGuests = await context.LocationGuests
                    .Where(lg => lg.RestaurantLocationId == seeded.LocationId)
                    .ToListAsync();

                Assert.Single(locationGuests);
                Assert.Equal(linkedGuestId, locationGuests[0].Id);
                Assert.Equal("Pre-linked name", locationGuests[0].Name);
                Assert.True(locationGuests[0].OffersOptOut);

                var feedbacks = await context.Feedbacks
                    .Where(f => f.RestaurantLocationId == seeded.LocationId)
                    .ToListAsync();
                Assert.All(feedbacks, f => Assert.Equal(linkedGuestId, f.LocationGuestId));
            }
        }

        [Fact]
        public async Task BackfillAsync_IsIdempotentOnSecondRun()
        {
            var seeded = await SeedUnlinkedFeedbacksAsync(
                firstCreatedAt: new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                secondCreatedAt: new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
            );

            await RunBackfillAsync();
            await RunBackfillAsync();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            Assert.Equal(
                1,
                await context.MasterGuests.CountAsync(
                    g => g.RestaurantId == seeded.RestaurantId
                )
            );
            Assert.Equal(
                1,
                await context.LocationGuests.CountAsync(
                    lg => lg.RestaurantLocationId == seeded.LocationId
                )
            );
            Assert.Equal(
                0,
                await context.Feedbacks.CountAsync(
                    f =>
                        f.RestaurantLocationId == seeded.LocationId
                        && f.LocationGuestId == null
                )
            );
        }

        private async Task RunBackfillAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var backfill = scope.ServiceProvider
                .GetRequiredService<IFeedbackGuestBackfillService>();
            await backfill.BackfillAsync();
        }

        private async Task<(
            int RestaurantId,
            int LocationId,
            int FirstFeedbackId,
            int SecondFeedbackId,
            DateTime FirstCreatedAt
        )> SeedUnlinkedFeedbacksAsync(
            DateTime firstCreatedAt,
            DateTime secondCreatedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var restaurant = new Restaurant
            {
                Name = "Backfill Bistro",
                AccountType = "Single",
                OwnerUserId = 888_001,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var firstFeedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Jane Doe",
                GuestContact = "jane@example.com",
                ContactType = ContactType.Email,
                Comment = "First visit.",
                OffersOptOut = true,
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = firstCreatedAt,
            };
            var secondFeedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Jane D.",
                GuestContact = "jane@example.com",
                ContactType = ContactType.Email,
                Comment = "Second visit.",
                OffersOptOut = false,
                ClassificationStatus = ClassificationStatus.Pending,
                CreatedAt = secondCreatedAt,
            };

            context.Feedbacks.AddRange(firstFeedback, secondFeedback);
            await context.SaveChangesAsync();

            return (
                restaurant.Id,
                location.Id,
                firstFeedback.Id,
                secondFeedback.Id,
                firstCreatedAt
            );
        }
    }
}
