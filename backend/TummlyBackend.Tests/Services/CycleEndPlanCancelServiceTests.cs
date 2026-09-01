using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public sealed class CycleEndPlanCancelServiceTests
    {
        [Fact]
        public async Task Apply_CallsScheduleCancelAtCycleEnd_WhenSubscriptionCorrelated()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    TargetPlan = "Growth",
                    TargetCadence = "monthly",
                    RevolutSubscriptionId = "sub_cancel_schedule",
                    SetupOrderId = "ord_setup_cancel",
                    CheckoutUrl = "https://checkout.example/cancel",
                    IdempotencyKey = "key-cancel",
                    IsOpen = false,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                }
            );
            await context.SaveChangesAsync();

            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new CycleEndPlanCancelService(context, merchant);

            await service.ApplyRevolutCancelAtCycleEndIfNeededAsync(restaurantId);

            Assert.Equal(1, merchant.ScheduleSubscriptionCancelCallCount);
            Assert.Equal(
                "sub_cancel_schedule",
                merchant.LastScheduledCancelSubscriptionId
            );
            Assert.Equal(0, merchant.CancelSubscriptionCallCount);
        }

        [Fact]
        public async Task Apply_NoOp_WhenNoSubscriptionCorrelated()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new CycleEndPlanCancelService(context, merchant);

            await service.ApplyRevolutCancelAtCycleEndIfNeededAsync(restaurantId);

            Assert.Equal(0, merchant.ScheduleSubscriptionCancelCallCount);
        }

        private static async Task<int> SeedRestaurantAsync(
            ApplicationDbContext context
        )
        {
            var owner = new User
            {
                FullName = "Owner",
                Email = "owner@example.com",
                PasswordHash = "x",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Cancel Cafe",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await context.SaveChangesAsync();
            return restaurant.Id;
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
