using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AuthServicePasswordResetTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _service;
        private readonly TrackingEmailService _emailService = new();

        public AuthServicePasswordResetTests()
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
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
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

            _service = new AuthService(
                _context,
                jwtService,
                _emailService,
                new NoOpSmsService(),
                configuration,
                new NoOpSignInMetadataResolver(),
                NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()),
                new ActivationGate()
            );
        }

        [Fact]
        public async Task ResetPasswordAsync_SendsPasswordChangedEmail()
        {
            var user = await SeedUserAsync("Alex Morgan", "operator@tummly.test");
            var resetToken = "reset-token-abc";

            _context.PasswordResets.Add(
                new PasswordReset
                {
                    UserId = user.Id,
                    ResetToken = resetToken,
                    IsUsed = false,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(30),
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            await _service.ResetPasswordAsync(
                new ResetPasswordDto
                {
                    Token = resetToken,
                    NewPassword = "NewPassword1!",
                    ConfirmPassword = "NewPassword1!",
                }
            );

            Assert.Single(_emailService.PasswordChangedEmails);
            Assert.Equal("operator@tummly.test", _emailService.PasswordChangedEmails[0].Email);
            Assert.Equal("Alex", _emailService.PasswordChangedEmails[0].FirstName);
        }

        private async Task<User> SeedUserAsync(string fullName, string email)
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                Role = "User",
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string FirstName)> PasswordChangedEmails { get; } = [];

            public override Task SendPasswordChangedEmailAsync(string toEmail, string firstName)
            {
                PasswordChangedEmails.Add((toEmail, firstName));
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
