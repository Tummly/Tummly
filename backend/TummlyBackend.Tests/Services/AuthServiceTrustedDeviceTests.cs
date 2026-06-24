using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AuthServiceTrustedDeviceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;
        private readonly NoOpEmailService _emailService = new();
        private readonly NoOpSmsService _smsService = new();

        public AuthServiceTrustedDeviceTests()
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

            var jwtService = new JwtService(jwtSettings);

            _service = new AuthService(
                _context,
                jwtService,
                _emailService,
                _smsService,
                configuration
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_IssuesDeviceToken_WhenRememberDeviceChecked()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            await SeedActiveOtpAsync(user.Email, "123456");

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = true,
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.True(payload.ContainsKey("deviceToken"));
            Assert.NotNull(payload["deviceToken"]?.ToString());

            var trustedCount = await _context.TrustedDevices.CountAsync();
            Assert.Equal(1, trustedCount);
            Assert.True(user.HasCompletedFirstSignIn);
        }

        [Fact]
        public async Task UniversalLoginAsync_SkipsOtp_WhenTrustedDeviceIsValid()
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
            Assert.NotNull(payload["token"]?.ToString());
            Assert.Equal("Single", payload["accountType"]?.ToString());
            Assert.DoesNotContain(user.Email, _emailService.SentOtpEmails);
        }

        [Fact]
        public async Task UniversalLoginAsync_RequiresOtp_OnFirstSignIn_EvenWithTrustedDevice()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
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
            Assert.Null(payload.GetValueOrDefault("token"));
            Assert.Equal("email", payload["otpChannel"]?.ToString());
            Assert.Contains(user.Email, _emailService.SentOtpEmails);
        }

        [Fact]
        public async Task UniversalLoginAsync_RequiresOtp_WhenDeviceTokenMissing()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);

            var result = await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.Equal("USER", payload["loginType"]);
            Assert.Null(payload.GetValueOrDefault("token"));
            Assert.Contains(user.Email, _emailService.SentOtpEmails);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<User> SeedUserAsync(bool hasCompletedFirstSignIn)
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
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task VerifyOtpAsync_AcceptsTwilioManagedSmsCode_WhenSmsChannelActive()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            await SeedActiveOtpAsync(
                user.Email,
                OtpVerification.TwilioManagedCode,
                OtpVerification.ChannelSms
            );

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                }
            );

            var payload = ToPropertyDictionary(result);
            Assert.NotNull(payload["token"]?.ToString());
            Assert.True(user.HasCompletedFirstSignIn);
        }

        private async Task SeedActiveOtpAsync(
            string email,
            string otpCode,
            string channel = OtpVerification.ChannelEmail
        )
        {
            await _context.OtpVerifications.AddAsync(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = otpCode,
                    Channel = channel,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                }
            );

            await _context.SaveChangesAsync();
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

        private sealed class NoOpEmailService : IEmailService
        {
            public List<string> SentOtpEmails { get; } = [];

            public Task SendOtpEmailAsync(string toEmail, string otp)
            {
                SentOtpEmails.Add(toEmail);
                return Task.CompletedTask;
            }

            public Task SendAccountSetupEmailAsync(
                string toEmail,
                string fullName,
                string setupLink
            ) =>
                Task.CompletedTask;

            public Task SendDeclineEmailAsync(
                string toEmail,
                string fullName
            ) =>
                Task.CompletedTask;

            public Task SendMoreInfoEmailAsync(
                string toEmail,
                string fullName
            ) =>
                Task.CompletedTask;

            public Task SendResetPasswordEmailAsync(
                string toEmail,
                string resetLink
            ) =>
                Task.CompletedTask;
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
