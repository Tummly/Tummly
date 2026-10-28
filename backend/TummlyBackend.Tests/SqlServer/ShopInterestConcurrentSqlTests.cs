using Microsoft.EntityFrameworkCore;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// Concurrent interest registrations for one user, product and Location.
    /// The unique index <c>IX_ShopInterestRegistrations_User_Product_Location</c>
    /// admits one row. The callers that lose the insert race still return
    /// the thanks result.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class ShopInterestConcurrentSqlTests
    {
        private const int ConcurrentCallers = 4;

        private readonly SqlServerFixture _fixture;

        public ShopInterestConcurrentSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task ConcurrentRegisters_ForOneUserProductLocation_StoreOneRow()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var suffix = Guid.NewGuid().ToString("N")[..12];
            var productKey = $"race-{suffix}";
            var userId = Random.Shared.Next(1_000_000, 2_000_000);
            var locationId = Random.Shared.Next(1_000_000, 2_000_000);
            await SeedProductAsync(productKey);

            var results = await Task.WhenAll(
                Enumerable
                    .Range(0, ConcurrentCallers)
                    .Select(_ => RegisterAsync(productKey, userId, locationId))
            );

            Assert.All(results, result => Assert.True(result.Ok));
            Assert.All(
                results,
                result => Assert.Equal(ShopInterestService.ThanksMessage, result.Message)
            );

            await using var verify = _fixture.CreateContext();
            Assert.Equal(
                1,
                await verify.ShopInterestRegistrations.CountAsync(row =>
                    row.ProductKey == productKey
                    && row.UserId == userId
                    && row.LocationId == locationId
                )
            );
        }

        private async Task<(bool Ok, string? Error, string? Message)> RegisterAsync(
            string productKey,
            int userId,
            int locationId
        )
        {
            await using var context = _fixture.CreateContext();
            var service = new ShopInterestService(
                context,
                new AdminAuditService(context, TimeProvider.System)
            );
            return await service.RegisterAsync(
                userId,
                restaurantId: 1,
                restaurantName: "Race Venue",
                new RegisterShopInterestRequest
                {
                    ProductKey = productKey,
                    LocationId = locationId,
                },
                locationName: "Race Site",
                userFullName: "Race Owner",
                userEmail: "race-owner@example.com"
            );
        }

        private async Task SeedProductAsync(string productKey)
        {
            await using var context = _fixture.CreateContext();
            context.ShopInterestProducts.Add(new ShopInterestProduct
            {
                ProductKey = productKey,
                Title = "Race tray",
                Description = "Concurrent registration check.",
                IsActive = true,
            });
            await context.SaveChangesAsync();
        }
    }
}
