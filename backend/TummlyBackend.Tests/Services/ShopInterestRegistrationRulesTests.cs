using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class ShopInterestRegistrationRulesTests : IDisposable
    {
        private const int UserId = 41;
        private const int LocationId = 7;
        private const int RestaurantId = 3;
        private const string Email = "jane.doe@example.com";

        private readonly ApplicationDbContext _context;
        private readonly ShopInterestService _service;

        public ShopInterestRegistrationRulesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _service = new ShopInterestService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task Withdraw_KeepsTheRow_AndSetsWithdrawalFields()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");

            var (ok, error) = await _service.UnregisterAsync(
                UserId,
                "tray-liner",
                LocationId
            );

            Assert.True(ok);
            Assert.Null(error);
            var row = Assert.Single(_context.ShopInterestRegistrations.ToList());
            Assert.NotNull(row.WithdrawnAtUtc);
            Assert.Equal(UserId, row.WithdrawnByUserId);
        }

        [Fact]
        public async Task Register_AfterWithdraw_CreatesANewActiveRow_AndKeepsHistory()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");
            await _service.UnregisterAsync(UserId, "tray-liner", LocationId);

            var (ok, _, message) = await RegisterAsync("tray-liner");

            Assert.True(ok);
            Assert.Equal(ShopInterestService.ThanksMessage, message);
            var rows = _context.ShopInterestRegistrations
                .OrderBy(row => row.Id)
                .ToList();
            Assert.Equal(2, rows.Count);
            Assert.NotNull(rows[0].WithdrawnAtUtc);
            Assert.Null(rows[1].WithdrawnAtUtc);
        }

        [Fact]
        public async Task Register_WhileActive_DoesNotCreateASecondRow()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");

            var (ok, _, message) = await RegisterAsync("tray-liner");

            Assert.True(ok);
            Assert.Equal(ShopInterestService.ThanksMessage, message);
            Assert.Single(_context.ShopInterestRegistrations.ToList());
        }

        [Fact]
        public async Task Customer_IsNotShownAsRegistered_AfterWithdraw()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");
            await _service.UnregisterAsync(UserId, "tray-liner", LocationId);

            var products = await _service.ListActiveProductsAsync(
                UserId,
                LocationId
            );

            Assert.False(Assert.Single(products).InterestRegistered);
        }

        [Fact]
        public async Task AdminList_ByDefault_HidesWithdrawnRows_ButCountsOnlyActive()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");
            await _service.UnregisterAsync(UserId, "tray-liner", LocationId);
            await RegisterAsync("tray-liner", userId: UserId + 1);

            var result = await _service.ListForAdminAsync();

            Assert.Single(result.Items);
            Assert.Equal("Active", result.Items[0].State);
            var count = Assert.Single(result.CountsByProduct);
            Assert.Equal(1, count.Count);
        }

        [Fact]
        public async Task AdminList_IncludeWithdrawn_ShowsStateAndWithdrawnDate()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");
            await _service.UnregisterAsync(UserId, "tray-liner", LocationId);

            var result = await _service.ListForAdminAsync(includeWithdrawn: true);

            var row = Assert.Single(result.Items);
            Assert.Equal("Withdrawn", row.State);
            Assert.NotNull(row.WithdrawnAtUtc);
            var count = Assert.Single(result.CountsByProduct);
            Assert.Equal(0, count.Count);
        }

        [Fact]
        public async Task AdminList_ZeroCountConcepts_StillAppear()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await SeedProductAsync("receipt-label", "Receipt Label");
            await RegisterAsync("tray-liner");

            var result = await _service.ListForAdminAsync();

            Assert.Equal(2, result.CountsByProduct.Count);
            Assert.Equal(
                0,
                result.CountsByProduct.Single(row => row.ProductKey == "receipt-label").Count
            );
        }

        [Fact]
        public async Task AdminList_MasksTheUserEmail()
        {
            await SeedProductAsync("tray-liner", "Tray Liner");
            await RegisterAsync("tray-liner");

            var result = await _service.ListForAdminAsync();

            var row = Assert.Single(result.Items);
            Assert.Equal(ShopInterestEmailMask.Mask(Email), row.UserEmailMasked);
            Assert.DoesNotContain("jane.doe", row.UserEmailMasked);
        }

        private async Task SeedProductAsync(string productKey, string title)
        {
            _context.ShopInterestProducts.Add(
                new ShopInterestProduct
                {
                    ProductKey = productKey,
                    Title = title,
                    Description = "Concept",
                    Material = string.Empty,
                    Dimensions = string.Empty,
                    IsActive = true,
                }
            );
            await _context.SaveChangesAsync();
        }

        private Task<(bool Ok, string? Error, string? Message)> RegisterAsync(
            string productKey,
            int userId = UserId
        ) =>
            _service.RegisterAsync(
                userId,
                RestaurantId,
                "Interest Venue",
                new RegisterShopInterestRequest
                {
                    ProductKey = productKey,
                    LocationId = LocationId,
                },
                "Main Dining",
                "Jane Doe",
                Email
            );
    }
}
