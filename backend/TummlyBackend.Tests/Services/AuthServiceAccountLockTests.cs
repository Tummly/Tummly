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
    public class AuthServiceAccountLockTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;
        private readonly NoOpEmailService _emailService = new();

        public AuthServiceAccountLockTests()
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
                new NoOpSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                new TrackingOperatorNotificationsService(),
                new NoOpBillingAccountLifecycle(),
                new NoOpCreditLedger()
            );
        }

        [Fact]
        public async Task UniversalLogin_LockedOperator_WrongPassword_ThrowsGenericAndStaysLocked()
        {
            var user = await SeedLockedOperatorAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.UniversalLoginAsync(
                    new UserLoginDto
                    {
                        Email = user.Email,
                        Password = "wrong-password",
                    }
                )
            );

            Assert.Equal("Invalid email or password.", ex.Message);
            Assert.DoesNotContain("locked", ex.Message, StringComparison.OrdinalIgnoreCase);

            await _context.Entry(user).ReloadAsync();
            Assert.True(user.IsLocked);
        }

        [Fact]
        public async Task UniversalLogin_LockedOperator_CorrectPassword_ReturnsOtpChallengeStillLocked()
        {
            var user = await SeedLockedOperatorAsync();

            var result = await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.Equal("USER", payload["loginType"]);
            Assert.Equal("email", payload["otpChannel"]?.ToString());
            Assert.Null(payload.GetValueOrDefault("token"));
            Assert.Contains(user.Email, _emailService.SentOtpEmails);

            await _context.Entry(user).ReloadAsync();
            Assert.True(user.IsLocked);
            Assert.Equal(5, user.FailedLoginAttempts);
        }

        [Fact]
        public async Task VerifyOtp_AfterLockedLogin_ClearsLockAndIssuesSession()
        {
            var user = await SeedLockedOperatorAsync();

            await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                }
            );

            var otp = await _context.OtpVerifications
                .Where(x => x.Email == user.Email && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstAsync();

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = otp.OtpCode,
                    RememberDevice = false,
                }
            );

            var payload = ToPropertyDictionary(result);
            Assert.NotNull(payload["token"]?.ToString());

            await _context.Entry(user).ReloadAsync();
            Assert.False(user.IsLocked);
            Assert.Equal(0, user.FailedLoginAttempts);
        }

        [Fact]
        public async Task UniversalLogin_LockedOperator_WithTrustedDevice_StillRequiresOtp()
        {
            var user = await SeedLockedOperatorAsync(hasCompletedFirstSignIn: true);
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
            Assert.Equal("email", payload["otpChannel"]?.ToString());
            Assert.Null(payload.GetValueOrDefault("token"));
            Assert.Contains(user.Email, _emailService.SentOtpEmails);

            await _context.Entry(user).ReloadAsync();
            Assert.True(user.IsLocked);
        }

        [Fact]
        public async Task CompleteExternal_LockedOperator_ReturnsOtpChallenge()
        {
            var user = await SeedLockedOperatorAsync(hasCompletedFirstSignIn: true);
            user.PasswordHash = null;
            await _context.SaveChangesAsync();

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

            await _context.Entry(user).ReloadAsync();
            Assert.True(user.IsLocked);
        }

        [Fact]
        public async Task AdminLogin_LockedAdmin_ThrowsAccountIsLocked()
        {
            var admin = new Admin
            {
                FullName = "Locked Admin",
                Email = "admin@tummly.test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Admin",
                IsActive = true,
                IsLocked = true,
                FailedLoginAttempts = 5,
            };
            await _context.Admins.AddAsync(admin);
            await _context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.AdminLoginAsync(
                    new AdminLoginDto
                    {
                        Email = admin.Email,
                        Password = "password123",
                    }
                )
            );

            Assert.Equal("Account is locked.", ex.Message);
        }

        private async Task<User> SeedLockedOperatorAsync(
            bool hasCompletedFirstSignIn = false
        )
        {
            var user = new User
            {
                Email = $"locked-{Guid.NewGuid():N}@tummly.test",
                FullName = "Locked Operator",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = hasCompletedFirstSignIn,
                IsLocked = true,
                FailedLoginAttempts = 5,
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

        private sealed class NoOpEmailService : EmailServiceStubBase
        {
            public List<string> SentOtpEmails { get; } = [];

            public override Task SendOtpEmailAsync(
                string toEmail,
                string otp,
                string? restaurantName = null
            )
            {
                SentOtpEmails.Add(toEmail);
                return Task.CompletedTask;
            }
        }

        private sealed class NoOpSmsService : ISmsService
        {
            public Task SendOtpSmsAsync(string phoneNumber) =>
                Task.CompletedTask;

            public Task<bool> VerifyOtpSmsAsync(string phoneNumber, string otp) =>
                Task.FromResult(otp == "123456");
        }

        private sealed class NoOpSignInMetadataResolver : ISignInMetadataResolver
        {
            public Task<NewDeviceSignInDetails> ResolveAsync(
                User user,
                SignInContext signInContext,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(new NewDeviceSignInDetails());
        }
    }
}
