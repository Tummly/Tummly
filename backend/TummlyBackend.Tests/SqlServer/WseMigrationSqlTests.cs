using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Migrations;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class WseMigrationSqlTests
    {
        private const string TestPricebook = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly SqlServerFixture _fixture;

        public WseMigrationSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Migration_BackfillsDeliveryState()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var now = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
            var deliveredAt = new DateTime(2026, 10, 20, 9, 5, 0, DateTimeKind.Utc);

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            var feedbackId = await SeedFeedbackAsync(context, now);

            var accepted = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = FeedbackGuestResponseChannel.Email,
                Intent = FeedbackRecoveryIntent.RespondToGuest,
                MaskedDestination = "a***@example.com",
                Body = "Accepted reply",
                AuthorDisplayName = "Owner",
                CreatedAt = now,
                EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.Accepted,
                EmailDeliveredAt = deliveredAt,
            };
            var pending = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = FeedbackGuestResponseChannel.Email,
                Intent = FeedbackRecoveryIntent.RespondToGuest,
                MaskedDestination = "a***@example.com",
                Body = "Pending reply",
                AuthorDisplayName = "Owner",
                CreatedAt = now,
                EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.Pending,
            };
            var sms = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = FeedbackGuestResponseChannel.Sms,
                Intent = FeedbackRecoveryIntent.RespondToGuest,
                MaskedDestination = "+44 ******123",
                Body = "SMS reply",
                AuthorDisplayName = "Owner",
                CreatedAt = now,
                EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable,
            };
            var neverSent = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = FeedbackGuestResponseChannel.Email,
                Intent = FeedbackRecoveryIntent.RespondToGuest,
                MaskedDestination = "a***@example.com",
                Body = "Never sent reply",
                AuthorDisplayName = "Owner",
                CreatedAt = now,
                EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable,
            };
            context.FeedbackGuestResponses.AddRange(accepted, pending, sms, neverSent);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                WSEDeliveryStopLedger.BackfillSql
            );

            context.ChangeTracker.Clear();
            var rows = await context
                .FeedbackGuestResponses.AsNoTracking()
                .Where(r =>
                    r.Id == accepted.Id
                    || r.Id == pending.Id
                    || r.Id == sms.Id
                    || r.Id == neverSent.Id
                )
                .ToDictionaryAsync(r => r.Id);

            Assert.Equal(GuestReplyDeliveryState.Accepted, rows[accepted.Id].DeliveryState);
            Assert.Equal(deliveredAt, rows[accepted.Id].DeliveryStateAtUtc);
            Assert.Equal(GuestReplyDeliveryState.Pending, rows[pending.Id].DeliveryState);
            Assert.Equal(now, rows[pending.Id].DeliveryStateAtUtc);
            Assert.Equal(GuestReplyDeliveryState.Unavailable, rows[sms.Id].DeliveryState);
            Assert.Equal(now, rows[sms.Id].DeliveryStateAtUtc);
            Assert.Equal(GuestReplyDeliveryState.Unavailable, rows[neverSent.Id].DeliveryState);
            Assert.Equal(now, rows[neverSent.Id].DeliveryStateAtUtc);

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Migration_DuplicateTopupPreCheck_PassesWithoutDuplicates()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();

            // The unique index blocks duplicates, so the pre-check must pass.
            await context.Database.ExecuteSqlRawAsync(
                WSEDeliveryStopLedger.DuplicateTopupPreCheckSql
            );
        }

        [SkippableFact]
        public async Task Migration_DuplicateTopupPreCheck_ThrowsOnDuplicateGroup()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var restaurantId = await SeedBillingRestaurantAsync();
            var paymentRef = $"order-{Guid.NewGuid():N}";

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            // Drop the unique index in this transaction only, so the duplicate can exist
            // as it would before the migration. The rollback restores the index.
            await context.Database.ExecuteSqlRawAsync(
                "DROP INDEX [IX_CreditLedgerEntries_RestaurantId_SourcePaymentRef_EntryType] ON [CreditLedgerEntries]"
            );
            context.CreditLedgerEntries.AddRange(
                TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.TopupAllocation),
                TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.TopupAllocation)
            );
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                context.Database.ExecuteSqlRawAsync(
                    WSEDeliveryStopLedger.DuplicateTopupPreCheckSql
                )
            );

            Assert.Contains(
                "WSE: duplicate TopupAllocation rows exist; resolve before migrating.",
                ex.ToString()
            );

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Ledger_SecondTopupAllocationSameRef_Throws()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var restaurantId = await SeedBillingRestaurantAsync();
            var paymentRef = $"order-{Guid.NewGuid():N}";

            await using (var first = _fixture.CreateContext())
            {
                first.CreditLedgerEntries.Add(
                    TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.TopupAllocation)
                );
                await first.SaveChangesAsync();
            }

            await using var second = _fixture.CreateContext();
            second.CreditLedgerEntries.Add(
                TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.TopupAllocation)
            );

            await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        }

        [SkippableFact]
        public async Task Ledger_SameRefDifferentEntryType_Allowed()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var restaurantId = await SeedBillingRestaurantAsync();
            var paymentRef = $"order-{Guid.NewGuid():N}";

            await using var context = _fixture.CreateContext();
            context.CreditLedgerEntries.Add(
                TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.TopupAllocation)
            );
            context.CreditLedgerEntries.Add(
                TopupRow(restaurantId, paymentRef, CreditLedgerEntryTypes.Refund)
            );
            await context.SaveChangesAsync();

            Assert.Equal(
                2,
                await context.CreditLedgerEntries.CountAsync(e =>
                    e.RestaurantId == restaurantId && e.SourcePaymentRef == paymentRef
                )
            );
        }

        private static CreditLedgerEntry TopupRow(
            int restaurantId,
            string paymentRef,
            string entryType
        )
        {
            return new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                Channel = CreditChannels.Sms,
                EntryType = entryType,
                Quantity = 100,
                SourcePaymentRef = paymentRef,
                CreatedAtUtc = DateTime.UtcNow,
            };
        }

        private async Task<int> SeedBillingRestaurantAsync()
        {
            await using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N");

            var user = new User
            {
                FullName = "Owner",
                Email = $"wse-sql-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"WSE SQL {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(restaurant.Id, TestPricebook)
            );
            await context.SaveChangesAsync();

            return restaurant.Id;
        }

        private static async Task<int> SeedFeedbackAsync(
            ApplicationDbContext context,
            DateTime now
        )
        {
            var suffix = Guid.NewGuid().ToString("N");

            var user = new User
            {
                FullName = "Owner",
                Email = $"wse-feedback-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"WSE Feedback {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"wse{suffix}"[..32],
                Status = QrCodeStatus.Active,
                CreatedAt = now,
            };
            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                QrCodeId = qrCode.Id,
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = $"wse-guest-{suffix}@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            return feedback.Id;
        }
    }
}
