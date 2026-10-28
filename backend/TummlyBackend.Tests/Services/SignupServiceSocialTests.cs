using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using Microsoft.Extensions.Logging.Abstractions;
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
    public class SignupServiceSocialTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly SignupService _signup;
        private readonly GuestLoopProvisioningService _provisioning;
        private readonly IPricebookCatalog _pricebook;
        private readonly IConfiguration _configuration;

        public SignupServiceSocialTests()
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
            var snapshot = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
            _db.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
            );
            _db.SaveChanges();

            _provisioning = new GuestLoopProvisioningService(
                _db,
                qrCodeProvisioning,
                new NoOpPrintReadyQrMaterialsWork(),
                _configuration,
                _pricebook,
                new NoOpBillingAccountLifecycle(),
                new NoOpFirstPaidConversionPaySession(),
                new CustomPlanCycleBillingService(
                    _db,
                    new NoOpRevolutMerchantForCustomPlan(),
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
                    new NoOpCreditLedger(),
                    new ComplimentaryStarterShopOrderService(
                        _db,
                        SeedDbMaterialsCatalog(_db),
                        new ShopOrderNumberAllocator(_db)
                    ),
                    new NoOpPrintReadyQrMaterialsWork(),
                    new EmailServiceStubBase()
                ),
                NullLogger<GuestLoopProvisioningService>.Instance
            );

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
        public async Task SaveOnboarding_SocialPending_AllowsEmptyPassword_AndProvisions()
        {
            var email = "social-owner@example.com";
            var sessionToken = Guid.NewGuid();
            var pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = sessionToken,
                SessionTokenIssuedAtUtc = DateTime.UtcNow,
                Email = email,
                Status = PendingSignupStatuses.Verified,
                OtpResendCount = 0,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                PasswordHash = null,
                AuthProvider = ExternalAuthProviders.Google,
                ProviderSubject = "sub-1",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            };
            _db.PendingSignups.Add(pending);
            await _db.SaveChangesAsync();

            var result = await _signup.SaveOnboardingAsync(
                sessionToken,
                BuildOnboardingDto(password: "", confirmPassword: "")
            );

            await _db.Entry(pending).ReloadAsync();

            Assert.Equal(PendingSignupStatuses.Complete, result.Status);
            Assert.Equal(PendingSignupStatuses.Complete, pending.Status);

            var user = await _db.Users.SingleAsync(u => u.Email == email);
            Assert.Null(user.PasswordHash);

            var login = await _db.UserExternalLogins.SingleAsync(x =>
                x.UserId == user.Id
            );
            Assert.Equal(ExternalAuthProviders.Google, login.Provider);
            Assert.Equal("sub-1", login.ProviderSubject);
        }

        [Fact]
        public async Task ProvisionFromPending_SocialMissingProviderSubject_Throws()
        {
            var email = "social-no-subject@example.com";
            var pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = Guid.NewGuid(),
                SessionTokenIssuedAtUtc = DateTime.UtcNow,
                Email = email,
                Status = PendingSignupStatuses.Verified,
                OtpResendCount = 0,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                PasswordHash = null,
                AuthProvider = ExternalAuthProviders.Google,
                ProviderSubject = null,
                FullName = "Social Owner",
                AccountType = "Single",
                OnboardingJson = """
                    {
                      "fullName": "Social Owner",
                      "groupName": "Social Kitchen",
                      "businessCategory": "takeaway",
                      "primaryPhone": "07911123456",
                      "locations": [
                        {
                          "locationName": "Main",
                          "address": "1 High Street",
                          "city": "Leeds",
                          "postcode": "LS1 1AA"
                        }
                      ]
                    }
                    """,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            };
            _db.PendingSignups.Add(pending);
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _provisioning.ProvisionFromPendingAsync(pending.Id)
            );

            Assert.Equal(
                "Pending signup is missing a provider subject.",
                ex.Message
            );
            Assert.False(await _db.Users.AnyAsync(u => u.Email == email));
            Assert.False(await _db.UserExternalLogins.AnyAsync());
        }

        [Fact]
        public async Task ProvisionFromPending_ExistingSocialUserMissingLink_AddsUserExternalLogin()
        {
            var email = "social-retry@example.com";
            var existing = new User
            {
                FullName = "Existing Social",
                Email = email,
                PasswordHash = null,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(existing);
            await _db.SaveChangesAsync();

            var onboarding = new SignupOnboardingPayload
            {
                FullName = "Existing Social",
                GroupName = "Retry Kitchen",
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

            var pending = new PendingSignup
            {
                Id = Guid.NewGuid(),
                SessionToken = Guid.NewGuid(),
                SessionTokenIssuedAtUtc = DateTime.UtcNow,
                Email = email,
                Status = PendingSignupStatuses.Verified,
                TermsAccepted = true,
                EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                PasswordHash = null,
                AuthProvider = ExternalAuthProviders.Google,
                ProviderSubject = "sub-retry",
                FullName = "Existing Social",
                AccountType = "Single",
                OnboardingJson = System.Text.Json.JsonSerializer.Serialize(
                    onboarding
                ),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            };
            _db.PendingSignups.Add(pending);
            await _db.SaveChangesAsync();

            await _provisioning.ProvisionFromPendingAsync(pending.Id);

            await _db.Entry(pending).ReloadAsync();
            Assert.Equal(PendingSignupStatuses.Complete, pending.Status);

            var login = await _db.UserExternalLogins.SingleAsync(x =>
                x.UserId == existing.Id
            );
            Assert.Equal(ExternalAuthProviders.Google, login.Provider);
            Assert.Equal("sub-retry", login.ProviderSubject);
        }

        [Fact]
        public async Task SaveOnboarding_EmailPendingPasswordRequired_StillEnforced()
        {
            var email = "email-owner@example.com";
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = email,
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    PasswordHash = null,
                    AuthProvider = null,
                    ProviderSubject = null,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _signup.SaveOnboardingAsync(
                    sessionToken,
                    BuildOnboardingDto(password: "short", confirmPassword: "short")
                )
            );

            Assert.Equal("Password must be at least 8 characters.", ex.Message);
            Assert.False(await _db.Users.AnyAsync(u => u.Email == email));
        }

        [Fact]
        public async Task GetBySession_SocialPending_ReturnsAuthProvider()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = "social-resume@example.com",
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    PasswordHash = null,
                    AuthProvider = ExternalAuthProviders.Google,
                    ProviderSubject = "sub-resume",
                    FullName = "Social Resume",
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await _db.SaveChangesAsync();

            var resume = await _signup.GetBySessionAsync(sessionToken);

            Assert.Equal(ExternalAuthProviders.Google, resume.AuthProvider);
            Assert.Equal("account", resume.LastStepHint);
            Assert.Equal("Social Resume", resume.FullName);
        }

        private static SaveSignupOnboardingDto BuildOnboardingDto(
            string password,
            string confirmPassword
        ) =>
            new()
            {
                Password = password,
                ConfirmPassword = confirmPassword,
                FullName = "Social Owner",
                GroupName = "Social Kitchen",
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
