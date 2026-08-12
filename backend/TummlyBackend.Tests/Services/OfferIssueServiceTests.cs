using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="OfferIssueService"/> — catalog Offer issue + MVP Claim
    /// (ticket 28 / 01 / 04).
    /// </summary>
    public class OfferIssueServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OfferIssueService _service;
        private readonly DateTime _now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

        public OfferIssueServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new OfferIssueService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task IssueOnCampaignAccepted_CreatesIssueWithClaimCodeAndClaimedAt()
        {
            var seeded = await SeedLocationGuestAndOfferAsync();

            var issue = await _service.IssueOnCampaignAcceptedAsync(
                campaignId: 42,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                channel: "email",
                _now
            );

            Assert.NotNull(issue);
            Assert.Matches(@"^TUM-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$", issue!.ClaimCode);
            Assert.Equal(_now, issue.IssuedAtUtc);
            Assert.Equal(_now, issue.ClaimedAtUtc);
            Assert.Equal(OfferIssueSources.Campaign, issue.Source);
            Assert.Equal(42, issue.CampaignId);
            Assert.Equal(seeded.CatalogOfferId, issue.CatalogOfferId);
            Assert.Equal(seeded.LocationGuestId, issue.LocationGuestId);
            Assert.Equal("10% off next visit", issue.Title);
            Assert.Equal(
                CatalogOfferMapping.ComputeExpiryAt(
                    CatalogOfferValidity.Days14AfterIssue,
                    _now,
                    null
                ),
                issue.ExpiryAtUtc
            );

            Assert.Equal(1, await _context.OfferIssues.CountAsync());
        }

        [Fact]
        public async Task IssueOnCampaignAccepted_SecondCallSameCampaignGuest_IsIdempotent()
        {
            var seeded = await SeedLocationGuestAndOfferAsync();

            var first = await _service.IssueOnCampaignAcceptedAsync(
                campaignId: 7,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                channel: "sms",
                _now
            );
            var second = await _service.IssueOnCampaignAcceptedAsync(
                campaignId: 7,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                channel: "sms",
                _now.AddMinutes(1)
            );

            Assert.NotNull(first);
            Assert.Null(second);
            Assert.Equal(1, await _context.OfferIssues.CountAsync());
        }

        [Fact]
        public async Task IssueOnCampaignAccepted_OptOut_SkipsIssue()
        {
            var seeded = await SeedLocationGuestAndOfferAsync(offersOptOut: true);

            var issue = await _service.IssueOnCampaignAcceptedAsync(
                campaignId: 3,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                channel: "email",
                _now
            );

            Assert.Null(issue);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
        }

        [Fact]
        public async Task IssueOnCampaignAccepted_InactiveOffer_SkipsIssue()
        {
            var seeded = await SeedLocationGuestAndOfferAsync(offerStatus: "paused");

            var issue = await _service.IssueOnCampaignAcceptedAsync(
                campaignId: 3,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                channel: "email",
                _now
            );

            Assert.Null(issue);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
        }

        [Fact]
        public async Task IssueOnThankYouSubmit_NoLiveAttach_IsNoOp()
        {
            var seeded = await SeedLocationGuestAndOfferAsync();

            var issue = await _service.IssueOnThankYouSubmitAsync(
                seeded.LocationId,
                seeded.LocationGuestId,
                feedbackId: 99,
                _now
            );

            Assert.Null(issue);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
        }

        [Fact]
        public async Task IssueOnThankYouSubmit_WhenPersistedActiveAttach_CreatesIssue()
        {
            var seeded = await SeedLocationGuestAndOfferAsync();
            await AttachThankYouAsync(seeded.LocationId, seeded.CatalogOfferId);

            var issue = await _service.IssueOnThankYouSubmitAsync(
                seeded.LocationId,
                seeded.LocationGuestId,
                feedbackId: 55,
                _now
            );

            Assert.NotNull(issue);
            Assert.Equal(OfferIssueSources.GuestFormThankYou, issue!.Source);
            Assert.Equal(55, issue.FeedbackId);
            Assert.Null(issue.CampaignId);
            Assert.Equal(_now, issue.ClaimedAtUtc);
            Assert.Equal(1, await _context.OfferIssues.CountAsync());
        }

        [Fact]
        public async Task IssueOnThankYouSubmit_WhenPersistedAttachPaused_IsNoOp()
        {
            var seeded = await SeedLocationGuestAndOfferAsync(
                offerStatus: "paused"
            );
            await AttachThankYouAsync(seeded.LocationId, seeded.CatalogOfferId);

            var issue = await _service.IssueOnThankYouSubmitAsync(
                seeded.LocationId,
                seeded.LocationGuestId,
                feedbackId: 55,
                _now
            );

            Assert.Null(issue);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
        }

        private async Task AttachThankYouAsync(int locationId, int offerId)
        {
            var location = await _context.RestaurantLocations
                .FirstAsync(row => row.Id == locationId);
            location.ThankYouCatalogOfferId = offerId;
            await _context.SaveChangesAsync();
        }

        private async Task<(
            int LocationId,
            int LocationGuestId,
            int CatalogOfferId
        )> SeedLocationGuestAndOfferAsync(
            bool offersOptOut = false,
            string offerStatus = "active"
        )
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Offer Issue Restaurant",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                OffersOptOut = offersOptOut,
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(lg);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = offerStatus,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Come back soon",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            return (location.Id, lg.Id, offer.Id);
        }
    }
}
