using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: durable Recovery offer attach on Feedback (ticket 02).
    /// Attach alone must not create OfferIssues.
    /// </summary>
    public class FeedbackRecoveryOfferAttachServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _offers;
        private readonly FeedbackRecoveryOfferAttachService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public FeedbackRecoveryOfferAttachServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _offers = new OffersCatalogService(_context, () => _now);
            _service = new FeedbackRecoveryOfferAttachService(_context, _offers);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetAsync_WhenUnset_ReturnsNull()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();

            var offerId = await _service.GetAsync(seeded.FeedbackId);

            Assert.Null(offerId);
        }

        [Fact]
        public async Task SetAsync_ActiveOffer_PersistsAndSurvivesReload()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();

            await _service.SetAsync(seeded.FeedbackId, seeded.CatalogOfferId);

            Assert.Equal(
                seeded.CatalogOfferId,
                await _service.GetAsync(seeded.FeedbackId)
            );

            var reloaded = await _context.Feedbacks
                .AsNoTracking()
                .FirstAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(seeded.CatalogOfferId, reloaded.RecoveryOfferId);
        }

        [Fact]
        public async Task SetAsync_Replace_UpdatesStoredOfferId()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();
            var second = await SeedActiveOfferAsync(seeded.LocationId);

            await _service.SetAsync(seeded.FeedbackId, seeded.CatalogOfferId);
            await _service.SetAsync(seeded.FeedbackId, second);

            Assert.Equal(second, await _service.GetAsync(seeded.FeedbackId));
        }

        [Fact]
        public async Task SetAsync_Null_ClearsStoredOfferId()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();

            await _service.SetAsync(seeded.FeedbackId, seeded.CatalogOfferId);
            await _service.SetAsync(seeded.FeedbackId, null);

            Assert.Null(await _service.GetAsync(seeded.FeedbackId));
        }

        [Fact]
        public async Task SetAsync_AttachAlone_DoesNotCreateOfferIssue()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();

            await _service.SetAsync(seeded.FeedbackId, seeded.CatalogOfferId);

            Assert.Equal(0, await _context.OfferIssues.CountAsync());
        }

        [Fact]
        public async Task SetAsync_InactiveOffer_Throws()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync(offerStatus: "paused");

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SetAsync(seeded.FeedbackId, seeded.CatalogOfferId)
            );

            Assert.Contains("Active", ex.Message, StringComparison.Ordinal);
            Assert.Null(
                (await _context.Feedbacks.FirstAsync(f => f.Id == seeded.FeedbackId))
                    .RecoveryOfferId
            );
        }

        [Fact]
        public async Task SetAsync_WrongLocationOffer_Throws()
        {
            var seeded = await SeedFeedbackAndActiveOfferAsync();
            var otherLocation = await SeedLocationAsync(seeded.RestaurantId);
            var otherOffer = await SeedActiveOfferAsync(otherLocation);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SetAsync(seeded.FeedbackId, otherOffer)
            );

            Assert.Contains("Active", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SetAsync_UnknownFeedback_Throws()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SetAsync(999_999, 1)
            );
        }

        private async Task<(
            int RestaurantId,
            int LocationId,
            int FeedbackId,
            int CatalogOfferId
        )> SeedFeedbackAndActiveOfferAsync(string offerStatus = "active")
        {
            var restaurant = new Restaurant
            {
                Name = "Attach Cafe",
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var locationId = await SeedLocationAsync(restaurant.Id);
            var offerId = await SeedActiveOfferAsync(locationId, offerStatus);

            var feedback = new Feedback
            {
                RestaurantLocationId = locationId,
                GuestName = "Guest",
                GuestContact = "guest@example.com",
                ContactType = ContactType.Email,
                Comment = "Cold food",
                CreatedAt = _now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (restaurant.Id, locationId, feedback.Id, offerId);
        }

        private async Task<int> SeedLocationAsync(int restaurantId)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = $"Loc-{Guid.NewGuid():N}"[..12],
                Address = "1 High Street",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedActiveOfferAsync(
            int locationId,
            string status = "active"
        )
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off",
                Description = "Come back",
                Validity = CatalogOfferValidity.Days30AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();
            return offer.Id;
        }
    }
}
