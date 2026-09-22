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
    public class AuthServiceExternalSignInTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;
        private readonly NoOpEmailService _emailService = new();

        public AuthServiceExternalSignInTests()
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

            _service = new AuthService(
                _context,
                new JwtService(jwtSettings),
                _emailService,
                new NoOpSmsService(),
                configuration,
                new TestSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                new TrackingOperatorNotificationsService(),
                new NoOpBillingAccountLifecycle(),
                new NoOpCreditLedger()
            );
        }

        [Fact]
        public async Task CompleteExternal_FirstSignIn_ReturnsOtpChallenge()
        {
            var user = await SeedSocialUserAsync(hasCompletedFirstSignIn: false);

            var result = await _service.CompleteExternalOperatorSignInAsync(
                user.Id,
                rememberDevice: false,
                deviceToken: null
            );

            var payload = ToPropertyDictionary(result);

            Assert.Equal("USER", payload["loginType"]);
            Assert.Equal("email", payload["otpChannel"]?.ToString());
            Assert.Null(payload.GetValueOrDefault("token"));
            Assert.Contains(user.Email, _emailService.SentOtpEmails);
        }

        [Fact]
        public async Task CompleteExternal_TrustedDevice_ReturnsJwt()
        {
            var user = await SeedSocialUserAsync(hasCompletedFirstSignIn: true);
            var deviceToken =
                await TrustedDeviceHelper.IssueTrustedDeviceAsync(
                    _context,
                    user.Id
                );
            await _context.SaveChangesAsync();

            var result = await _service.CompleteExternalOperatorSignInAsync(
                user.Id,
                rememberDevice: true,
                deviceToken
            );

            var payload = ToPropertyDictionary(result);

            Assert.Equal("USER", payload["loginType"]);
            Assert.False(string.IsNullOrWhiteSpace(payload["token"]?.ToString()));
            Assert.False(
                string.IsNullOrWhiteSpace(payload["refreshToken"]?.ToString())
            );
            Assert.DoesNotContain(user.Email, _emailService.SentOtpEmails);
        }

        [Fact]
        public async Task UniversalLogin_NullPasswordHash_ThrowsInvalidCredentials()
        {
            var user = await SeedSocialUserAsync(hasCompletedFirstSignIn: true);

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.UniversalLoginAsync(
                    new UserLoginDto
                    {
                        Email = user.Email,
                        Password = "anything",
                    }
                )
            );

            Assert.Equal("Invalid email or password.", exception.Message);
        }

        [Fact]
        public async Task UserLogin_NullPasswordHash_ThrowsInvalidCredentials()
        {
            var user = await SeedSocialUserAsync(hasCompletedFirstSignIn: true);

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.UserLoginAsync(
                    new UserLoginDto
                    {
                        Email = user.Email,
                        Password = "anything",
                    }
                )
            );

            Assert.Equal("Invalid email or password.", exception.Message);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<User> SeedSocialUserAsync(bool hasCompletedFirstSignIn)
        {
            var user = new User
            {
                Email = "social@example.com",
                FullName = "Social Operator",
                PhoneNumber = "5551234567",
                PasswordHash = null,
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = hasCompletedFirstSignIn,
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

        private sealed class NoOpEmailService : EmailServiceStubBase
        {
            public List<string> SentOtpEmails { get; } = [];

            public override Task SendOtpEmailAsync(string toEmail, string otp)
            {
                SentOtpEmails.Add(toEmail);
                return Task.CompletedTask;
            }
        }

        private sealed class TestSignInMetadataResolver : ISignInMetadataResolver
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
                        FirstName = user.FullName.Split(' ')[0],
                        SignInTime = signInContext.SignedInAtUtc.ToString("u"),
                        DeviceSummary =
                            UserAgentHelper.Summarize(signInContext.UserAgent),
                        LocationSummary = "London, England, United Kingdom",
                    }
                );
            }
        }

        private sealed class NoOpSmsService : ISmsService
        {
            public Task SendOtpSmsAsync(string phoneNumber) =>
                Task.CompletedTask;

            public Task<bool> VerifyOtpSmsAsync(
                string phoneNumber,
                string otp
            ) =>
                Task.FromResult(otp == "123456");
        }
    }
}
