using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AdminServiceTrialRequestsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminService _service;

        public AdminServiceTrialRequestsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.com",
                        ["JwtSettings:Secret"] =
                            "test-secret-key-that-is-long-enough-for-hmac-sha256",
                    }
                )
                .Build();

            var emailService = new TrackingEmailService();
            var trialReviewTransition = new TrialReviewTransition(
                _context,
                emailService,
                configuration,
                NullLogger<TrialReviewTransition>.Instance
            );

            _service = new AdminService(
                _context,
                trialReviewTransition,
                configuration,
                NullLogger<AdminService>.Instance
            );
        }

        [Fact]
        public async Task GetAllTrialRequestsAsync_IncludesOperatorLocations_ForCreatedAccounts()
        {
            var trialRequest = new TrialRequest
            {
                BusinessName = "Test Cafe",
                BusinessCategory = "Cafe / coffee shop",
                Locations = "1",
                FullName = "Jane Operator",
                Email = "Jane@Example.com",
                Mobile = "07123456789",
                Role = "Owner",
                Goal = "Grow repeat guests",
                TermsAccepted = true,
                IsApproved = true,
                IsAccountCreated = true,
                AccountType = "Single",
                Status = TrialRequestStatus.AccountCreated,
            };

            _context.TrialRequests.Add(trialRequest);

            var user = new User
            {
                FullName = "Jane Operator",
                Email = "jane@example.com",
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Test Cafe",
                AccountType = "Single",
                OwnerUserId = user.Id,
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LinkToken = "abc123token4567890123456789012",
                    LocationName = "Test Cafe",
                    Address = "125 High Street, Manchester",
                    Postcode = "M1 4AB",
                }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetAllTrialRequestsAsync();

            var result = Assert.Single(results);
            var location = Assert.Single(result.OperatorLocations);
            Assert.Equal("125 High Street, Manchester", location.Address);
            Assert.Equal("M1 4AB", location.Postcode);
        }

        [Fact]
        public async Task GetAllTrialRequestsAsync_IncludesOperatorLocations_WhenAccountFlagIsMissing()
        {
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Late Flag Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Alex Operator",
                    Email = "alex@example.com",
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = false,
                    AccountType = "Single",
                    Status = TrialRequestStatus.AccountCreated,
                }
            );

            var user = new User
            {
                FullName = "Alex Operator",
                Email = "alex@example.com",
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Late Flag Cafe",
                AccountType = "Single",
                OwnerUserId = user.Id,
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LinkToken = "def456token789012345678901234",
                    LocationName = "Late Flag Cafe",
                    Address = "42 Market Street, Leeds",
                    Postcode = "LS1 6DT",
                }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetAllTrialRequestsAsync();

            var result = Assert.Single(results);
            Assert.Equal("42 Market Street, Leeds", result.PrimaryAddress);
            Assert.Equal("LS1 6DT", result.PrimaryPostcode);
            Assert.Single(result.OperatorLocations);
        }

        [Fact]
        public async Task GetAllTrialRequestsAsync_IncludesMainLocation_FromTrialRequest()
        {
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Harbour Bistro",
                    BusinessCategory = "Casual dining restaurant",
                    Locations = "1",
                    FullName = "Sam Operator",
                    Email = "sam@example.com",
                    Mobile = "07123456789",
                    MainLocation = "10 Dock Road, Bristol",
                    TownCity = "Bristol",
                    Postcode = "BS1 4ST",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = false,
                    IsAccountCreated = false,
                    AccountType = "Single",
                    Status = TrialRequestStatus.EmailVerified,
                }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetAllTrialRequestsAsync();

            var result = Assert.Single(results);
            Assert.Equal("10 Dock Road, Bristol", result.MainLocation);
            Assert.Equal("Bristol", result.TownCity);
            Assert.Equal("BS1 4ST", result.MainLocationPostcode);
        }

        [Fact]
        public async Task GetAllTrialRequestsAsync_IncludesActivationFields_ForCreatedAccounts()
        {
            const string plainCode = "ABCD2345";

            var trialRequest = new TrialRequest
            {
                BusinessName = "Activation Cafe",
                BusinessCategory = "Cafe / coffee shop",
                Locations = "1",
                FullName = "Pat Operator",
                Email = "pat@example.com",
                Mobile = "07123456789",
                Role = "Owner",
                Goal = "Grow repeat guests",
                TermsAccepted = true,
                IsApproved = true,
                IsAccountCreated = true,
                AccountType = "Single",
                Status = TrialRequestStatus.AccountCreated,
            };

            _context.TrialRequests.Add(trialRequest);

            var user = new User
            {
                FullName = "Pat Operator",
                Email = "pat@example.com",
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivationCodeHash = ActivationCodeHelper.HashCode(plainCode),
                ActivationCodeEncrypted = ActivationCodeProtectionHelper.Encrypt(
                    plainCode,
                    "test-secret-key-that-is-long-enough-for-hmac-sha256"
                ),
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var results = await _service.GetAllTrialRequestsAsync();

            var result = Assert.Single(results);
            Assert.Equal(user.Id, result.OperatorUserId);
            Assert.Equal("not_activated", result.ActivationStatus);
            Assert.Equal("pending", result.ActivationStatusDetail);
            Assert.Equal("ABCD-2345", result.ActivationCode);
        }

        [Fact]
        public async Task ExtendActivationAsync_RestoresExpiredAccount()
        {
            var user = new User
            {
                FullName = "Expired Operator",
                Email = "expired@example.com",
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivatedAt = DateTime.UtcNow.AddDays(-40),
                ActivationExpiresAt = DateTime.UtcNow.AddDays(-10),
            };

            _context.Users.Add(user);
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Expired Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Expired Operator",
                    Email = "expired@example.com",
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = true,
                    AccountType = "Single",
                    Status = TrialRequestStatus.AccountCreated,
                }
            );
            await _context.SaveChangesAsync();

            var updated = await _service.ExtendActivationAsync(
                user.Id,
                new ExtendActivationDto()
            );

            Assert.NotNull(updated);
            Assert.Equal("activated", updated.ActivationStatus);
            Assert.Equal("active", updated.ActivationStatusDetail);
            Assert.NotNull(updated.ActivationExpiresAt);

            var persisted = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(persisted?.ActivationExpiresAt);
            Assert.True(persisted.ActivationExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task GetActivationDownloadAsync_ReturnsSvgAsset()
        {
            const string plainCode = "WXYZ9876";

            var user = new User
            {
                FullName = "Download Operator",
                Email = "download@example.com",
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivationCodeHash = ActivationCodeHelper.HashCode(plainCode),
                ActivationCodeEncrypted = ActivationCodeProtectionHelper.Encrypt(
                    plainCode,
                    "test-secret-key-that-is-long-enough-for-hmac-sha256"
                ),
            };

            _context.Users.Add(user);
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Download Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Download Operator",
                    Email = "download@example.com",
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = true,
                    AccountType = "Single",
                    Status = TrialRequestStatus.AccountCreated,
                }
            );
            await _context.SaveChangesAsync();

            var download = await _service.GetActivationDownloadAsync(user.Id);

            Assert.NotNull(download);
            Assert.Equal("image/svg+xml", download.Value.ContentType);
            var svg = System.Text.Encoding.UTF8.GetString(download.Value.Content);
            Assert.Contains("WXYZ-9876", svg);
            Assert.Contains("Download Cafe", svg);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : IEmailService
        {
            public Task SendOtpEmailAsync(string toEmail, string otp) =>
                Task.CompletedTask;

            public Task SendTrialRequestReceivedEmailAsync(
                string toEmail,
                string fullName,
                string businessName
            ) => Task.CompletedTask;

            public Task SendAccountSetupEmailAsync(
                string toEmail,
                string fullName,
                string setupLink
            ) => Task.CompletedTask;

            public Task SendAccountSetupReminderEmailAsync(
                string toEmail,
                string fullName,
                string setupLink,
                DateTime expiresAtUtc
            ) => Task.CompletedTask;

            public Task SendDeclineEmailAsync(
                string toEmail,
                string fullName,
                string declineReason
            ) => Task.CompletedTask;

            public Task SendMoreInfoEmailAsync(
                string toEmail,
                string fullName,
                string moreInfoMessage
            ) => Task.CompletedTask;

            public Task SendResetPasswordEmailAsync(
                string toEmail,
                string resetLink
            ) => Task.CompletedTask;

            public Task SendPasswordChangedEmailAsync(
                string toEmail,
                string firstName
            ) => Task.CompletedTask;

            public Task SendNewDeviceSignInEmailAsync(
                string toEmail,
                NewDeviceSignInDetails details
            ) => Task.CompletedTask;
        }
    }
}
