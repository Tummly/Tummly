using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationGuestActivityBackfillGateTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly LocationGuestActivityBackfillService _sut;

        public LocationGuestActivityBackfillGateTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _sut = new LocationGuestActivityBackfillService(_context);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task BackfillAsync_WhenAllKindsSatisfied_DoesNotAddEvents()
        {
            var seeded = await SeedGuestWithAllEventsAsync();
            var countBefore = await _context.LocationGuestActivityEvents.CountAsync();

            await _sut.BackfillAsync();

            Assert.Equal(
                countBefore,
                await _context.LocationGuestActivityEvents.CountAsync()
            );
            Assert.Equal(
                seeded.LocationGuestId,
                await _context.LocationGuestActivityEvents
                    .Where(e => e.Kind == LocationGuestActivityKinds.GuestJoined)
                    .Select(e => e.LocationGuestId)
                    .SingleAsync()
            );
        }

        [Fact]
        public async Task BackfillAsync_WhenMissingGuestJoined_BackfillsThatKind()
        {
            var guest = await SeedGuestWithoutEventsAsync();

            await _sut.BackfillAsync();

            Assert.True(
                await _context.LocationGuestActivityEvents.AnyAsync(e =>
                    e.LocationGuestId == guest.Id
                    && e.Kind == LocationGuestActivityKinds.GuestJoined
                )
            );
        }

        [Fact]
        public async Task BackfillAsync_SecondRun_IsIdempotent()
        {
            await SeedGuestWithoutEventsAsync();

            await _sut.BackfillAsync();
            var countAfterFirst = await _context.LocationGuestActivityEvents.CountAsync();

            await _sut.BackfillAsync();

            Assert.Equal(
                countAfterFirst,
                await _context.LocationGuestActivityEvents.CountAsync()
            );
        }

        [Fact]
        public async Task BackfillAsync_WhenTagAppliedCountsMatchButPairMissing_BackfillsMembership()
        {
            var guest = await SeedGuestWithoutEventsAsync();
            var tagA = new GuestTag
            {
                RestaurantId = guest.MasterGuest!.RestaurantId,
                DisplayName = "Alpha",
                NormalizedName = "alpha",
                AiSourced = false,
                CreatedAt = DateTime.UtcNow,
            };
            var tagB = new GuestTag
            {
                RestaurantId = guest.MasterGuest.RestaurantId,
                DisplayName = "Beta",
                NormalizedName = "beta",
                AiSourced = false,
                CreatedAt = DateTime.UtcNow,
            };
            _context.GuestTags.AddRange(tagA, tagB);
            await _context.SaveChangesAsync();

            _context.LocationGuestTags.Add(
                new LocationGuestTag
                {
                    LocationGuestId = guest.Id,
                    GuestTagId = tagA.Id,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            // Orphan TagApplied for tagB (no membership) + missing TagApplied for tagA:
            // counts are equal (1==1) but the membership pair is still unsatisfied.
            _context.LocationGuestActivityEvents.Add(
                new LocationGuestActivityEvent
                {
                    LocationGuestId = guest.Id,
                    Kind = LocationGuestActivityKinds.TagApplied,
                    OccurredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    PayloadJson = LocationGuestActivityPayload.Serialize(
                        new LocationGuestActivityPayload
                        {
                            GuestTagId = tagB.Id,
                            TagName = tagB.DisplayName,
                        }
                    ),
                }
            );
            await _context.SaveChangesAsync();

            await _sut.BackfillAsync();

            Assert.True(
                await _context.LocationGuestActivityEvents.AnyAsync(e =>
                    e.Kind == LocationGuestActivityKinds.TagApplied
                    && e.LocationGuestId == guest.Id
                    && e.PayloadJson != null
                    && e.PayloadJson.Contains($"\"guestTagId\":{tagA.Id}")
                )
            );
        }

        private async Task<(int LocationGuestId, int FeedbackId)> SeedGuestWithAllEventsAsync()
        {
            var guest = await SeedGuestWithoutEventsAsync();
            var feedback = new Feedback
            {
                RestaurantLocationId = guest.RestaurantLocationId,
                LocationGuestId = guest.Id,
                GuestName = guest.Name,
                GuestContact = "activity-gate@example.com",
                ContactType = ContactType.Email,
                Comment = "ok",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Positive,
                ClassificationClaimedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            var tag = new GuestTag
            {
                RestaurantId = guest.MasterGuest!.RestaurantId,
                DisplayName = "Vip",
                NormalizedName = "vip",
                AiSourced = false,
                CreatedAt = DateTime.UtcNow,
            };
            _context.GuestTags.Add(tag);
            await _context.SaveChangesAsync();

            var membership = new LocationGuestTag
            {
                LocationGuestId = guest.Id,
                GuestTagId = tag.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuestTags.Add(membership);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            _context.LocationGuestActivityEvents.AddRange(
                new LocationGuestActivityEvent
                {
                    LocationGuestId = guest.Id,
                    Kind = LocationGuestActivityKinds.GuestJoined,
                    OccurredAt = guest.CreatedAt,
                    CreatedAt = now,
                },
                new LocationGuestActivityEvent
                {
                    LocationGuestId = guest.Id,
                    FeedbackId = feedback.Id,
                    Kind = LocationGuestActivityKinds.Feedback,
                    OccurredAt = feedback.CreatedAt,
                    CreatedAt = now,
                },
                new LocationGuestActivityEvent
                {
                    LocationGuestId = guest.Id,
                    Kind = LocationGuestActivityKinds.TagApplied,
                    OccurredAt = membership.CreatedAt,
                    CreatedAt = now,
                    PayloadJson = LocationGuestActivityPayload.Serialize(
                        new LocationGuestActivityPayload
                        {
                            GuestTagId = tag.Id,
                            TagName = tag.DisplayName,
                        }
                    ),
                },
                new LocationGuestActivityEvent
                {
                    LocationGuestId = guest.Id,
                    FeedbackId = feedback.Id,
                    Kind = LocationGuestActivityKinds.ClassificationSucceeded,
                    OccurredAt = feedback.ClassificationClaimedAt!.Value,
                    CreatedAt = now,
                    PayloadJson = LocationGuestActivityPayload.Serialize(
                        new LocationGuestActivityPayload { Sentiment = "positive" }
                    ),
                }
            );
            await _context.SaveChangesAsync();

            return (guest.Id, feedback.Id);
        }

        private async Task<LocationGuest> SeedGuestWithoutEventsAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Activity Gate Bistro",
                AccountType = "Single",
                OwnerUserId = 4,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = $"act-gate-{Guid.NewGuid():N}"[..32],
                LocationName = "Main",
                Address = "4 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "activity-gate@example.com",
                NormalizedEmail = "activity-gate@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                MasterGuest = master,
                RestaurantLocationId = location.Id,
                Name = "Activity",
                CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest;
        }
    }
}
