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
        // Starts failing, as a checkout that did not open; a test sets FailCreateOrder = false to let it open.
        private readonly CountingCustomCycleMerchant _merchant =
            new() { FailCreateOrder = true };
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
                smartGuestLink,
                TestActiveQrCapacity.For(_db)
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
                    _merchant,
                    TimeProvider.System,
                    Microsoft.Extensions.Options.Options.Create(
                        new TummlyBackend.Configurations.TummlySellerVatSettings()
                    ),
                    new TummlyBackend.Services.CustomPlanQuoteService(
                        _db,
                        Microsoft.Extensions.Options.Options.Create(new TummlyBackend.Configurations.TummlySellerVatSettings())
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
            var quote = await QuoteAsync(
                "monthly",
                "growth",
                2,
                new Dictionary<string, int> { ["offer"] = 1 }
            );
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 2,
                    packs = new Dictionary<string, int> { ["offer"] = 1 },
                    quoteHash = quote.QuoteHash,
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
            var quote = await QuoteAsync(
                "monthly",
                "growth",
                2,
                new Dictionary<string, int>()
            );
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 2,
                    packs = new Dictionary<string, int>(),
                    quoteHash = quote.QuoteHash,
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
            var quote = await QuoteAsync(
                "monthly",
                "group",
                31,
                new Dictionary<string, int>()
            );
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
            var quote = await QuoteAsync(
                "annual",
                "group",
                500,
                new Dictionary<string, int>()
            );
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "annual",
                    basePlan = "group",
                    locations = 500,
                    packs = new Dictionary<string, int>(),
                    quoteHash = quote.QuoteHash,
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
            var quote = await QuoteAsync(
                "monthly",
                "group",
                30,
                new Dictionary<string, int>()
            );
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "group",
                    locations = 30,
                    packs = new Dictionary<string, int>(),
                    quoteHash = quote.QuoteHash,
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
        public async Task SaveOnboarding_NoQuoteHash_ReturnsQuoteChanged()
        {
            await SeedVerifiedPendingAsync();
            await QuoteGroupAsync(31);

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

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("quote_changed", ex.Code);
            await _db.Entry(_pending).ReloadAsync();
            Assert.Equal(PendingSignupStatuses.Verified, _pending.Status);
            Assert.Null(_pending.ChosenPlan);
            Assert.Null(_pending.CustomPlanConfigJson);
        }

        [Fact]
        public async Task SaveOnboarding_EmptyQuoteHash_ReturnsQuoteChanged()
        {
            await SeedVerifiedPendingAsync();
            await QuoteGroupAsync(31);

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
                    quoteHash = "",
                }
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("quote_changed", ex.Code);
            await _db.Entry(_pending).ReloadAsync();
            Assert.Null(_pending.ChosenPlan);
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
        public async Task SaveOnboarding_PilotWithCapacityUnits_IsRejected()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Pilot;
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "starter",
                    locations = 1,
                    packs = new Dictionary<string, int> { ["team_user"] = 2 },
                }
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("pilot_no_capacity_addons", ex.Code);
            Assert.False(await _db.BillingAccounts.AnyAsync());
        }

        [Fact]
        public async Task SaveOnboarding_LegacyCapacityPack_IsRejectedNotConverted()
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
                    basePlan = "starter",
                    locations = 1,
                    packs = new Dictionary<string, int> { ["team_users_5"] = 1 },
                    quoteHash = "0123456789abcdef0123456789abcdef",
                }
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.SaveOnboardingAsync(_sessionToken, dto)
            );
            Assert.Equal("legacy_pack_key", ex.Code);
            await _db.Entry(_pending).ReloadAsync();
            Assert.Null(_pending.CustomPlanConfigJson);
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
        public async Task RetryProvision_PremisesAlreadyRegistered_SavesDraftForReview_NoPilotNoKitNoQr()
        {
            var theirOwner = new User
            {
                FullName = "Their Owner",
                Email = "their-owner@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(theirOwner);
            await _db.SaveChangesAsync();
            var theirs = new Restaurant
            {
                Name = "Their Kitchen",
                AccountType = "Single",
                OwnerUserId = theirOwner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Restaurants.Add(theirs);
            await _db.SaveChangesAsync();
            _db.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = theirs.Id,
                    LocationName = "Their Main",
                    Address = "1 High Street",
                    City = "Leeds",
                    Postcode = "LS1 1AA",
                    LifecycleStatus = LocationLifecycleStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            await SeedOnboardingCompletePendingWithPlanAsync(
                BillingSubscriptionPlans.Pilot
            );

            await _signup.RetryProvisionAsync(_sessionToken);

            var location = await _db.RestaurantLocations.SingleAsync(l =>
                l.LocationName == "Main"
            );
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
            Assert.Equal(PremisesReviewStatus.PendingReview, location.PremisesReviewStatus);
            Assert.Empty(
                await _db.QrCodes
                    .Where(qr => qr.RestaurantLocationId == location.Id)
                    .ToListAsync()
            );
            var billing = await _db.BillingAccounts.SingleAsync(b =>
                b.RestaurantId == location.RestaurantId
            );
            Assert.Null(billing.PilotStartedAtUtc);
            Assert.Equal(0, _mintLedger.MintPilotCallCount);
            Assert.Equal(
                0,
                await _db.ShopOrders.CountAsync(o => o.IsComplimentary)
            );
            Assert.Equal(0, _pilotEmail.PilotStartedCalls);
            Assert.Equal(
                1,
                await _db.AdminAuditEvents.CountAsync(e =>
                    e.Action == AdminAuditActions.LocationPremisesFlagged
                )
            );
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

        [Fact]
        public async Task RetryProvision_CompletedSignupSavedWithLegacyPack_StartsNoOrder()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            await ReplaceSavedPacksAsync(new Dictionary<string, int> { ["team_users_5"] = 1 });
            _merchant.FailCreateOrder = false;
            var ordersBefore = _merchant.CreateOrderCallCount;

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.RetryProvisionAsync(_sessionToken)
            );

            Assert.Equal("legacy_pack_key", ex.Code);
            Assert.Equal(ordersBefore, _merchant.CreateOrderCallCount);
            await AssertStillFreeWithNoCustomConfigAsync();
        }

        [Fact]
        public async Task RetryProvision_CompletedSignupWithChangedPrice_StartsNoOrder()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            var offerRate = await _db.CommercialCustomUnitRateRows.SingleAsync(r =>
                r.RateKey == CommercialCustomUnitRateKeys.Offer
            );
            offerRate.MonthlyNetPence += 100;
            await _db.SaveChangesAsync();
            _merchant.FailCreateOrder = false;
            var ordersBefore = _merchant.CreateOrderCallCount;

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.RetryProvisionAsync(_sessionToken)
            );

            Assert.Equal("quote_changed", ex.Code);
            Assert.Equal(ordersBefore, _merchant.CreateOrderCallCount);
            await AssertStillFreeWithNoCustomConfigAsync();
        }

        [Fact]
        public async Task RetryProvision_CompletedSignupWithUnchangedQuote_OpensCheckoutOnce()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            _merchant.FailCreateOrder = false;
            var ordersBefore = _merchant.CreateOrderCallCount;

            await _signup.RetryProvisionAsync(_sessionToken);

            Assert.Equal(ordersBefore + 1, _merchant.CreateOrderCallCount);
            var intent = await _db.RevolutOrderIntents.SingleAsync();
            Assert.Equal(RevolutOrderIntentPurposes.CustomPlanCycle, intent.Purpose);
        }

        [Fact]
        public async Task RetryProvision_FirstProvisionWithLegacyPackConfig_StartsNoOrderAndRecordsTheCode()
        {
            await SeedOnboardingCompletePendingWithPlanAsync(BillingSubscriptionPlans.Custom);
            _pending.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "starter",
                    locations = 1,
                    packs = new Dictionary<string, int> { ["qr_10"] = 1 },
                    quoteHash = "0123456789abcdef0123456789abcdef",
                }
            );
            await _db.SaveChangesAsync();
            _merchant.FailCreateOrder = false;

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.RetryProvisionAsync(_sessionToken)
            );

            Assert.Equal("legacy_pack_key", ex.Code);
            Assert.Equal(0, _merchant.CreateOrderCallCount);
            await AssertStillFreeWithNoCustomConfigAsync();
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal("legacy_pack_key", billing.PendingPaymentFailureCode);
        }

        [Fact]
        public async Task ProvisioningStatus_StaleQuoteFailure_CarriesTheSavedPlanForReview()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            await MovePriceAndRetryAsync();

            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);

            Assert.Equal(PendingSignupStatuses.PaymentFailed, status.Status);
            Assert.Equal("quote_changed", status.PaymentFailureCode);
            Assert.NotNull(status.SavedPlan);
            Assert.Equal(BillingSubscriptionPlans.Custom, status.SavedPlan!.ChosenPlan);
            Assert.Equal("monthly", status.SavedPlan.ChosenCadence);
            Assert.Equal(_pending.CustomPlanConfigJson, status.SavedPlan.CustomPlanConfigJson);
        }

        [Fact]
        public async Task ProvisioningStatus_OtherCheckoutFailure_CarriesNoSavedPlan()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();

            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);

            Assert.Equal(PendingSignupStatuses.PaymentFailed, status.Status);
            Assert.Null(status.SavedPlan);
        }

        [Fact]
        public async Task UpdatePlan_FreshQuote_AfterPriceMoved_SavesItAndOpensCheckoutOnce()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            await MovePriceAndRetryAsync();
            var fresh = await QuoteAsync("monthly", "growth", 2, new() { ["offer"] = 2 });
            var ordersBefore = _merchant.CreateOrderCallCount;

            await _signup.UpdatePlanAfterFailedCheckoutAsync(
                _sessionToken,
                UpdateDtoFor(fresh)
            );

            Assert.Equal(ordersBefore + 1, _merchant.CreateOrderCallCount);
            await _db.Entry(_pending).ReloadAsync();
            Assert.Equal(
                fresh.QuoteHash,
                CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson)!.QuoteHash
            );
            var status = await _signup.GetProvisioningStatusAsync(_sessionToken);
            Assert.Equal(PendingSignupStatuses.AwaitingPayment, status.Status);
            Assert.False(string.IsNullOrWhiteSpace(status.PaymentRedirectUrl));
        }

        [Fact]
        public async Task UpdatePlan_StillStaleHash_StartsNoOrderAndKeepsTheSavedPlan()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            var stale = await QuoteAsync("monthly", "growth", 2, new() { ["offer"] = 2 });
            await MovePriceAndRetryAsync();
            var savedBefore = _pending.CustomPlanConfigJson;
            var ordersBefore = _merchant.CreateOrderCallCount;

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.UpdatePlanAfterFailedCheckoutAsync(
                    _sessionToken,
                    UpdateDtoFor(stale)
                )
            );

            Assert.Equal("quote_changed", ex.Code);
            Assert.Equal(ordersBefore, _merchant.CreateOrderCallCount);
            await _db.Entry(_pending).ReloadAsync();
            Assert.Equal(savedBefore, _pending.CustomPlanConfigJson);
        }

        [Fact]
        public async Task UpdatePlan_RetiredPackChosenAgain_StartsNoOrder()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            var ordersBefore = _merchant.CreateOrderCallCount;
            var dto = new UpdateSignupPlanDto
            {
                ChosenPlan = BillingSubscriptionPlans.Custom,
                ChosenCadence = "monthly",
                CustomPlanConfigJson = JsonSerializer.Serialize(
                    new
                    {
                        version = 2,
                        cadence = "monthly",
                        basePlan = "growth",
                        locations = 2,
                        packs = new Dictionary<string, int> { ["qr_10"] = 1 },
                        quoteHash = "0123456789abcdef0123456789abcdef",
                    }
                ),
            };

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.UpdatePlanAfterFailedCheckoutAsync(_sessionToken, dto)
            );

            Assert.Equal("legacy_pack_key", ex.Code);
            Assert.Equal(ordersBefore, _merchant.CreateOrderCallCount);
        }

        [Fact]
        public async Task UpdatePlan_ChoseAgainWithoutTheRetiredPack_OpensCheckoutForTheNewQuote()
        {
            await SaveCustomOnboardingWithFailedCheckoutAsync();
            await ReplaceSavedPacksAsync(new Dictionary<string, int> { ["qr_10"] = 1 });
            var retry = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.RetryProvisionAsync(_sessionToken)
            );
            Assert.Equal("legacy_pack_key", retry.Code);
            _merchant.FailCreateOrder = false;
            var fresh = await QuoteAsync("monthly", "growth", 3, new() { ["team_user"] = 2 });
            var ordersBefore = _merchant.CreateOrderCallCount;

            await _signup.UpdatePlanAfterFailedCheckoutAsync(
                _sessionToken,
                UpdateDtoFor(fresh)
            );

            Assert.Equal(ordersBefore + 1, _merchant.CreateOrderCallCount);
            var intent = await _db.RevolutOrderIntents.SingleAsync();
            Assert.Equal(fresh.PeriodNetPence, intent.NetAmountMinor);
        }

        [Fact]
        public async Task UpdatePlan_BeforeAnyFailedCheckout_IsRejected()
        {
            await SeedVerifiedPendingAsync();
            var fresh = await QuoteAsync("monthly", "growth", 2, new() { ["offer"] = 2 });

            await Assert.ThrowsAsync<SignupFlowException>(() =>
                _signup.UpdatePlanAfterFailedCheckoutAsync(
                    _sessionToken,
                    UpdateDtoFor(fresh)
                )
            );
        }

        private async Task MovePriceAndRetryAsync()
        {
            var offerRate = await _db.CommercialCustomUnitRateRows.SingleAsync(r =>
                r.RateKey == CommercialCustomUnitRateKeys.Offer
            );
            offerRate.MonthlyNetPence += 100;
            await _db.SaveChangesAsync();
            _merchant.FailCreateOrder = false;
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _signup.RetryProvisionAsync(_sessionToken)
            );
            Assert.Equal("quote_changed", ex.Code);
            await _db.Entry(_pending).ReloadAsync();
        }

        private static UpdateSignupPlanDto UpdateDtoFor(CustomPlanQuoteResult quote) =>
            new()
            {
                ChosenPlan = quote.StoredAsPlan,
                ChosenCadence = quote.Cadence,
                CustomPlanConfigJson = quote.ToConfig().ToJson(),
            };

        private async Task SaveCustomOnboardingWithFailedCheckoutAsync()
        {
            await SeedVerifiedPendingAsync();
            var dto = BuildValidOnboardingDto();
            dto.ChosenPlan = BillingSubscriptionPlans.Custom;
            dto.ChosenCadence = "monthly";
            var packs = new Dictionary<string, int> { ["offer"] = 2 };
            var quote = await QuoteAsync("monthly", "growth", 2, packs);
            dto.CustomPlanConfigJson = JsonSerializer.Serialize(
                new
                {
                    version = 2,
                    cadence = "monthly",
                    basePlan = "growth",
                    locations = 2,
                    packs,
                    quoteHash = quote.QuoteHash,
                }
            );
            await _signup.SaveOnboardingAsync(_sessionToken, dto);
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Custom, billing.PendingPaymentPlan);
            Assert.False(string.IsNullOrWhiteSpace(billing.PendingPaymentFailureCode));
            await _db.Entry(_pending).ReloadAsync();
        }

        private async Task ReplaceSavedPacksAsync(Dictionary<string, int> packs)
        {
            var saved = CustomPlanConfig.TryParse(_pending.CustomPlanConfigJson)!;
            saved.Packs = packs;
            _pending.CustomPlanConfigJson = saved.ToJson();
            await _db.SaveChangesAsync();
        }

        private async Task AssertStillFreeWithNoCustomConfigAsync()
        {
            _db.ChangeTracker.Clear();
            var billing = await _db.BillingAccounts.SingleAsync();
            Assert.Equal(BillingSubscriptionPlans.Free, billing.SubscriptionPlan);
            Assert.Null(billing.CustomPlanConfigJson);
            Assert.Empty(await _db.RevolutOrderIntents.ToListAsync());
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

        private async Task<CustomPlanQuoteResult> QuoteAsync(
            string cadence,
            string basePlan,
            int locations,
            Dictionary<string, int> packs
        ) =>
            await new CustomPlanQuoteService(_db, Options.Create(new TummlySellerVatSettings())).QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = cadence,
                    BasePlan = basePlan,
                    Locations = locations,
                    Packs = packs.ToDictionary(
                        kv => kv.Key,
                        kv => (decimal)kv.Value,
                        StringComparer.Ordinal
                    ),
                }
            );

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
