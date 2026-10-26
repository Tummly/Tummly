using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public class OperatorPlanGateTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public OperatorPlanGateTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public void HasPlan_FreeIsFalse_PilotAndPaidAreTrue()
        {
            Assert.False(OperatorPlanGate.HasPlan(Account(BillingSubscriptionPlans.Free)));
            Assert.True(OperatorPlanGate.HasPlan(Account(BillingSubscriptionPlans.Pilot)));
            Assert.True(OperatorPlanGate.HasPlan(Account(BillingSubscriptionPlans.Starter)));
        }

        [Fact]
        public void EvaluatePlanDeny_FreeReturnsPlanRequired_PilotReturnsNull()
        {
            Assert.Equal(
                OperatorPlanGate.PlanRequiredCode,
                OperatorPlanGate.EvaluatePlanDeny(Account(BillingSubscriptionPlans.Free))
            );
            Assert.Null(OperatorPlanGate.EvaluatePlanDeny(Account(BillingSubscriptionPlans.Pilot)));
        }

        [Fact]
        public void PlanRequiredMessage_IsTheApprovedCopy()
        {
            Assert.Equal("plan_required", OperatorPlanGate.PlanRequiredCode);
            Assert.Equal(
                "Choose a Pilot or paid plan to continue.",
                OperatorPlanGate.PlanRequiredMessage
            );
        }

        [Fact]
        public async Task EnsurePlanAndPaidWrite_FreeAccount_ThrowsPlanRequired()
        {
            var locationId = await SeedLocationAsync(
                BillingSubscriptionPlans.Free,
                BillingStatuses.Free
            );

            var ex = await Assert.ThrowsAsync<OperatorBillingLockedException>(() =>
                OperatorPlanGate.EnsurePlanAndPaidWriteForLocationAsync(_context, locationId)
            );

            Assert.Equal("plan_required", ex.Code);
        }

        [Fact]
        public async Task EnsurePlanAndPaidWrite_PilotInSoftLock_ThrowsSoftLock()
        {
            var locationId = await SeedLocationAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.SoftLock
            );

            var ex = await Assert.ThrowsAsync<OperatorBillingLockedException>(() =>
                OperatorPlanGate.EnsurePlanAndPaidWriteForLocationAsync(_context, locationId)
            );

            Assert.Equal(OperatorBillingLockEvaluator.SoftLock, ex.Code);
        }

        [Fact]
        public async Task EnsurePlanAndPaidWrite_PilotActive_Allows()
        {
            var locationId = await SeedLocationAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );

            await OperatorPlanGate.EnsurePlanAndPaidWriteForLocationAsync(_context, locationId);
        }

        [Fact]
        public async Task EnsurePlanAndPaidWrite_NoBillingAccount_Allows()
        {
            var restaurant = new Restaurant
            {
                Name = "No Billing Row",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            await OperatorPlanGate.EnsurePlanAndPaidWriteForLocationAsync(_context, location.Id);
        }

        [Fact]
        public void Forbidden_PlanRequired_UsesApprovedMessage()
        {
            var result = OperatorBillingLockGate.Forbidden(OperatorPlanGate.PlanRequiredCode);

            Assert.Equal(403, result.StatusCode);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            Assert.Equal("plan_required", doc.RootElement.GetProperty("code").GetString());
            Assert.Equal(
                "Choose a Pilot or paid plan to continue.",
                doc.RootElement.GetProperty("message").GetString()
            );
        }

        [Fact]
        public void Forbidden_SoftLock_KeepsCodeAsMessage()
        {
            var result = OperatorBillingLockGate.Forbidden(OperatorBillingLockEvaluator.SoftLock);

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            Assert.Equal("soft_lock", doc.RootElement.GetProperty("message").GetString());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static BillingAccount Account(string plan)
        {
            var account = BillingCreditsService.CreateDefaultBillingAccount(1, "TUMMLY-UK-GBP-2026-08-V3");
            account.SubscriptionPlan = plan;
            return account;
        }

        private async Task<int> SeedLocationAsync(string plan, string billingStatus)
        {
            var restaurant = new Restaurant
            {
                Name = "Plan Gate Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            account.SubscriptionPlan = plan;
            account.BillingStatus = billingStatus;
            if (billingStatus == BillingStatuses.SoftLock)
            {
                account.SoftLockEnteredAt = DateTime.UtcNow.AddDays(-1);
            }
            _context.BillingAccounts.Add(account);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}
