using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// Sign-in email codes are purpose-bound (<c>sign_in</c>). A code issued for signup,
    /// trial or team invite never signs a user in, and sign-in never removes those codes.
    /// </summary>
    public class AuthServiceOtpPurposeTests : IDisposable
    {
        private const string Email = "operator@example.com";

        private readonly ApplicationDbContext _context;
        private readonly CapturingEmailService _email = new();
        private readonly IOtpChallengeService _otp;
        private readonly AuthService _service;

        public AuthServiceOtpPurposeTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _otp = OtpTestHelpers.CreateChallengeService(_context);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-secret-key-that-is-long-enough-for-hmac-sha256",
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

            _service = new AuthService(
                _context,
                new JwtService(jwtSettings),
                _email,
                new NoOpSmsService(),
                configuration,
                new NoOpSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                new TrackingOperatorNotificationsService(),
                new NoOpBillingAccountLifecycle(),
                new NoOpCreditLedger(),
                Microsoft.Extensions.Options.Options.Create(new TummlyBackend.Configurations.DemoSessionOptions()),
                _otp
            );
        }

        [Fact]
        public async Task SignIn_SignupCodeRefused()
        {
            await SeedUserAsync();
            await _service.SendAuthOtpAsync(Email, "resend");
            var signInCode = _email.Codes.Single();

            var signup = await _otp.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Guid.NewGuid().ToString("N"), 1)
            );

            var refused = await Assert.ThrowsAsync<Exception>(() =>
                _service.VerifyOtpAsync(new VerifyOtpDto { Email = Email, OtpCode = signup.Code })
            );
            Assert.Equal("Invalid OTP.", refused.Message);

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto { Email = Email, OtpCode = signInCode }
            );
            Assert.NotNull(ToPropertyDictionary(result)["token"]?.ToString());
        }

        [Fact]
        public async Task SignIn_RequestCode_DoesNotRemoveSignupCode()
        {
            await SeedUserAsync();
            var signup = await _otp.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Guid.NewGuid().ToString("N"), 1)
            );

            // A signup code must not start a sign-in cooldown either, so this call succeeds.
            await _service.SendAuthOtpAsync(Email, "resend");

            var signupRow = await _context.OtpVerifications.SingleAsync(row =>
                row.Id == signup.OtpVerificationId
            );
            Assert.False(signupRow.IsUsed);
            Assert.Equal(OtpPurposes.Signup, signupRow.Purpose);
            Assert.Equal(2, await _context.OtpVerifications.CountAsync());
        }

        [Fact]
        public async Task SignIn_FiveWrong_RequiresNewCode()
        {
            await SeedUserAsync();
            await _service.SendAuthOtpAsync(Email, "resend");
            var code = _email.Codes.Single();
            var wrong = code == "000000" ? "999999" : "000000";

            for (var attempt = 0; attempt < OtpChallengeService.MaxWrongAttempts; attempt++)
            {
                var ex = await Assert.ThrowsAsync<Exception>(() =>
                    _service.VerifyOtpAsync(new VerifyOtpDto { Email = Email, OtpCode = wrong })
                );
                Assert.Equal("Invalid OTP.", ex.Message);
            }

            // The budget is spent. The right code no longer works.
            var spent = await Assert.ThrowsAsync<Exception>(() =>
                _service.VerifyOtpAsync(new VerifyOtpDto { Email = Email, OtpCode = code })
            );
            Assert.Equal("Invalid OTP.", spent.Message);

            // Move the locked row out of the 60 second cooldown window, then request a new code.
            var rows = await _context.OtpVerifications.ToListAsync();
            foreach (var row in rows)
            {
                row.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            }
            await _context.SaveChangesAsync();

            await _service.SendAuthOtpAsync(Email, "resend");
            var fresh = _email.Codes.Last();

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto { Email = Email, OtpCode = fresh }
            );
            Assert.NotNull(ToPropertyDictionary(result)["token"]?.ToString());
        }

        private async Task<User> SeedUserAsync()
        {
            var user = new User
            {
                Email = Email,
                FullName = "Operator",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private static Dictionary<string, object?> ToPropertyDictionary(object result)
        {
            return result
                .GetType()
                .GetProperties()
                .ToDictionary(
                    property => property.Name,
                    property => property.GetValue(result)
                );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class CapturingEmailService : EmailServiceStubBase
        {
            public List<string> Codes { get; } = [];

            public override Task SendOtpEmailAsync(
                string toEmail,
                string otp,
                string? restaurantName = null
            )
            {
                Codes.Add(otp);
                return Task.CompletedTask;
            }
        }

        private sealed class NoOpSmsService : ISmsService
        {
            public Task SendOtpSmsAsync(string phoneNumber) => Task.CompletedTask;

            public Task<bool> VerifyOtpSmsAsync(string phoneNumber, string otp) =>
                Task.FromResult(false);
        }

        private sealed class NoOpSignInMetadataResolver : ISignInMetadataResolver
        {
            public Task<NewDeviceSignInDetails> ResolveAsync(
                User user,
                SignInContext signInContext,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new NewDeviceSignInDetails
                    {
                        FirstName = "Operator",
                        SignInTime = signInContext.SignedInAtUtc.ToString("u"),
                        DeviceSummary = "Browser",
                        LocationSummary = "London, England, United Kingdom",
                    }
                );
        }
    }
}
