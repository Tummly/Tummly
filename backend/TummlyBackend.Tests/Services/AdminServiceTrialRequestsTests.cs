using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
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
                    }
                )
                .Build();

            _service = new AdminService(
                _context,
                new TrackingEmailService(),
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
                Status = "Account Created",
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
                    Status = "Account Created",
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

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : IEmailService
        {
            public Task SendOtpEmailAsync(string toEmail, string otp) =>
                Task.CompletedTask;

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
