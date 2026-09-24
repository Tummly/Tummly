using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class SignupProvisionTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly SignupService _signup;
        private readonly GuestLoopProvisioningService _provisioning;
        private readonly IPricebookCatalog _pricebook;
        private readonly IConfiguration _configuration;
        private readonly CountingCreditLedger _mintLedger = new();
        private readonly string _email = "pilot-owner@example.com";
        private Guid _sessionToken;
        private PendingSignup _pending = null!;

        public SignupProvisionTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _db = new ApplicationDbContext(options);

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                        ["JwtSettings:Secret"] =
                            "test-secret-key-that-is-long-enough-for-hmac-sha256",
                    }
                )
                .Build();

            var smartGuestLink = new SmartGuestLinkService(
                _db,
                _configuration,
                new NoOpBillingAccountLifecycle()
            );

            var qrCodeProvisioning = new QrCodeProvisioningService(
                _db,
                smartGuestLink
            );

            var packDir = ResolvePricebookPackDir();
            _pricebook = PricebookCatalog.LoadFromDirectory(packDir);
            _provisioning = new GuestLoopProvisioningService(
                _db,
                qrCodeProvisioning,
                new NoOpPrintReadyQrMaterialsWork(),
                new ComplimentaryStarterShopOrderService(
                    _db,
                    MaterialsCatalog.LoadFromDirectory(ResolveMaterialsPackDir()),
                    new ShopOrderNumberAllocator(_db)
                ),
                _configuration,
                _pricebook,
                _mintLedger,
                new NoOpBillingAccountLifecycle(),
                new RecordingFirstPaidConversionPaySession(_db)
            );

            _signup = new SignupService(
                _db,
                new TrackingEmailService(),
                _provisioning
            );
        }

        [Fact]
        public async Task SaveOnboarding_Verified_ProvisionsFreeAndCompletes()
        {
            await SeedVerifiedPendingAsync();

            var result = await _signup.SaveOnboardingAsync(
                _sessionToken,
                BuildValidOnboardingDto()
            );

            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(PendingSignupStatuses.Complete, result.Status);
            Assert.True(await _db.Users.AnyAsync(u => u.Email == _email));
            Assert.Equal(PendingSignupStatuses.Complete, _pending.Status);
            Assert.Equal(BillingSubscriptionPlans.Free, _pending.ChosenPlan);
            Assert.Equal("monthly", _pending.ChosenCadence);
            Assert.Equal("Single", _pending.AccountType);

            var user = await _db.Users.SingleAsync(u => u.Email == _email);
            Assert.NotNull(user.ActivatedAt);
            Assert.NotNull(user.ActivationExpiresAt);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Free,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Free, billing.BillingStatus);
            Assert.Equal(0, _mintLedger.MintPilotCallCount);
        }

        [Fact]
        public async Task SaveOnboarding_AlreadyComplete_IsIdempotent()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _signup.SaveOnboardingAsync(_sessionToken, dto);

            Assert.Equal(
                1,
                await _db.Users.CountAsync(u => u.Email == _email)
            );
            Assert.Equal(1, await _db.Restaurants.CountAsync());
        }

        [Fact]
        public async Task RetryProvision_OnboardingComplete_CreatesUserAndCompletes()
        {
            await SeedOnboardingCompletePendingAsync();

            await _signup.RetryProvisionAsync(_sessionToken);

            await _db.Entry(_pending).ReloadAsync();

            Assert.True(await _db.Users.AnyAsync(u => u.Email == _email));
            Assert.Equal(PendingSignupStatuses.Complete, _pending.Status);
            Assert.Equal(BillingSubscriptionPlans.Free, _pending.ChosenPlan);

            var restaurant = await _db.Restaurants.SingleAsync();
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Free,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Free, billing.BillingStatus);
            Assert.Equal(restaurant.Id, billing.RestaurantId);
            Assert.True(
                await _db.GuestLoopSetups.AnyAsync(g =>
                    g.RestaurantId == restaurant.Id
                )
            );
        }

        [Fact]
        public async Task RetryProvision_IsIdempotent()
        {
            await SeedOnboardingCompletePendingAsync();

            await _signup.RetryProvisionAsync(_sessionToken);
            await _signup.RetryProvisionAsync(_sessionToken);

            Assert.Equal(
                1,
                await _db.Users.CountAsync(u => u.Email == _email)
            );
            Assert.Equal(1, await _db.Restaurants.CountAsync());
        }

        [Fact]
        public async Task GetProvisioningStatus_ReadyWhenComplete()
        {
            await SeedOnboardingCompletePendingAsync();
            await _signup.RetryProvisionAsync(_sessionToken);

            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);

            Assert.Equal(PendingSignupStatuses.Complete, status.Status);
            Assert.True(status.Ready);
        }

        [Fact]
        public async Task RetryProvision_ChosenPlanPilot_ProvisionsPilot()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Pilot
            );

            await _signup.RetryProvisionAsync(_sessionToken);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Pilot,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Pilot, billing.BillingStatus);
            Assert.NotNull(billing.PilotPeriodEnd);
            Assert.Equal(1, _mintLedger.MintPilotCallCount);
        }

        [Fact]
        public async Task RetryProvision_ChosenPlanStarter_StaysFree_AndOpensPaySession()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Starter
            );

            await _signup.RetryProvisionAsync(_sessionToken);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Free,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Free, billing.BillingStatus);

            var session = await _db.RevolutPendingPaySessions.SingleAsync();
            Assert.True(session.IsOpen);
            Assert.Equal(
                BillingSubscriptionPlans.Starter,
                session.TargetPlan
            );
            Assert.Equal(
                RecordingFirstPaidConversionPaySession.CheckoutUrl,
                session.CheckoutUrl
            );

            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);
            Assert.True(status.Ready);
            Assert.Equal(
                PendingSignupStatuses.AwaitingPayment,
                status.Status
            );
            Assert.Equal(
                RecordingFirstPaidConversionPaySession.CheckoutUrl,
                status.PaymentRedirectUrl
            );
        }

        private async Task SeedOnboardingCompletePendingWithPlanAsync(
            string chosenPlan
        )
        {
            await SeedOnboardingCompletePendingAsync();
            _pending.ChosenPlan = chosenPlan;
            _pending.ChosenCadence = "monthly";
            await _db.SaveChangesAsync();
        }

        private static SaveSignupOnboardingDto BuildValidOnboardingDto() =>
            new()
            {
                Password = "Password1!",
                ConfirmPassword = "Password1!",
                FullName = "Pilot Owner",
                GroupName = "Pilot Kitchen",
                BusinessCategory = "takeaway",
                PrimaryPhone = "07911123456",
                Locations =
                [
                    new SaveSignupOnboardingDto.LocationItem
                    {
                        LocationName = "Main",
                        Address = "1 High Street",
                        City = "Leeds",
                        Postcode = "LS1 1AA",
                    },
                ],
            };

        private async Task SeedVerifiedPendingAsync()
        {
            _sessionToken = Guid.NewGuid();
            _pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = _sessionToken,
                Email = _email,
                Status = PendingSignupStatuses.Verified,
                OtpResendCount = 0,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            };

            _db.PendingSignups.Add(_pending);
            await _db.SaveChangesAsync();
        }

        private async Task SeedOnboardingCompletePendingAsync()
        {
            _sessionToken = Guid.NewGuid();
            var payload = new SignupOnboardingPayload
            {
                FullName = "Pilot Owner",
                GroupName = "Pilot Kitchen",
                BusinessCategory = "takeaway",
                PrimaryPhone = "07911123456",
                Locations =
                [
                    new SaveSignupOnboardingDto.LocationItem
                    {
                        LocationName = "Main",
                        Address = "1 High Street",
                        City = "Leeds",
                        Postcode = "LS1 1AA",
                    },
                ],
            };

            _pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = _sessionToken,
                Email = _email,
                Status = PendingSignupStatuses.OnboardingComplete,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-10),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FullName = "Pilot Owner",
                AccountType = "Single",
                OnboardingJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            };

            _db.PendingSignups.Add(_pending);
            await _db.SaveChangesAsync();
        }

        private static string ResolvePricebookPackDir()
        {
            var packDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(packDir))
            {
                packDir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            return packDir;
        }

        private static string ResolveMaterialsPackDir()
        {
            var packDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "materials-catalog-v1"
                )
            );
            if (!Directory.Exists(packDir))
            {
                packDir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "materials-catalog-v1"
                    )
                );
            }

            return packDir;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private sealed class CountingCreditLedger : NoOpCreditLedger
        {
            public int MintPilotCallCount { get; private set; }

            public override Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            )
            {
                MintPilotCallCount++;
                return base.MintPilotAtActivationAsync(
                    restaurantId,
                    cancellationToken
                );
            }
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public override Task SendOtpEmailAsync(string toEmail, string otp) =>
                Task.CompletedTask;
        }

        private sealed class NoOpPrintReadyQrMaterialsWork
            : IPrintReadyQrMaterialsWork
        {
            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
