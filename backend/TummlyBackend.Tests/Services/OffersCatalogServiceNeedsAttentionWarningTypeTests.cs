using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Needs attention warning-type list scope (ticket offers-na-03).
    /// </summary>
    public class OffersCatalogServiceNeedsAttentionWarningTypeTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogServiceNeedsAttentionWarningTypeTests()
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
        public async Task ListAsync_NeedsAttentionExpiryScope_OmitsVoidOnly()
        {
            var seeded = await SeedNeedsAttentionSetAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "needs-attention",
                    AttentionWarningType = "expiry",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(2, response.TotalCount);
            Assert.Equal(3, response.TabCounts.NeedsAttention);
            Assert.Contains(
                seeded.ExpiringOfferId,
                response.Items.Select(item => item.Id)
            );
            Assert.Contains(
                seeded.DualRuleOfferId,
                response.Items.Select(item => item.Id)
            );
            Assert.DoesNotContain(
                seeded.VoidOnlyOfferId,
                response.Items.Select(item => item.Id)
            );
        }

        [Fact]
        public async Task ListAsync_NeedsAttentionVoidScope_OmitsExpiryOnly()
        {
            var seeded = await SeedNeedsAttentionSetAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "needs-attention",
                    AttentionWarningType = "void",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(2, response.TotalCount);
            Assert.Equal(3, response.TabCounts.NeedsAttention);
            Assert.Contains(
                seeded.VoidOnlyOfferId,
                response.Items.Select(item => item.Id)
            );
            Assert.Contains(
                seeded.DualRuleOfferId,
                response.Items.Select(item => item.Id)
            );
            Assert.DoesNotContain(
                seeded.ExpiringOfferId,
                response.Items.Select(item => item.Id)
            );
        }

        [Fact]
        public async Task ListAsync_WarningTypeIgnoredOutsideNeedsAttentionView()
        {
            var seeded = await SeedNeedsAttentionSetAsync();

            var response = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = seeded.LocationId,
                    View = "all",
                    AttentionWarningType = "expiry",
                    Page = 1,
                    PageSize = 25,
                }
            );

            Assert.Equal(3, response.TotalCount);
        }

        [Fact]
        public async Task ListAsync_InvalidWarningType_Throws()
        {
            var seeded = await SeedNeedsAttentionSetAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ListAsync(
                    new CatalogOffersListQuery
                    {
                        LocationId = seeded.LocationId,
                        View = "needs-attention",
                        AttentionWarningType = "unknown",
                        Page = 1,
                        PageSize = 25,
                    }
                )
            );
        }

        private async Task<(
            int LocationId,
            int ExpiringOfferId,
            int VoidOnlyOfferId,
            int DualRuleOfferId
        )> SeedNeedsAttentionSetAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Needs attention rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Scope loc",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var expiringOffer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Expiring soon",
                Description = "Expiry rule only",
                Validity = CatalogOfferValidity.ChooseExpiryDate,
                CustomExpiryDate = DateOnly.FromDateTime(_now.AddDays(3)),
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            var voidOnlyOffer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Void only",
                Description = "Open void only",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            var dualRuleOffer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Dual rule",
                Description = "Expiry and void",
                Validity = CatalogOfferValidity.ChooseExpiryDate,
                CustomExpiryDate = DateOnly.FromDateTime(_now.AddDays(5)),
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.AddRange(
                expiringOffer,
                voidOnlyOffer,
                dualRuleOffer
            );
            await _context.SaveChangesAsync();

            foreach (var offerId in new[]
            {
                expiringOffer.Id,
                voidOnlyOffer.Id,
                dualRuleOffer.Id,
            })
            {
                _context.Campaigns.Add(
                    new Campaign
                    {
                        RestaurantLocationId = location.Id,
                        Name = $"Campaign {offerId}",
                        Status = "draft",
                        OfferId = offerId,
                        CreatedAt = _now,
                        UpdatedAt = _now,
                    }
                );
            }

            var guest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "void-guest@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(guest);
            await _context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = guest.Id,
                Name = "Void guest",
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(locationGuest);
            await _context.SaveChangesAsync();

            async Task<int> SeedIssueAsync(int catalogOfferId)
            {
                var issue = new OfferIssue
                {
                    CatalogOfferId = catalogOfferId,
                    LocationGuestId = locationGuest.Id,
                    ClaimCode = Guid.NewGuid().ToString("N")[..8],
                    IssuedAtUtc = _now.AddDays(-2),
                    ClaimedAtUtc = _now.AddDays(-1),
                    RedeemedAtUtc = _now.AddHours(-6),
                    Source = "campaign",
                    ExpiryAtUtc = _now.AddDays(14),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "Test issue",
                    Description = "Void seed",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                };
                _context.OfferIssues.Add(issue);
                await _context.SaveChangesAsync();
                return issue.Id;
            }

            var voidOnlyIssueId = await SeedIssueAsync(voidOnlyOffer.Id);
            var dualRuleIssueId = await SeedIssueAsync(dualRuleOffer.Id);

            _context.OfferVoidRequests.AddRange(
                new OfferVoidRequest
                {
                    OfferIssueId = voidOnlyIssueId,
                    CatalogOfferId = voidOnlyOffer.Id,
                    RestaurantLocationId = location.Id,
                    RequestedByUserId = 1,
                    RequestedAtUtc = _now,
                    OriginalRedeemedAtUtc = _now.AddHours(-6),
                    ReasonId = OfferVoidRequestReasonIds.RedeemedByMistake,
                    CorrectionId = OfferVoidRequestCorrectionIds.KeepUnusable,
                    Status = OfferVoidRequestStatuses.Pending,
                },
                new OfferVoidRequest
                {
                    OfferIssueId = dualRuleIssueId,
                    CatalogOfferId = dualRuleOffer.Id,
                    RestaurantLocationId = location.Id,
                    RequestedByUserId = 1,
                    RequestedAtUtc = _now,
                    OriginalRedeemedAtUtc = _now.AddHours(-6),
                    ReasonId = OfferVoidRequestReasonIds.RedeemedByMistake,
                    CorrectionId = OfferVoidRequestCorrectionIds.KeepUnusable,
                    Status = OfferVoidRequestStatuses.Pending,
                }
            );
            await _context.SaveChangesAsync();

            return (
                location.Id,
                expiringOffer.Id,
                voidOnlyOffer.Id,
                dualRuleOffer.Id
            );
        }
    }
}
