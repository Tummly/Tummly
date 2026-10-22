using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class SignupSessionLoginTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly SignupService _sut;

        public SignupSessionLoginTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _db = new ApplicationDbContext(options);
            _sut = new SignupService(
                _db,
                new EmailServiceStubBase(),
                new NoOpProvisioningService(),
                new CustomPlanQuoteService(_db),
                new StubJwtService()
            );
        }

        [Fact]
        public async Task SessionLogin_PilotComplete_IssuesTokensOnce()
        {
            var (sessionToken, _) = await SeedCompletePilotAsync();

            var data = await _sut.SessionLoginAsync(sessionToken);
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("token", out _));
            Assert.True(doc.RootElement.TryGetProperty("refreshToken", out _));

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.NotNull(pending.SessionLoginConsumedAtUtc);

            var used = await Assert.ThrowsAsync<SignupConflictException>(() =>
                _sut.SessionLoginAsync(sessionToken)
            );
            Assert.Equal("session_login_used", used.Code);
        }

        [Fact]
        public async Task SessionLogin_NotComplete_PaymentPending()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = "pending@example.com",
                    Status = PendingSignupStatuses.AwaitingPayment,
                    TermsAccepted = true,
                    ChosenPlan = BillingSubscriptionPlans.Starter,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupConflictException>(() =>
                _sut.SessionLoginAsync(sessionToken)
            );
            Assert.Equal("payment_pending", ex.Code);
        }

        [Fact]
        public async Task SessionLogin_Expired_Throws()
        {
            var (sessionToken, _) = await SeedCompletePilotAsync(
                completedAt: DateTime.UtcNow.AddHours(-25)
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.SessionLoginAsync(sessionToken)
            );
            Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SessionLogin_FreeBilling_PaymentPending()
        {
            var (sessionToken, restaurantId) = await SeedCompletePilotAsync();
            var billing = await _db.BillingAccounts.SingleAsync(b =>
                b.RestaurantId == restaurantId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingStatus = BillingStatuses.Free;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupConflictException>(() =>
                _sut.SessionLoginAsync(sessionToken)
            );
            Assert.Equal("payment_pending", ex.Code);
        }

        [Fact]
        public async Task SessionLogin_ContinueOnFree_AfterFailedCheckout_IssuesTokens()
        {
            var (sessionToken, restaurantId) = await SeedCompletePilotAsync();
            var billing = await _db.BillingAccounts.SingleAsync(b =>
                b.RestaurantId == restaurantId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingStatus = BillingStatuses.Free;
            billing.PendingPaymentFailureCode = "plan_variation_missing";
            await _db.SaveChangesAsync();

            var refused = await Assert.ThrowsAsync<SignupConflictException>(() =>
                _sut.SessionLoginAsync(sessionToken)
            );
            Assert.Equal("payment_pending", refused.Code);

            var data = await _sut.SessionLoginAsync(sessionToken, continueOnFree: true);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
            Assert.True(doc.RootElement.TryGetProperty("token", out _));
        }

        [Fact]
        public async Task SessionLogin_ContinueOnFree_WithoutFailure_PaymentPending()
        {
            var (sessionToken, restaurantId) = await SeedCompletePilotAsync();
            var billing = await _db.BillingAccounts.SingleAsync(b =>
                b.RestaurantId == restaurantId
            );
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingStatus = BillingStatuses.Free;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupConflictException>(() =>
                _sut.SessionLoginAsync(sessionToken, continueOnFree: true)
            );
            Assert.Equal("payment_pending", ex.Code);
        }

        private async Task<(Guid SessionToken, int RestaurantId)> SeedCompletePilotAsync(
            DateTime? completedAt = null
        )
        {
            var sessionToken = Guid.NewGuid();
            var email = $"owner-{Guid.NewGuid():N}@example.com";
            var at = completedAt ?? DateTime.UtcNow;

            var user = new User
            {
                Email = email,
                FullName = "Owner",
                Role = "Owner",
                AccountType = "Single",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Kitchen",
                AccountType = "Single",
                OwnerUserId = user.Id,
                BusinessCategory = "takeaway",
            };
            _db.Restaurants.Add(restaurant);
            await _db.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;
            _db.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = "v3",
                }
            );
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = email,
                    Status = PendingSignupStatuses.Complete,
                    TermsAccepted = true,
                    ChosenPlan = BillingSubscriptionPlans.Pilot,
                    CreatedAtUtc = at.AddHours(-1),
                    UpdatedAtUtc = at,
                }
            );
            await _db.SaveChangesAsync();
            return (sessionToken, restaurant.Id);
        }

        public void Dispose() => _db.Dispose();
    }
}
