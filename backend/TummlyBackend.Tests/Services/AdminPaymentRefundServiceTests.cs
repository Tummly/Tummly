using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminPaymentRefundServiceTests
    {
        [Fact]
        public async Task RefundAsync_OnSuccess_AppendsPaymentRefundAudit()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var audit = new AdminAuditService(db, TimeProvider.System);
            var svc = CreateService(db, merchant, audit);

            var result = await svc.RefundAsync(
                new AdminPaymentRefundRequest
                {
                    RestaurantId = seeded.RestaurantId,
                    OrderId = seeded.PaymentOrderId,
                    AmountMinor = 1200,
                    IdempotencyKey = "idem-refund-success-001",
                    ActorStaffUserId = seeded.AdminId,
                }
            );

            Assert.True(result.Succeeded);
            Assert.False(string.IsNullOrWhiteSpace(result.RefundOrderId));

            var events = await db.AdminAuditEvents.ToListAsync();
            Assert.Single(events);
            var row = events[0];
            Assert.Equal(AdminAuditActions.PaymentRefund, row.Action);
            Assert.Equal(AdminAuditTargetTypes.PaymentOrder, row.TargetType);
            Assert.Equal(seeded.PaymentOrderId, row.TargetId);
            Assert.Equal(seeded.RestaurantId, row.RestaurantId);
            Assert.Equal(seeded.AdminId, row.ActorAdminUserId);
            Assert.Equal(seeded.AdminEmail, row.ActorIdentity);

            using var detail = JsonDocument.Parse(row.DetailJson!);
            Assert.Equal(
                result.RefundOrderId,
                detail.RootElement.GetProperty("refundOrderId").GetString()
            );
            Assert.Equal(
                1200,
                detail.RootElement.GetProperty("amountMinor").GetInt32()
            );
            Assert.Equal(
                "idem-refund-success-001",
                detail.RootElement.GetProperty("idempotencyKey").GetString()
            );
        }

        [Fact]
        public async Task RefundAsync_IdempotentCompleted_DoesNotAppendSecondEvent()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var audit = new AdminAuditService(db, TimeProvider.System);
            var svc = CreateService(db, merchant, audit);

            const string idempotencyKey = "idem-refund-replay-001";
            db.AdminPaymentRefundIntents.Add(
                new AdminPaymentRefundIntent
                {
                    Id = Guid.NewGuid(),
                    IdempotencyKey = idempotencyKey,
                    RestaurantId = seeded.RestaurantId,
                    SourcePaymentOrderId = seeded.PaymentOrderId,
                    RefundOrderId = "ord_refund_existing",
                    AmountMinor = 1200,
                    ActorStaffUserId = seeded.AdminId,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();

            var beforeCount = await db.AdminAuditEvents.CountAsync();

            var result = await svc.RefundAsync(
                new AdminPaymentRefundRequest
                {
                    RestaurantId = seeded.RestaurantId,
                    OrderId = seeded.PaymentOrderId,
                    AmountMinor = 1200,
                    IdempotencyKey = idempotencyKey,
                    ActorStaffUserId = seeded.AdminId,
                }
            );

            Assert.True(result.Succeeded);
            Assert.Equal("ord_refund_existing", result.RefundOrderId);
            Assert.Equal(0, merchant.RefundOrderCallCount);
            Assert.Equal(beforeCount, await db.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task RefundAsync_EarlyFail_DoesNotAppend()
        {
            await using var db = CreateContext();
            var seeded = await SeedAsync(db);
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            var audit = new AdminAuditService(db, TimeProvider.System);
            var svc = CreateService(db, merchant, audit);

            var result = await svc.RefundAsync(
                new AdminPaymentRefundRequest
                {
                    RestaurantId = seeded.RestaurantId,
                    OrderId = seeded.PaymentOrderId,
                    IdempotencyKey = "  ",
                    ActorStaffUserId = seeded.AdminId,
                }
            );

            Assert.False(result.Succeeded);
            Assert.Equal("idempotency_key_required", result.Code);
            Assert.Empty(await db.AdminAuditEvents.ToListAsync());
            Assert.Equal(0, merchant.RefundOrderCallCount);
        }

        private static AdminPaymentRefundService CreateService(
            ApplicationDbContext db,
            IRevolutMerchantClient merchant,
            IAdminAuditService audit
        )
        {
            return new AdminPaymentRefundService(
                db,
                merchant,
                TimeProvider.System,
                audit
            );
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

        private static async Task<Seeded> SeedAsync(ApplicationDbContext db)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Owner",
                Role = "Owner",
                PhoneNumber = "07700900111",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Refund Unit Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();

            db.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );

            var paymentOrderId = $"ord_pay_{Guid.NewGuid():N}";
            db.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.TopupAllocation,
                    Quantity = 100,
                    SourcePaymentRef = paymentOrderId,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    ExpiresAtUtc = DateTime.UtcNow.AddMonths(12),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );

            var adminEmail = $"admin-{Guid.NewGuid():N}@tummly.com";
            var admin = new Admin
            {
                FullName = "Tummly Admin",
                Email = adminEmail,
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Admins.Add(admin);
            await db.SaveChangesAsync();

            return new Seeded(restaurant.Id, paymentOrderId, admin.Id, adminEmail);
        }

        private sealed record Seeded(
            int RestaurantId,
            string PaymentOrderId,
            int AdminId,
            string AdminEmail
        );
    }
}
