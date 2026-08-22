using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Exceptions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AuthServiceRefreshTokenTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;

        public AuthServiceRefreshTokenTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
                    Secret =
                        configuration["Jwt:Secret"]!,
                    Issuer = configuration["Jwt:Issuer"]!,
                    Audience = configuration["Jwt:Audience"]!,
                    ExpiryMinutes = 60,
                }
            );

            _service = new AuthService(
                _context,
                new JwtService(jwtSettings),
                new EmailServiceStubBase(),
                new NoOpSmsService(),
                configuration,
                new TestSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                new TrackingOperatorNotificationsService()
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_IssuesRefreshToken()
        {
            var user = await SeedUserAsync();
            await SeedActiveOtpAsync(user.Email);

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                }
            );

            var payload = ToPropertyDictionary(result);
            var refreshToken = payload["refreshToken"]?.ToString();

            Assert.False(string.IsNullOrWhiteSpace(refreshToken));
            Assert.Equal(1, await _context.RefreshTokens.CountAsync());
            Assert.NotEqual(
                refreshToken,
                (await _context.RefreshTokens.SingleAsync()).Token
            );
        }

        [Fact]
        public async Task UniversalLoginAsync_IssuesRefreshToken_WhenTrustedDeviceSkipsOtp()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);
            var deviceToken =
                await TrustedDeviceHelper.IssueTrustedDeviceAsync(
                    _context,
                    user.Id
                );
            await _context.SaveChangesAsync();

            var result = await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                    DeviceToken = deviceToken,
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.Equal("USER", payload["loginType"]);
            Assert.False(
                string.IsNullOrWhiteSpace(payload["refreshToken"]?.ToString())
            );
        }

        [Fact]
        public async Task RefreshSessionAsync_ReturnsNewTokens_AndRejectsReuse()
        {
            var user = await SeedUserAsync();
            await SeedActiveOtpAsync(user.Email);

            var signIn = ToPropertyDictionary(
                await _service.VerifyOtpAsync(
                    new VerifyOtpDto
                    {
                        Email = user.Email,
                        OtpCode = "123456",
                    }
                )
            );

            var refreshToken = signIn["refreshToken"]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(refreshToken));

            var rotated = ToPropertyDictionary(
                await _service.RefreshSessionAsync(refreshToken!)
            );

            Assert.False(string.IsNullOrWhiteSpace(rotated["token"]?.ToString()));
            Assert.False(
                string.IsNullOrWhiteSpace(rotated["refreshToken"]?.ToString())
            );
            Assert.NotEqual(refreshToken, rotated["refreshToken"]?.ToString());

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                _service.RefreshSessionAsync(refreshToken!)
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<User> SeedUserAsync(
            bool hasCompletedFirstSignIn = false
        )
        {
            var user = new User
            {
                Email = "operator@example.com",
                FullName = "Operator",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
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

        private async Task SeedActiveOtpAsync(string email)
        {
            await _context.OtpVerifications.AddAsync(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = "123456",
                    Channel = OtpVerification.ChannelEmail,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                }
            );
            await _context.SaveChangesAsync();
        }

        private static Dictionary<string, object?> ToPropertyDictionary(
            object result
        )
        {
            return result
                .GetType()
                .GetProperties()
                .ToDictionary(
                    property => property.Name,
                    property => property.GetValue(result)
                );
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
                        FirstName = "Operator",
                        SignInTime = signInContext.SignedInAtUtc.ToString("u"),
                        DeviceSummary = "Browser",
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
