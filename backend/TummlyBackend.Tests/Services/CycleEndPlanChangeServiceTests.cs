using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CycleEndPlanChangeServiceTests
    {
        [Fact]
        public async Task Apply_CallsChangePlanAtCycleEnd_WithMappedLookupKey()
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
                    RevolutSubscriptionId = "sub_active_9",
                    SetupOrderId = "ord_setup_9",
                    CheckoutUrl = "https://checkout.example/x",
                    IdempotencyKey = "key-9",
                    IsOpen = false,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                }
            );
            await context.SaveChangesAsync();

            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new CycleEndPlanChangeService(context, merchant);

            await service.ApplyRevolutChangePlanIfNeededAsync(
                restaurantId,
                "Starter",
                "monthly"
            );

            Assert.Equal(1, merchant.ChangeSubscriptionPlanCallCount);
            Assert.Equal("sub_active_9", merchant.LastChangePlanSubscriptionId);
            Assert.Equal(
                RevolutPlanVariationKeys.StarterMonthly,
                merchant.LastChangePlanLookupKey
            );
        }

        [Fact]
        public async Task Apply_FailsClosed_WhenMerchantReportsVariationMissing()
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
                    RevolutSubscriptionId = "sub_active_9",
                    SetupOrderId = "ord_setup_9",
                    CheckoutUrl = "https://checkout.example/x",
                    IdempotencyKey = "key-9",
                    IsOpen = false,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var merchant = new MissingMapMerchant();
            var service = new CycleEndPlanChangeService(context, merchant);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    service.ApplyRevolutChangePlanIfNeededAsync(
                        restaurantId,
                        "Starter",
                        "monthly"
                    )
            );

            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                ex.Message
            );
        }

        [Fact]
        public async Task Apply_NoOp_WhenNoSubscriptionCorrelated()
        {
            await using var context = CreateContext();
            var restaurantId = await SeedRestaurantAsync(context);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var service = new CycleEndPlanChangeService(context, merchant);

            await service.ApplyRevolutChangePlanIfNeededAsync(
                restaurantId,
                "Starter",
                "monthly"
            );

            Assert.Equal(0, merchant.ChangeSubscriptionPlanCallCount);
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
                Name = "Cycle End Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
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
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class MissingMapMerchant : IRevolutMerchantClient
        {
            public void EnsureReadyForCreate(string? planVariationLookupKey = null)
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();


public Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(

                string subscriptionId,

                CancellationToken cancellationToken = default

            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            )
            {
                throw new RevolutMerchantNotReadyException(
                    RevolutMerchantCreateGate.PlanVariationMissing
                );
            }

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();
        }
    }
}
