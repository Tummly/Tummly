using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: Recovery live attach counts toward Offers list In flight +
    /// attach-source recovery (ticket 02).
    /// </summary>
    public class OffersCatalogServiceRecoveryAttachTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogServiceRecoveryAttachTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new OffersCatalogService(_context, () => _now);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task ListAsync_RecoveryAttach_CountsInFlightAndAttachKinds()
        {
            var seeded = await SeedActiveOfferWithRecoveryAttachAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(1, response.TabCounts.InFlight);
            Assert.Single(response.Items);
            Assert.Equal(seeded.CatalogOfferId, response.Items[0].Id);
            Assert.Contains(
                CatalogOfferStatus.AttachSourceRecovery,
                response.Items[0].AttachKinds
            );
        }

        [Fact]
        public async Task ListAsync_AttachSourceRecovery_MatchesRecoveryAttach()
        {
            var seeded = await SeedActiveOfferWithRecoveryAttachAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    AttachSource = new[] { CatalogOfferStatus.AttachSourceRecovery },
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Single(response.Items);
            Assert.Equal(seeded.CatalogOfferId, response.Items[0].Id);
        }

        [Fact]
        public async Task ListAsync_CampaignAndRecovery_CombineLiveAttachCount()
        {
            var seeded = await SeedActiveOfferWithRecoveryAttachAsync();
            _context.Campaigns.Add(
                new Campaign
                {
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Summer",
                    Status = "draft",
                    OfferId = seeded.CatalogOfferId,
                    CreatedAt = _now,
                    UpdatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Single(response.Items);
            Assert.Contains(
                CatalogOfferStatus.AttachKindCampaign,
                response.Items[0].AttachKinds
            );
            Assert.Contains(
                CatalogOfferStatus.AttachSourceRecovery,
                response.Items[0].AttachKinds
            );
        }

        [Fact]
        public async Task ListAsync_PausedOffer_RecoveryAttach_NotInFlight()
        {
            var seeded = await SeedActiveOfferWithRecoveryAttachAsync(
                offerStatus: CatalogOfferStatus.Paused
            );

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(0, response.TabCounts.InFlight);
            Assert.Empty(response.Items);
        }

        [Fact]
        public async Task GetByIdAsync_RecoveryAttach_IncludesAttachKind()
        {
            var seeded = await SeedActiveOfferWithRecoveryAttachAsync();

            var dto = await _service.GetByIdAsync(seeded.CatalogOfferId);

            Assert.NotNull(dto);
            Assert.Contains(
                CatalogOfferStatus.AttachSourceRecovery,
                dto!.AttachKinds
            );
        }

        private async Task<(
            int LocationId,
            int CatalogOfferId
        )> SeedActiveOfferWithRecoveryAttachAsync(
            string offerStatus = CatalogOfferStatus.Active
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Catalog Cafe",
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "12 High Street",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = offerStatus,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Recovery 15%",
                Description = "Sorry",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 15m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Guest",
                GuestContact = "g@example.com",
                ContactType = ContactType.Email,
                Comment = "Cold",
                CreatedAt = _now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                RecoveryOfferId = offer.Id,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (location.Id, offer.Id);
        }
    }
}
