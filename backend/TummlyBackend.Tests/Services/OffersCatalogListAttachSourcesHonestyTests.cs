using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: list/Details attachKinds + attach-source filters stay honest for
    /// Recovery and Guest form thank-you (ticket 10).
    /// </summary>
    public class OffersCatalogListAttachSourcesHonestyTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogListAttachSourcesHonestyTests()
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
        public async Task ListAsync_AttachSourceGuestFormThankYou_MatchesThankYouOnly()
        {
            var seeded = await SeedOfferAsync(CatalogOfferStatus.Active);
            await AttachThankYouAsync(seeded.LocationId, seeded.OfferId);

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    AttachSource = new[]
                    {
                        CatalogOfferStatus.AttachSourceGuestFormThankYou,
                    },
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Single(response.Items);
            Assert.Equal(seeded.OfferId, response.Items[0].Id);
            Assert.Equal(
                new[] { CatalogOfferStatus.AttachSourceGuestFormThankYou },
                response.Items[0].AttachKinds
            );
            Assert.DoesNotContain(
                CatalogOfferStatus.AttachKindCampaign,
                response.Items[0].AttachKinds
            );
        }

        [Fact]
        public async Task GetByIdAsync_ThankYouOnly_AttachKindsHonest()
        {
            var seeded = await SeedOfferAsync(CatalogOfferStatus.Active);
            await AttachThankYouAsync(seeded.LocationId, seeded.OfferId);

            var dto = await _service.GetByIdAsync(seeded.OfferId);

            Assert.NotNull(dto);
            Assert.Equal(
                new[] { CatalogOfferStatus.AttachSourceGuestFormThankYou },
                dto!.AttachKinds
            );
            Assert.DoesNotContain(
                CatalogOfferStatus.AttachKindCampaign,
                dto.AttachKinds
            );
            Assert.DoesNotContain(
                CatalogOfferStatus.AttachSourceRecovery,
                dto.AttachKinds
            );
        }

        [Fact]
        public async Task ListAsync_ClearThankYouAttach_MovesActiveToDrafts()
        {
            var seeded = await SeedOfferAsync(CatalogOfferStatus.Active);
            await AttachThankYouAsync(seeded.LocationId, seeded.OfferId);

            var inFlightBefore = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );
            Assert.Equal(1, inFlightBefore.TabCounts.InFlight);
            Assert.Single(inFlightBefore.Items);

            var location = await _context.RestaurantLocations
                .FirstAsync(row => row.Id == seeded.LocationId);
            location.ThankYouCatalogOfferId = null;
            await _context.SaveChangesAsync();
            await _service.SyncInFlightStoredStatusAsync(seeded.OfferId);

            var inFlightAfter = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );
            Assert.Equal(0, inFlightAfter.TabCounts.InFlight);
            Assert.Empty(inFlightAfter.Items);

            var drafts = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "drafts",
                    Page = 1,
                    PageSize = 25,
                }
            );
            Assert.Equal(1, drafts.TabCounts.Drafts);
            Assert.Single(drafts.Items);
            Assert.Equal(seeded.OfferId, drafts.Items[0].Id);
            Assert.Equal(CatalogOfferStatus.Draft, drafts.Items[0].Status);
        }

        [Fact]
        public async Task ListAsync_RecoveryOnly_AttachKindsNoCampaign()
        {
            var seeded = await SeedOfferWithRecoveryAsync();

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
            Assert.Equal(
                new[] { CatalogOfferStatus.AttachSourceRecovery },
                response.Items[0].AttachKinds
            );
            Assert.DoesNotContain(
                CatalogOfferStatus.AttachKindCampaign,
                response.Items[0].AttachKinds
            );
        }

        [Fact]
        public async Task GetByIdAsync_RecoveryOnly_AttachKindsNoCampaign()
        {
            var seeded = await SeedOfferWithRecoveryAsync();

            var dto = await _service.GetByIdAsync(seeded.OfferId);

            Assert.NotNull(dto);
            Assert.Equal(
                new[] { CatalogOfferStatus.AttachSourceRecovery },
                dto!.AttachKinds
            );
            Assert.DoesNotContain(
                CatalogOfferStatus.AttachKindCampaign,
                dto.AttachKinds
            );
        }

        [Fact]
        public async Task ListAsync_PausedWithCampaign_AttachSourceCampaign_DoesNotMatch()
        {
            var seeded = await SeedOfferAsync(CatalogOfferStatus.Paused);
            _context.Campaigns.Add(
                new Campaign
                {
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Closed campaign",
                    Status = "sent",
                    OfferId = seeded.OfferId,
                    CreatedAt = _now,
                    UpdatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    AttachSource = new[]
                    {
                        CatalogOfferStatus.AttachSourceCampaign,
                    },
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Empty(response.Items);
            Assert.Equal(0, response.TabCounts.InFlight);
        }

        [Fact]
        public async Task ListAsync_PausedWithRecovery_AttachSourceRecovery_DoesNotMatch()
        {
            var seeded = await SeedOfferWithRecoveryAsync(
                offerStatus: CatalogOfferStatus.Paused
            );

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    AttachSource = new[]
                    {
                        CatalogOfferStatus.AttachSourceRecovery,
                    },
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Empty(response.Items);
        }

        [Fact]
        public async Task GetByIdAsync_PausedWithRecovery_LiveAttachKindsEmpty()
        {
            var seeded = await SeedOfferWithRecoveryAsync(
                offerStatus: CatalogOfferStatus.Paused
            );

            var dto = await _service.GetByIdAsync(seeded.OfferId);

            Assert.NotNull(dto);
            Assert.Empty(dto!.AttachKinds);
        }

        [Fact]
        public async Task ListAsync_ClearRecoveryAttach_MovesActiveToDrafts()
        {
            var seeded = await SeedOfferWithRecoveryAsync();

            var inFlightBefore = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "in-flight",
                    Page = 1,
                    PageSize = 25,
                }
            );
            Assert.Equal(1, inFlightBefore.TabCounts.InFlight);

            var feedback = await _context.Feedbacks
                .FirstAsync(row => row.RecoveryOfferId == seeded.OfferId);
            feedback.RecoveryOfferId = null;
            await _context.SaveChangesAsync();
            await _service.SyncInFlightStoredStatusAsync(seeded.OfferId);

            var after = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "drafts",
                    Page = 1,
                    PageSize = 25,
                }
            );
            Assert.Equal(0, after.TabCounts.InFlight);
            Assert.Equal(1, after.TabCounts.Drafts);
            Assert.Single(after.Items);
            Assert.Equal(seeded.OfferId, after.Items[0].Id);
            Assert.Equal(CatalogOfferStatus.Draft, after.Items[0].Status);
        }

        private async Task AttachThankYouAsync(int locationId, int offerId)
        {
            var location = await _context.RestaurantLocations
                .FirstAsync(row => row.Id == locationId);
            location.ThankYouCatalogOfferId = offerId;
            await _context.SaveChangesAsync();
        }

        private async Task<(int LocationId, int OfferId)> SeedOfferAsync(
            string status
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Honesty Rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Honesty Loc",
                Address = "2 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Honesty attach",
                Description = "Ticket 10",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            return (location.Id, offer.Id);
        }

        private async Task<(int LocationId, int OfferId)> SeedOfferWithRecoveryAsync(
            string offerStatus = CatalogOfferStatus.Active
        )
        {
            var seeded = await SeedOfferAsync(offerStatus);
            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    GuestName = "Guest",
                    GuestContact = "g@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Cold",
                    CreatedAt = _now,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Negative,
                    DetectedTagsJson = "[]",
                    WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                    RecoveryOfferId = seeded.OfferId,
                }
            );
            await _context.SaveChangesAsync();
            return seeded;
        }
    }
}
