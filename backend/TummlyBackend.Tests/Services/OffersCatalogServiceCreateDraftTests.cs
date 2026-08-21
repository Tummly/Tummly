using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: stored-Draft create vs Active create on OffersCatalogService
    /// (ai-assistant-drafts issue 18).
    /// </summary>
    public class OffersCatalogServiceCreateDraftTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OffersCatalogService _service;
        private readonly DateTime _now = new(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);

        public OffersCatalogServiceCreateDraftTests()
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
        public async Task CreateDraftAsync_PersistsStoredDraft_Attachable()
        {
            var locationId = await SeedLocationAsync();
            var request = SampleCreateRequest(locationId, "Draft lunch deal");

            var dto = await _service.CreateDraftAsync(request, createdByUserId: 1);

            Assert.Equal(CatalogOfferStatus.Draft, dto.Status);
            Assert.Equal("Draft lunch deal", dto.Title);

            var entity = await _context.CatalogOffers.SingleAsync();
            Assert.Equal(CatalogOfferStatus.Draft, entity.Status);

            var attachable = await _service.IsAttachableForLocationAsync(
                dto.Id,
                locationId
            );
            Assert.True(attachable);
        }

        [Fact]
        public async Task CreateActiveAsync_PersistsDraftUntilLiveAttach()
        {
            var locationId = await SeedLocationAsync();
            var request = SampleCreateRequest(locationId, "Active lunch deal");

            var dto = await _service.CreateActiveAsync(request, createdByUserId: 1);

            Assert.Equal(CatalogOfferStatus.Draft, dto.Status);

            var attachable = await _service.IsAttachableForLocationAsync(
                dto.Id,
                locationId
            );
            Assert.True(attachable);
        }

        [Fact]
        public async Task CreateDraftAsync_AppearsOnDraftsListView()
        {
            var locationId = await SeedLocationAsync();
            var request = SampleCreateRequest(locationId, "Stored draft row");

            var dto = await _service.CreateDraftAsync(request);

            var list = await _service.ListAsync(
                new CatalogOffersListQuery
                {
                    LocationId = locationId,
                    View = "drafts",
                    Sort = "recent-activity",
                    Page = 1,
                    PageSize = 25,
                    UtcOffsetMinutes = 0,
                }
            );

            Assert.Equal(1, list.TotalCount);
            Assert.Equal(1, list.TabCounts.Drafts);
            var item = Assert.Single(list.Items);
            Assert.Equal(dto.Id, item.Id);
            Assert.Equal(CatalogOfferStatus.Draft, item.Status);
        }

        [Fact]
        public async Task CreateActiveAsync_FreeItem_PersistsAdditionalExclusionsWithoutMinimumSpend()
        {
            var locationId = await SeedLocationAsync();
            var request = new CreateCatalogOfferRequest
            {
                LocationId = locationId,
                OfferType = "free_item",
                Title = "Enjoy a free side",
                Description = "One free side with your visit.",
                Validity = "7_days_after_issue",
                FreeItemText = "side",
                PurchaseRequirement = "no_purchase_required",
                AdditionalExclusions = "Not valid on weekends",
            };

            var dto = await _service.CreateActiveAsync(request, createdByUserId: 1);

            Assert.Equal("free_item", dto.OfferType);
            Assert.Equal("side", dto.FreeItemText);
            Assert.Equal("no_purchase_required", dto.PurchaseRequirement);
            Assert.Null(dto.MinimumSpend);
            Assert.Equal("Not valid on weekends", dto.AdditionalExclusions);

            var entity = await _context.CatalogOffers.SingleAsync();
            Assert.Equal("Not valid on weekends", entity.AdditionalExclusions);
        }

        private static CreateCatalogOfferRequest SampleCreateRequest(
            int locationId,
            string title
        ) =>
            new()
            {
                LocationId = locationId,
                OfferType = "percentage_discount",
                Title = title,
                Description = "A stored Offers catalog definition.",
                Validity = "7_days_after_issue",
                DiscountPercentage = 10m,
            };

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Draft Create Rest",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Draft Create Loc",
                Address = "1 High St",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}
