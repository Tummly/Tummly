using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RestaurantAccountTypePromotionTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RestaurantAccountTypePromotion _promotion;

        public RestaurantAccountTypePromotionTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            _promotion = new RestaurantAccountTypePromotion(_context);
        }

        [Fact]
        public async Task Ensure_DoesNotPromote_WhenOnlyOneActiveLocation()
        {
            var restaurantId = await SeedRestaurantAsync(
                accountType: "Single",
                activeCount: 1,
                pausedCount: 0,
                draftCount: 1
            );

            var result = await _promotion.EnsureRoutingAccountTypeAsync(
                restaurantId
            );

            Assert.Equal("Single", result);
            var restaurant = await _context.Restaurants.SingleAsync();
            Assert.Equal("Single", restaurant.AccountType);
        }

        [Fact]
        public async Task Ensure_PromotesToMulti_WhenSecondActiveLocationExists()
        {
            var restaurantId = await SeedRestaurantAsync(
                accountType: "Single",
                activeCount: 2,
                pausedCount: 0,
                draftCount: 0
            );

            var result = await _promotion.EnsureRoutingAccountTypeAsync(
                restaurantId
            );

            Assert.Equal("Multi", result);
            var restaurant = await _context.Restaurants.SingleAsync();
            Assert.Equal("Multi", restaurant.AccountType);
            var owner = await _context.Users.SingleAsync();
            Assert.Equal("Multi", owner.AccountType);
        }

        [Fact]
        public async Task Ensure_PromotesToMulti_WhenActivePlusPausedReachTwo()
        {
            var restaurantId = await SeedRestaurantAsync(
                accountType: "Single",
                activeCount: 1,
                pausedCount: 1,
                draftCount: 0
            );

            var result = await _promotion.EnsureRoutingAccountTypeAsync(
                restaurantId
            );

            Assert.Equal("Multi", result);
            Assert.Equal(
                "Multi",
                (await _context.Restaurants.SingleAsync()).AccountType
            );
        }

        [Fact]
        public async Task Ensure_LeavesMultiUnchanged()
        {
            var restaurantId = await SeedRestaurantAsync(
                accountType: "Multi",
                activeCount: 2,
                pausedCount: 0,
                draftCount: 0
            );

            var result = await _promotion.EnsureRoutingAccountTypeAsync(
                restaurantId
            );

            Assert.Equal("Multi", result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<int> SeedRestaurantAsync(
            string accountType,
            int activeCount,
            int pausedCount,
            int draftCount
        )
        {
            var user = new User
            {
                FullName = "Alex Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                AccountType = accountType,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Test",
                AccountType = accountType,
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                    restaurantId: 0,
                    "TUMMLY-UK-GBP-2026-08-V3"
                ),
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;
            await _context.SaveChangesAsync();

            void AddLocations(int count, LocationLifecycleStatus status)
            {
                for (var i = 0; i < count; i++)
                {
                    _context.RestaurantLocations.Add(
                        new RestaurantLocation
                        {
                            RestaurantId = restaurant.Id,
                            LocationName = $"{status}-{i}",
                            Address = "1 High Street",
                            City = "Leeds",
                            Postcode = "LS1 1AA",
                            LifecycleStatus = status,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                }
            }

            AddLocations(activeCount, LocationLifecycleStatus.Active);
            AddLocations(pausedCount, LocationLifecycleStatus.Paused);
            AddLocations(draftCount, LocationLifecycleStatus.Draft);
            await _context.SaveChangesAsync();

            return restaurant.Id;
        }
    }
}
