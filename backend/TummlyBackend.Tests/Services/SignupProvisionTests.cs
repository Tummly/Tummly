using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
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
        private readonly RecordingSignupMerchant _merchant = new();
        private readonly FixedTimeProvider _clock = new(
            new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc)
        );
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
                _configuration,
                _pricebook
            );

            var paySession = new SignupPaySessionService(
                _db,
                _merchant,
                _pricebook,
                _configuration,
                _clock
            );

            _signup = new SignupService(
                _db,
                new TrackingEmailService(),
                _provisioning,
                paySession
            );
        }

        [Fact]
        public async Task ChoosePlan_Pilot_CreatesUserAndCompletes()
        {
            await SeedOnboardingCompletePendingAsync();

            var result = await _signup.ChoosePlanAsync(
                _sessionToken,
                "Pilot",
                "monthly"
            );

            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal("provisioned", result.Mode);
            Assert.True(await _db.Users.AnyAsync(u => u.Email == _email));
            Assert.Equal(PendingSignupStatuses.Complete, _pending.Status);

            var restaurant = await _db.Restaurants.SingleAsync();
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Pilot,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Pilot, billing.BillingStatus);
            Assert.Equal(restaurant.Id, billing.RestaurantId);
            Assert.True(
                await _db.GuestLoopSetups.AnyAsync(g =>
                    g.RestaurantId == restaurant.Id
                )
            );
        }

        [Fact]
        public async Task ChoosePlan_Pilot_IsIdempotent()
        {
            await SeedOnboardingCompletePendingAsync();

            await _signup.ChoosePlanAsync(_sessionToken, "Pilot", "monthly");
            await _signup.ChoosePlanAsync(_sessionToken, "Pilot", "monthly");

            Assert.Equal(
                1,
                await _db.Users.CountAsync(u => u.Email == _email)
            );
            Assert.Equal(1, await _db.Restaurants.CountAsync());
        }

        [Fact]
        public async Task ChoosePlan_Paid_CreatesCheckoutIntentAndReturnsUrl()
        {
            await SeedOnboardingCompletePendingAsync();

            var result = await _signup.ChoosePlanAsync(
                _sessionToken,
                "Growth",
                "monthly"
            );

            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal("checkout", result.Mode);
            Assert.Equal(RecordingSignupMerchant.CheckoutUrl, result.CheckoutUrl);
            Assert.Equal(PendingSignupStatuses.AwaitingPayment, _pending.Status);
            Assert.Equal(BillingSubscriptionPlans.Growth, _pending.ChosenPlan);
            Assert.Equal("monthly", _pending.ChosenCadence);
            Assert.False(await _db.Users.AnyAsync(u => u.Email == _email));

            var intent = await _db.RevolutOrderIntents.SingleAsync();
            Assert.Equal(RevolutOrderIntentPurposes.SignupPlan, intent.Purpose);
            Assert.Equal(_pending.Id, intent.PendingSignupId);
            Assert.Null(intent.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Growth, intent.TargetPlan);
            Assert.Equal("monthly", intent.TargetCadence);
            Assert.True(intent.IsOpen);
            Assert.Contains(
                "/signup/provisioning",
                _merchant.LastCreateOrderRequest!.RedirectUrl
            );
            Assert.Equal(1, _merchant.CreateOrderCallCount);
        }

        [Fact]
        public async Task ChoosePlan_Paid_ReusesOpenIntent()
        {
            await SeedOnboardingCompletePendingAsync();

            await _signup.ChoosePlanAsync(_sessionToken, "Growth", "monthly");
            var second = await _signup.ChoosePlanAsync(
                _sessionToken,
                "Growth",
                "monthly"
            );

            Assert.Equal("checkout", second.Mode);
            Assert.Equal(1, _merchant.CreateOrderCallCount);
            Assert.Equal(1, await _db.RevolutOrderIntents.CountAsync());
        }

        [Fact]
        public async Task Webhook_SignupPlan_ProvisionsPaidAccount_Idempotent()
        {
            await SeedOnboardingCompletePendingAsync();
            var choose = await _signup.ChoosePlanAsync(
                _sessionToken,
                "Growth",
                "monthly"
            );
            Assert.Equal("checkout", choose.Mode);

            var intent = await _db.RevolutOrderIntents.SingleAsync();
            var applier = CreateSignupApplier();

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: intent.OrderId,
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

            await _db.Entry(_pending).ReloadAsync();
            Assert.Equal(PendingSignupStatuses.Complete, _pending.Status);
            Assert.True(await _db.Users.AnyAsync(u => u.Email == _email));

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Growth,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Active, billing.BillingStatus);
            Assert.Equal(BillingCycles.Monthly, billing.BillingCycle);
            Assert.False(
                (await _db.RevolutOrderIntents.SingleAsync()).IsOpen
            );

            await applier.ApplyAsync(
                new RevolutOrderCompletedApplyRequest(
                    OrderId: intent.OrderId,
                    OrderState: "completed",
                    BillingReason: null,
                    SubscriptionId: null,
                    RawWebhookBody: "{}",
                    RawOrderBody: "{}"
                )
            );

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
            await _signup.ChoosePlanAsync(_sessionToken, "Pilot", "monthly");

            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);

            Assert.Equal(PendingSignupStatuses.Complete, status.Status);
            Assert.True(status.Ready);
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

        private RevolutOrderCompletedApplier CreateSignupApplier()
        {
            return new RevolutOrderCompletedApplier(
                _db,
                new NoOpIncludedPeriodMint(),
                new NoOpVatInvoices(),
                new PlanChangeService(_db, _pricebook, _clock),
                new ExtraGroupLocationService(
                    _db,
                    _pricebook,
                    new AlwaysReadyRevolutMerchantCreateGate(),
                    _merchant,
                    _configuration,
                    _clock
                ),
                new CreditLedgerService(_db, _clock, _pricebook),
                _merchant,
                _clock,
                _provisioning
            );
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

        public void Dispose()
        {
            _db.Dispose();
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

        private sealed class RecordingSignupMerchant : IRevolutMerchantClient
        {
            public const string CheckoutUrl =
                "https://checkout.revolut.com/payment-link/fake-signup";

            public int CreateOrderCallCount { get; private set; }

            public RevolutCreateOrderRequest? LastCreateOrderRequest
            {
                get;
                private set;
            }

            public void EnsureReadyForCreate(
                string? planVariationLookupKey = null
            )
            {
            }

            public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
                string email,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutListCustomersResult(Succeeded: true)
                );

            public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
                RevolutCreateCustomerRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: "cust_signup"
                    )
                );

            public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
                RevolutCreateSubscriptionRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: "sub_signup"
                    )
                );

            public Task<RevolutMerchantCreateResult> CreateOrderAsync(
                RevolutCreateOrderRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CreateOrderCallCount++;
                LastCreateOrderRequest = request;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: $"ord_signup_{CreateOrderCallCount}",
                        CheckoutUrl: CheckoutUrl
                    )
                );
            }

            public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
                string subscriptionId,
                string planVariationLookupKey,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );

            public Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );

            public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
                string subscriptionId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: subscriptionId
                    )
                );

            public Task<RevolutOrderRetrieveResult> GetOrderAsync(
                string orderId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutOrderRetrieveResult(
                        Succeeded: true,
                        Id: orderId,
                        State: "pending",
                        CheckoutUrl: CheckoutUrl
                    )
                );

            public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
                string orderId,
                string merchantReference,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: true,
                        Id: orderId
                    )
                );
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class NoOpIncludedPeriodMint : IIncludedPeriodMintService
        {
            public Task<IncludedPeriodMintResult> MintOnOrderCompletedAsync(
                IncludedPeriodOrderCompletedRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    IncludedPeriodMintResult.Skipped("signup_plan_no_mint")
                );

            public Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    IncludedPeriodMintResult.Skipped("signup_plan_no_mint")
                );

            public Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
                int restaurantId,
                DateTime? nowUtc,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    IncludedPeriodMintResult.Skipped("signup_plan_no_mint")
                );
        }

        private sealed class NoOpVatInvoices : ITummlyVatInvoiceService
        {
            public Task<TummlyVatInvoice> MintForCompletedOrderAsync(
                TummlyVatInvoiceMintRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
                TummlyVatCreditNoteMintRequest request,
                CancellationToken cancellationToken = default
            ) => throw new NotImplementedException();

            public Task<TummlyVatInvoice?> FindByRevolutOrderIdAsync(
                string revolutOrderId,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<TummlyVatInvoice?>(null);

            public Task<IReadOnlyList<InvoiceRowDto>> ListInvoiceRowsForRestaurantAsync(
                int restaurantId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult<IReadOnlyList<InvoiceRowDto>>([]);

            public Task<(byte[] Content, string FileName)?> RenderPdfAsync(
                int restaurantId,
                string documentNumber,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<(byte[] Content, string FileName)?>(null);
        }
    }
}
