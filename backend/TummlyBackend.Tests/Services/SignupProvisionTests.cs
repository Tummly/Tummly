using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
﻿using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
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
        private readonly RecordingFirstPaidConversionPaySession _firstPaid;
        private readonly string _email = "pilot-owner@example.com";
        private Guid _sessionToken;
        private PendingSignup _pending = null!;

        private readonly RecordingPilotStartedEmailService _pilotEmail = new();

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
            _firstPaid = new RecordingFirstPaidConversionPaySession(_db);
            _provisioning = new GuestLoopProvisioningService(
                _db,
                qrCodeProvisioning,
                new NoOpPrintReadyQrMaterialsWork(),
                _configuration,
                _pricebook,
                new NoOpBillingAccountLifecycle(),
                _firstPaid,
                new CustomPlanCycleBillingService(
                    _db,
                    new NoOpRevolutMerchantForCustomPlan(),
                    TimeProvider.System,
                    Microsoft.Extensions.Options.Options.Create(
                        new TummlyBackend.Configurations.TummlySellerVatSettings()
                    )
                ),
                PilotStartTestFactory.Create(
                    _db,
                    _pricebook,
                    _mintLedger,
                    new ComplimentaryStarterShopOrderService(
                        _db,
                        SeedDbMaterialsCatalog(_db),
                        new ShopOrderNumberAllocator(_db)
                    ),
                    new NoOpPrintReadyQrMaterialsWork(),
                    _pilotEmail
                ),
                NullLogger<GuestLoopProvisioningService>.Instance
            );

            var snapshot = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
            _db.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
            );
            _db.SaveChanges();

            _signup = new SignupService(
                _db,
                new TrackingEmailService(),
                _provisioning,
                new CustomPlanQuoteService(_db, Options.Create(new TummlySellerVatSettings())),
                new StubJwtService(),
                new OtpChallengeService(
                    _db,
                    new OtpCodeHasher("tests-only-otp-hash-key"),
                    TimeProvider.System
                ),
                TimeProvider.System,
                NullLogger<SignupService>.Instance
            );
        }

        [Fact]
        public async Task SaveOnboarding_CustomPlan_CheckoutFails_StaysFreeAndAudits()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Custom;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 2,
                    packs = new Dictionary<string, int> { ["offers_5"] = 1 },
                }
            );

            var result = await _signup.SaveOnboardingAsync(_sessionToken, dto);

            Assert.Equal(PendingSignupStatuses.Complete, result.Status);
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Free, billing.SubscriptionPlan);
            Assert.Null(billing.CustomPlanConfigJson);
            Assert.Null(billing.BillingCycle);
            Assert.Equal(BillingSubscriptionPlans.Custom, billing.PendingPaymentPlan);
            Assert.NotNull(billing.PendingPaymentCustomConfigJson);
            var audit = await _db.AdminAuditEvents.SingleAsync();
            Assert.Equal(AdminAuditActions.BillingCustomSignupCheckoutFailed, audit.Action);
            Assert.False(audit.Succeeded);
            Assert.Equal(billing.RestaurantId, audit.RestaurantId);
        }

        [Fact]
        public async Task SaveOnboarding_MissingPlan_ThrowsChoosePlan()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = null;

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal(SignupService.ChoosePlanMessage, ex.Message);
        }

        [Fact]
        public async Task SaveOnboarding_CustomV2_NoPacks_CollapsesToGrowth()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Custom;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 2,
                    packs = new Dictionary<string, int>(),
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Growth, _pending.ChosenPlan);
            Assert.Null(_pending.CustomPlanConfigJson);
        }

        [Fact]
        public async Task SaveOnboarding_Group31Locations_SavedAsCustomWithBandedAmount()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Group;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 31,
                    packs = new Dictionary<string, int>(),
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Custom, _pending.ChosenPlan);
            var config = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson);
            Assert.NotNull(config);
            Assert.Equal(31, config!.Locations);
            Assert.Equal(121100, config.PeriodNetPence);
        }

        [Fact]
        public async Task SaveOnboarding_Custom500Annual_PeriodNetPence()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Custom;
            dto.ChosenCadence = "annual";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "annual",
                    basePlan = "group",
                    locations = 500,
                    packs = new Dictionary<string, int>(),
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Custom, _pending.ChosenPlan);
            var config = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson);
            Assert.NotNull(config);
            Assert.Equal(500, config!.Locations);
            Assert.Equal(16691800, config.PeriodNetPence);
        }

        [Fact]
        public async Task SaveOnboarding_Group30Locations_SavedAsCustomWithBandedAmount()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Group;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 30,
                    packs = new Dictionary<string, int>(),
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Custom, _pending.ChosenPlan);
            var config = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson);
            Assert.NotNull(config);
            Assert.Equal(30, config!.Locations);
            Assert.Equal(117400, config.PeriodNetPence);
        }

        [Fact]
        public async Task SaveOnboarding_MatchingQuoteHash_Saves()
        {
            await SeedVerifiedPendingAsync();
            var quote = await QuoteGroupAsync(31);
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Group;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 31,
                    packs = new Dictionary<string, int>(),
                    quoteHash = quote.QuoteHash,
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Custom, _pending.ChosenPlan);
            var config = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson);
            Assert.NotNull(config);
            Assert.Equal(quote.PeriodNetPence, config!.PeriodNetPence);
        }

        [Fact]
        public async Task SaveOnboarding_StaleQuoteHash_ReturnsQuoteChanged()
        {
            await SeedVerifiedPendingAsync();
            var staleQuote = await QuoteGroupAsync(31);
            var band2 = _db.CommercialCustomUnitRateRows.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.ExtraLocationBand2
            );
            band2.MonthlyNetPence = 3600;
            await _db.SaveChangesAsync();

            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Group;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 31,
                    packs = new Dictionary<string, int>(),
                    quoteHash = staleQuote.QuoteHash,
                }
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("quote_changed", ex.Code);
            Assert.Equal(
                "Your quote has changed. Review the updated total before continuing.",
                ex.Message
            );
            await _db.Entry(_pending).ReloadAsync();
            Assert.Equal(PendingSignupStatuses.Verified, _pending.Status);
            Assert.Null(_pending.ChosenPlan);
            Assert.Null(_pending.CustomPlanConfigJson);
        }

        [Fact]
        public async Task SaveOnboarding_NoQuoteHash_RequotesWithoutReject()
        {
            await SeedVerifiedPendingAsync();
            var band2 = _db.CommercialCustomUnitRateRows.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.ExtraLocationBand2
            );
            band2.MonthlyNetPence = 3600;
            await _db.SaveChangesAsync();
            var currentQuote = await QuoteGroupAsync(31);

            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Group;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 31,
                    packs = new Dictionary<string, int>(),
                }
            );

            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            await _db.Entry(_pending).ReloadAsync();

            Assert.Equal(BillingSubscriptionPlans.Custom, _pending.ChosenPlan);
            var config = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson);
            Assert.NotNull(config);
            Assert.Equal(currentQuote.PeriodNetPence, config!.PeriodNetPence);
        }

        [Fact]
        public async Task SaveOnboarding_InvalidLocations_ThrowsInvalidLocationCount()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Custom;
            dto.ChosenCadence = "monthly";
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 0,
                    packs = new Dictionary<string, int>(),
                }
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("invalid_location_count", ex.Code);
        }

        [Fact]
        public async Task SaveOnboarding_Verified_ProvisionsPilotAndCompletes()
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
            Assert.Equal(BillingSubscriptionPlans.Pilot, _pending.ChosenPlan);
            Assert.Equal("monthly", _pending.ChosenCadence);
            Assert.Equal("Single", _pending.AccountType);

            var user = await _db.Users.SingleAsync(u => u.Email == _email);
            Assert.NotNull(user.ActivatedAt);
            Assert.NotNull(user.ActivationExpiresAt);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(
                BillingSubscriptionPlans.Pilot,
                billing.SubscriptionPlan
            );
            Assert.Equal(BillingStatuses.Pilot, billing.BillingStatus);
            Assert.True(_mintLedger.MintPilotCallCount >= 1);
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
            Assert.Equal(BillingSubscriptionPlans.Pilot, _pending.ChosenPlan);

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
            Assert.Equal(
                1,
                await _db.ShopOrders.CountAsync(o => o.IsComplimentary)
            );
            Assert.Equal(1, _pilotEmail.PilotStartedCalls);

            await _signup.RetryProvisionAsync(_sessionToken);

            Assert.Equal(1, _pilotEmail.PilotStartedCalls);
        }

        [Fact]
        public async Task RetryProvision_CompletePilotWithoutStart_StartsPilot()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Pilot
            );
            await _signup.RetryProvisionAsync(_sessionToken);
            Assert.Equal(1, _pilotEmail.PilotStartedCalls);

            // Simulate a Complete signup whose Pilot start never ran.
            var billing = await _db.BillingAccounts.SingleAsync();
            billing.PilotStartedAtUtc = null;
            billing.PilotPeriodEnd = null;
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingStatus = BillingStatuses.Free;
            var owner = await _db.Users.SingleAsync(u => u.Email == _email);
            owner.ActivationExpiresAt = null;
            await _db.SaveChangesAsync();
            var mintsBefore = _mintLedger.MintPilotCallCount;

            await _signup.RetryProvisionAsync(_sessionToken);

            billing = await _db.BillingAccounts.AsNoTracking().SingleAsync();
            Assert.NotNull(billing.PilotStartedAtUtc);
            Assert.Equal(BillingSubscriptionPlans.Pilot, billing.SubscriptionPlan);
            Assert.Equal(mintsBefore + 1, _mintLedger.MintPilotCallCount);
            // The simulated reset made this a real second start: one more email.
            Assert.Equal(2, _pilotEmail.PilotStartedCalls);
            var startedAt = billing.PilotStartedAtUtc;

            await _signup.RetryProvisionAsync(_sessionToken);

            billing = await _db.BillingAccounts.AsNoTracking().SingleAsync();
            Assert.Equal(startedAt, billing.PilotStartedAtUtc);
            Assert.Equal(mintsBefore + 1, _mintLedger.MintPilotCallCount);
            Assert.Equal(2, _pilotEmail.PilotStartedCalls);
        }

        [Fact]
        public async Task RetryProvision_ChosenPlanGrowth_CheckoutFails_StaysFree_AndAudits()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Growth
            );
            _firstPaid.FailWithCode = "revolut_http_error";

            await _signup.RetryProvisionAsync(_sessionToken);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Free, billing.SubscriptionPlan);
            Assert.Empty(await _db.RevolutPendingPaySessions.ToListAsync());
            Assert.Equal(BillingSubscriptionPlans.Growth, billing.PendingPaymentPlan);
            Assert.Equal("revolut_http_error", billing.PendingPaymentFailureCode);

            var audit = await _db.AdminAuditEvents.SingleAsync();
            Assert.Equal(AdminAuditActions.BillingPaidSignupCheckoutFailed, audit.Action);
            Assert.False(audit.Succeeded);
            Assert.Equal(billing.RestaurantId, audit.RestaurantId);
            Assert.Contains("revolut_http_error", audit.DetailJson);
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
                0,
                await _db.ShopOrders.CountAsync(o => o.IsComplimentary)
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
            Assert.Equal(
                "https://tummly.example/signup/complete",
                _firstPaid.LastSuccessRedirectUrl
            );
        }

        [Fact]
        public async Task ProvisionFromPending_PaidPlan_NoActivationExpiry()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Starter
            );

            await _signup.RetryProvisionAsync(_sessionToken);

            var user = await _db.Users.SingleAsync(u => u.Email == _email);
            Assert.NotNull(user.ActivatedAt);
            Assert.Null(user.ActivationExpiresAt);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Null(billing.PilotStartedAtUtc);
            Assert.Null(billing.PilotPeriodEnd);
            Assert.Equal(0, _mintLedger.MintPilotCallCount);
        }

        [Fact]
        public async Task ProvisionFromPending_FreePlan_NoActivationExpiry()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Free
            );

            await _signup.RetryProvisionAsync(_sessionToken);

            var user = await _db.Users.SingleAsync(u => u.Email == _email);
            Assert.NotNull(user.ActivatedAt);
            Assert.Null(user.ActivationExpiresAt);

            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Null(billing.PilotStartedAtUtc);
            Assert.Null(billing.PilotPeriodEnd);
            Assert.Equal(0, _mintLedger.MintPilotCallCount);
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
                ChosenPlan = BillingSubscriptionPlans.Pilot,
                ChosenCadence = "monthly",
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

        private async Task<CustomPlanQuoteResult> QuoteGroupAsync(int locations) =>
            await new CustomPlanQuoteService(_db, Options.Create(new TummlySellerVatSettings())).QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = locations,
                }
            );

        private async Task SeedVerifiedPendingAsync()
        {
            _sessionToken = Guid.NewGuid();
            _pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = _sessionToken,
                SessionTokenIssuedAtUtc = DateTime.UtcNow,
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
                SessionTokenIssuedAtUtc = DateTime.UtcNow,
                Email = _email,
                Status = PendingSignupStatuses.OnboardingComplete,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-10),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FullName = "Pilot Owner",
                AccountType = "Single",
                ChosenPlan = BillingSubscriptionPlans.Pilot,
                ChosenCadence = "monthly",
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

        private static DbMaterialsCatalog SeedDbMaterialsCatalog(
            ApplicationDbContext context
        )
        {
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(context)
                .GetAwaiter()
                .GetResult();
            return new DbMaterialsCatalog(context);
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
            public override Task SendOtpEmailAsync(
                string toEmail,
                string otp,
                string? restaurantName = null
            ) =>
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
