using Microsoft.EntityFrameworkCore;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// Concurrent £0 Counter requests for one Location. The filtered unique
    /// index <c>IX_ShopOrders_LocationId_Complimentary</c> admits one row. The
    /// callers that lose the insert race return the winning order.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class ComplimentaryStarterConcurrentSqlTests
    {
        private const int ConcurrentCallers = 4;

        private readonly SqlServerFixture _fixture;

        public ComplimentaryStarterConcurrentSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task ConcurrentStarterRequests_ForOneLocation_CreateOneOrder()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var seeded = await SeedLocationAsync();

            var results = await Task.WhenAll(
                Enumerable
                    .Range(0, ConcurrentCallers)
                    .Select(_ => EnsureStarterOrderAsync(seeded))
            );

            Assert.Equal(1, results.Count(result => result.Created));
            Assert.All(
                results,
                result => Assert.Equal(results[0].ShopOrderId, result.ShopOrderId)
            );

            await using var verify = _fixture.CreateContext();
            Assert.Equal(
                1,
                await verify.ShopOrders.CountAsync(row =>
                    row.LocationId == seeded.LocationId && row.IsComplimentary
                )
            );
        }

        private async Task<ComplimentaryStarterShopOrderResult> EnsureStarterOrderAsync(
            (int RestaurantId, int LocationId, int UserId) seeded
        )
        {
            await using var context = _fixture.CreateContext();
            var service = new ComplimentaryStarterShopOrderService(
                context,
                new DbMaterialsCatalog(context),
                new ShopOrderNumberAllocator(context)
            );
            return await service.EnsureForLocationAsync(
                seeded.RestaurantId,
                seeded.LocationId,
                seeded.UserId,
                "Starter Owner"
            );
        }

        private async Task<(int RestaurantId, int LocationId, int UserId)> SeedLocationAsync()
        {
            await using var context = _fixture.CreateContext();
            await ShopMaterialsCatalogSeed.EnsureSeededAsync(context);

            var suffix = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            var owner = new User
            {
                FullName = "Starter Owner",
                Email = $"starter-owner-{suffix}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Starter Venue {suffix}",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            // Start the order-number sequence here. Its first insert races on
            // its own primary key, which this test does not target.
            context.ShopOrderSequences.Add(new ShopOrderSequence
            {
                RestaurantId = restaurant.Id,
                NextNumber = 1,
            });
            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                RevolutCustomerId = $"cust_starter_{suffix}",
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = ShopCapacityTestSeed.PricebookId,
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Starter Site",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return (restaurant.Id, location.Id, owner.Id);
        }
    }
}
