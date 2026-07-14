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
    public class AuthServiceTrustedDeviceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;
        private readonly NoOpEmailService _emailService = new();
        private readonly NoOpSmsService _smsService = new();
        private readonly TrackingOperatorNotificationsService _notifications = new();

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
                configuration,
                new TestSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate(),
                _notifications
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
        public async Task UniversalLoginAsync_ExposesHasVerifiedPhone_WhenPhoneOnFile()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            user.TermsAccepted = false;
            await _context.SaveChangesAsync();

            var result = await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.True(Convert.ToBoolean(payload["hasVerifiedPhone"]));
            Assert.NotNull(payload["maskedPhone"]?.ToString());
        }

        [Fact]
        public async Task SendAuthOtpSmsAsync_AllowsImmediateSwitch_FromEmailOtp()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);

            await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                }
            );

            var result = await _service.SendAuthOtpSmsAsync(user.Email);

            Assert.Equal(OtpVerification.ChannelSms, result.OtpChannel);
            Assert.False(result.Skipped);

            var activeOtp = await _context.OtpVerifications
                .Where(x => x.Email == user.Email && x.IsUsed == false)
                .SingleAsync();

            Assert.Equal(OtpVerification.ChannelSms, activeOtp.Channel);
            Assert.Equal(OtpVerification.TwilioManagedCode, activeOtp.OtpCode);
        }

        [Fact]
        public async Task SendAuthOtpSmsAsync_EnforcesCooldown_WhenSmsOtpAlreadyActive()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            await SeedActiveOtpAsync(
                user.Email,
                OtpVerification.TwilioManagedCode,
                OtpVerification.ChannelSms
            );

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.SendAuthOtpSmsAsync(user.Email)
            );

            Assert.Equal(
                "Please wait before resending OTP.",
                exception.Message
            );
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

        [Fact]
        public async Task VerifyOtpAsync_SendsNewDeviceAlert_OnReturningSignIn()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);
            await SeedActiveOtpAsync(user.Email, "123456");

            await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                },
                new SignInContext
                {
                    SignedInAtUtc = new DateTime(2026, 6, 24, 14, 32, 0, DateTimeKind.Utc),
                    IpAddress = "203.0.113.42",
                    UserAgent =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                }
            );

            Assert.Single(_emailService.NewDeviceSignInEmails);
            Assert.Equal(user.Email, _emailService.NewDeviceSignInEmails[0].Email);
            Assert.Equal("Operator", _emailService.NewDeviceSignInEmails[0].Details.FirstName);
            Assert.Equal(
                "Chrome on Windows",
                _emailService.NewDeviceSignInEmails[0].Details.DeviceSummary
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_ProducesNewSignInNotice_OnReturningSignIn()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);
            await SeedActiveOtpAsync(user.Email, "123456");

            await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                },
                new SignInContext
                {
                    SignedInAtUtc = new DateTime(2026, 6, 24, 14, 32, 0, DateTimeKind.Utc),
                    IpAddress = "203.0.113.42",
                    UserAgent =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                }
            );

            Assert.Single(_notifications.Produced);
            Assert.Equal(user.Id, _notifications.Produced[0].UserId);
            Assert.Equal("new-sign-in", _notifications.Produced[0].Type);
            Assert.Equal(
                "New sign-in to your Tummly account",
                _notifications.Produced[0].Title
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_DoesNotProduceNewSignInNotice_OnFirstSignIn()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            await SeedActiveOtpAsync(user.Email, "123456");

            await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                },
                new SignInContext
                {
                    IpAddress = "203.0.113.42",
                    UserAgent = "Mozilla/5.0 Chrome/120.0.0.0",
                }
            );

            Assert.Empty(_notifications.Produced);
        }

        [Fact]
        public async Task UniversalLoginAsync_DoesNotProduceNewSignInNotice_OnTrustSkip()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);
            var deviceToken =
                await TrustedDeviceHelper.IssueTrustedDeviceAsync(
                    _context,
                    user.Id
                );
            await _context.SaveChangesAsync();

            await _service.UniversalLoginAsync(
                new UserLoginDto
                {
                    Email = user.Email,
                    Password = "password123",
                    DeviceToken = deviceToken,
                }
            );

            Assert.Empty(_notifications.Produced);
        }

        [Fact]
        public async Task VerifyOtpAsync_DoesNotSendNewDeviceAlert_OnFirstSignIn()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            await SeedActiveOtpAsync(user.Email, "123456");

            await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                },
                new SignInContext
                {
                    IpAddress = "203.0.113.42",
                    UserAgent = "Mozilla/5.0 Chrome/120.0.0.0",
                }
            );

            Assert.Empty(_emailService.NewDeviceSignInEmails);
        }

        [Fact]
        public async Task VerifyOtpAsync_DoesNotRequireWorkspaceSetup_ForMultiLocationSingleRestaurant()
        {
            var user = await SeedUserAsync(
                hasCompletedFirstSignIn: false,
                accountType: "Multi"
            );
            await SeedRestaurantAsync(user.Id, "Group A");
            await SeedActiveOtpAsync(user.Email, "123456");

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.False(Convert.ToBoolean(payload["workspaceSetupRequired"]));
        }

        [Fact]
        public async Task VerifyOtpAsync_RequiresWorkspaceSetup_WhenOperatorOwnsMultipleRestaurants()
        {
            var user = await SeedUserAsync(
                hasCompletedFirstSignIn: false,
                accountType: "Multi"
            );
            await SeedRestaurantAsync(user.Id, "Group A");
            await SeedRestaurantAsync(user.Id, "Group B");
            await SeedActiveOtpAsync(user.Email, "123456");

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.True(Convert.ToBoolean(payload["workspaceSetupRequired"]));
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsActivationRequired_WhenPendingActivation()
        {
            var user = await SeedUserAsync(
                hasCompletedFirstSignIn: false,
                activated: false
            );
            user.ActivationCodeHash =
                ActivationCodeHelper.HashCode("ABCD2345");
            await _context.SaveChangesAsync();
            await SeedActiveOtpAsync(user.Email, "123456");

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = user.Email,
                    OtpCode = "123456",
                    RememberDevice = false,
                }
            );

            var payload = ToPropertyDictionary(result);

            Assert.True(Convert.ToBoolean(payload["activationRequired"]));
            Assert.Null(payload["activationExpiresAt"]);
        }

        [Fact]
        public async Task ActivateAccountAsync_ActivatesPendingOperator()
        {
            const string plainCode = "ABCD2345";
            var user = await SeedUserAsync(
                hasCompletedFirstSignIn: true,
                activated: false
            );
            user.ActivationCodeHash =
                ActivationCodeHelper.HashCode(plainCode);
            await _context.SaveChangesAsync();

            var result = await _service.ActivateAccountAsync(
                user.Id,
                "ABCD-2345"
            );

            Assert.False(result.ActivationRequired);
            Assert.NotNull(result.ActivationExpiresAt);

            var updated = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updated?.ActivatedAt);
            Assert.NotNull(updated.ActivationExpiresAt);
        }

        [Fact]
        public async Task ActivateAccountAsync_RejectsWrongCode()
        {
            var user = await SeedUserAsync(
                hasCompletedFirstSignIn: true,
                activated: false
            );
            user.ActivationCodeHash =
                ActivationCodeHelper.HashCode("ABCD2345");
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() =>
                _service.ActivateAccountAsync(user.Id, "WXYZ9876")
            );
        }

        [Fact]
        public async Task UniversalLoginAsync_RejectsExpiredOperator()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: true);
            user.ActivatedAt = DateTime.UtcNow.AddDays(-40);
            user.ActivationExpiresAt = DateTime.UtcNow.AddDays(-1);
            await _context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<ActivationExpiredException>(() =>
                _service.UniversalLoginAsync(
                    new UserLoginDto
                    {
                        Email = user.Email,
                        Password = "password123",
                    }
                )
            );

            Assert.Equal(
                ActivationGate.ActivationExpiredMessage,
                exception.Message
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_RejectsExpiredOperator()
        {
            var user = await SeedUserAsync(hasCompletedFirstSignIn: false);
            user.ActivatedAt = DateTime.UtcNow.AddDays(-40);
            user.ActivationExpiresAt = DateTime.UtcNow.AddDays(-1);
            await _context.SaveChangesAsync();
            await SeedActiveOtpAsync(user.Email, "123456");

            var exception = await Assert.ThrowsAsync<ActivationExpiredException>(() =>
                _service.VerifyOtpAsync(
                    new VerifyOtpDto
                    {
                        Email = user.Email,
                        OtpCode = "123456",
                        RememberDevice = false,
                    }
                )
            );

            Assert.Equal(
                ActivationGate.ActivationExpiredMessage,
                exception.Message
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<User> SeedUserAsync(
            bool hasCompletedFirstSignIn,
            string accountType = "Single",
            bool activated = true
        )
        {
            var user = new User
            {
                Email = "operator@example.com",
                FullName = "Operator",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = accountType,
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = hasCompletedFirstSignIn,
            };

            if (activated)
            {
                user.ActivatedAt = DateTime.UtcNow;
                user.ActivationExpiresAt = DateTime.UtcNow.AddDays(30);
            }

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private async Task SeedRestaurantAsync(int ownerUserId, string name)
        {
            await _context.Restaurants.AddAsync(
                new Restaurant
                {
                    Name = name,
                    OwnerUserId = ownerUserId,
                    AccountType = "Multi",
                }
            );

            await _context.SaveChangesAsync();
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

        private sealed class NoOpEmailService : EmailServiceStubBase
        {
            public List<string> SentOtpEmails { get; } = [];

            public List<(string Email, NewDeviceSignInDetails Details)>
                NewDeviceSignInEmails { get; } = [];

            public override Task SendOtpEmailAsync(string toEmail, string otp)
            {
                SentOtpEmails.Add(toEmail);
                return Task.CompletedTask;
            }

            public override Task SendNewDeviceSignInEmailAsync(
                string toEmail,
                NewDeviceSignInDetails details
            )
            {
                NewDeviceSignInEmails.Add((toEmail, details));
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
                        DeviceSummary = UserAgentHelper.Summarize(signInContext.UserAgent),
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
