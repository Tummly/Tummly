using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AuthServicePilotMintTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;

        public AuthServicePilotMintTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] =
                            "test-secret-key-that-is-long-enough-for-hmac-sha256",
                        ["Jwt:Issuer"] = "tummly-test",
                        ["Jwt:Audience"] = "tummly-test",
                        ["Jwt:ExpiryMinutes"] = "60",
                    }
                )
                .Build();

            var jwtSettings = Microsoft.Extensions.Options.Options.Create(
                new TummlyBackend.Configurations.JwtSettings
                {
                    Secret = configuration["Jwt:Secret"]!,
                    Issuer = configuration["Jwt:Issuer"]!,
                    Audience = configuration["Jwt:Audience"]!,
                    ExpiryMinutes = 60,
                }
            );

            var jwtService = new JwtService(jwtSettings);
            var catalog = TestPricebookPaths.LoadV3();
            var ledger = new CreditLedgerService(
                _context,
                TimeProvider.System,
                catalog
            );

            _service = new AuthService(
                _context,
                jwtService,
                new EmailServiceStubBase(),
                new PilotMintNoOpSmsService(),
                configuration,
                new PilotMintSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                new TrackingOperatorNotificationsService(),
                new NoOpBillingAccountLifecycle(),
                ledger
            );
        }

        [Fact]
        public async Task ActivateAccountAsync_RollsBackActivation_WhenPilotAlreadyMinted()
        {
            const string plainCode = "ABCD2345";
            var user = await SeedPendingOwnerAsync(plainCode);
            var restaurantId = await SeedRestaurantWithBillingAsync(user.Id);

            _context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Email,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 500,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() =>
                _service.ActivateAccountAsync(user.Id, "ABCD-2345")
            );

            var reloaded = await _context.Users.AsNoTracking()
                .SingleAsync(row => row.Id == user.Id);
            Assert.Null(reloaded.ActivatedAt);
            Assert.Null(reloaded.ActivationExpiresAt);
        }

        private async Task<User> SeedPendingOwnerAsync(string plainCode)
        {
            var user = new User
            {
                Email = "pilot-mint@example.com",
                FullName = "Pilot Mint Owner",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivationCodeHash = ActivationCodeHelper.HashCode(plainCode),
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<int> SeedRestaurantWithBillingAsync(int ownerUserId)
        {
            var restaurant = new Restaurant
            {
                Name = "Pilot Mint Cafe",
                AccountType = "Single",
                OwnerUserId = ownerUserId,
                BillingContactUserId = ownerUserId,
                PrivacyContactUserId = ownerUserId,
                SupportContactUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await _context.SaveChangesAsync();
            return restaurant.Id;
        }

        public void Dispose()
        {
        }

        private sealed class PilotMintNoOpSmsService : ISmsService
        {
            public Task SendOtpSmsAsync(string phoneNumber) =>
                Task.CompletedTask;

            public Task<bool> VerifyOtpSmsAsync(string phoneNumber, string otp) =>
                Task.FromResult(true);
        }

        private sealed class PilotMintSignInMetadataResolver : ISignInMetadataResolver
        {
            public Task<NewDeviceSignInDetails> ResolveAsync(
                User user,
                SignInContext signInContext,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(
                    new NewDeviceSignInDetails
                    {
                        FirstName = "Pilot",
                        SignInTime = signInContext.SignedInAtUtc.ToString("u"),
                        DeviceSummary = "Browser",
                        LocationSummary = "London, England, United Kingdom",
                    }
                );
            }
        }
    }
}
