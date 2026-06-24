using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Exceptions;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class GuestLoopProvisioningServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GuestLoopProvisioningService _service;

        public GuestLoopProvisioningServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example"
                })
                .Build();

            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration
            );

            _service = new GuestLoopProvisioningService(
                _context,
                smartGuestLink
            );
        }

        [Fact]
        public async Task ValidateInviteTokenAsync_ReturnsRichResult_ForValidToken()
        {
            await SeedTrialRequestAsync("invite-token");

            var result =
                await _service.ValidateInviteTokenAsync("invite-token");

            Assert.Equal("Single", result.AccountType);
            Assert.Equal("owner@example.com", result.Email);
            Assert.Equal("Alex Owner", result.FullName);
            Assert.Equal("The Golden Fork", result.RestaurantName);
            Assert.Equal("07911123456", result.Mobile);
            Assert.Equal("takeaway", result.BusinessCategory);
        }

        [Fact]
        public async Task ValidateInviteTokenAsync_Throws_WhenTokenMissing()
        {
            await Assert.ThrowsAsync<InviteTokenNotFoundException>(() =>
                _service.ValidateInviteTokenAsync("")
            );
        }

        [Fact]
        public async Task ValidateInviteTokenAsync_Throws_WhenNotApproved()
        {
            await SeedTrialRequestAsync("pending-token", isApproved: false);

            await Assert.ThrowsAsync<InviteTokenNotApprovedException>(() =>
                _service.ValidateInviteTokenAsync("pending-token")
            );
        }

        [Fact]
        public async Task ValidateInviteTokenAsync_Throws_WhenExpired()
        {
            await SeedTrialRequestAsync(
                "expired-token",
                inviteExpiresAt: DateTime.UtcNow.AddDays(-1)
            );

            await Assert.ThrowsAsync<InviteTokenExpiredException>(() =>
                _service.ValidateInviteTokenAsync("expired-token")
            );
        }

        [Fact]
        public async Task ValidateInviteTokenAsync_Throws_WhenAccountAlreadyCreated()
        {
            await SeedTrialRequestAsync(
                "used-token",
                isAccountCreated: true
            );

            await Assert.ThrowsAsync<AccountAlreadyCreatedException>(() =>
                _service.ValidateInviteTokenAsync("used-token")
            );
        }

        [Fact]
        public async Task ProvisionAsync_CreatesEntitiesAndLinkTokens()
        {
            await SeedTrialRequestAsync("provision-token");

            await _service.ProvisionAsync(new CompleteSetupDto
            {
                Token = "provision-token",
                Password = "password123",
                ConfirmPassword = "password123",
                FullName = "Alex Owner",
                GroupName = "The Golden Fork",
                BusinessCategory = "takeaway",
                PrimaryPhone = "07911123456",
                Locations =
                [
                    new CompleteSetupDto.LocationItem
                    {
                        LocationName = "Main",
                        Address = "1 High Street"
                    }
                ]
            });

            var user = await _context.Users.SingleAsync();
            var restaurant = await _context.Restaurants.SingleAsync();
            var location = await _context.RestaurantLocations.SingleAsync();
            var guestLoop = await _context.GuestLoopSetups.SingleAsync();
            var trialRequest = await _context.TrialRequests.SingleAsync();

            Assert.Equal("owner@example.com", user.Email);
            Assert.Equal("+447911123456", user.PhoneNumber);
            Assert.Equal("The Golden Fork", restaurant.Name);
            Assert.Equal(32, location.LinkToken.Length);
            Assert.Equal(restaurant.Id, guestLoop.RestaurantId);
            Assert.True(trialRequest.IsAccountCreated);
        }

        [Fact]
        public async Task ProvisionAsync_GeneratesUniqueLinkTokens()
        {
            await SeedTrialRequestAsync("multi-token", accountType: "Multi");

            await _service.ProvisionAsync(new CompleteSetupDto
            {
                Token = "multi-token",
                Password = "password123",
                ConfirmPassword = "password123",
                FullName = "Alex Owner",
                GroupName = "Golden Group",
                BusinessCategory = "multi-site",
                PrimaryPhone = "07911123456",
                Locations =
                [
                    new CompleteSetupDto.LocationItem
                    {
                        LocationName = "Site A",
                        Address = "1 High Street"
                    },
                    new CompleteSetupDto.LocationItem
                    {
                        LocationName = "Site B",
                        Address = "2 High Street"
                    }
                ]
            });

            var tokens = await _context.RestaurantLocations
                .Select(x => x.LinkToken)
                .ToListAsync();

            Assert.Equal(2, tokens.Count);
            Assert.Equal(tokens.Distinct().Count(), tokens.Count);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task SeedTrialRequestAsync(
            string approvalToken,
            bool isApproved = true,
            bool isAccountCreated = false,
            DateTime? inviteExpiresAt = null,
            string accountType = "Single"
        )
        {
            _context.TrialRequests.Add(new TrialRequest
            {
                BusinessName = "The Golden Fork",
                BusinessCategory = "takeaway",
                Locations = "1",
                FullName = "Alex Owner",
                Email = "owner@example.com",
                Mobile = "07911123456",
                Role = "Owner",
                Goal = "Grow repeat visits",
                TermsAccepted = true,
                IsEmailVerified = true,
                IsApproved = isApproved,
                Status = isApproved ? "Approved" : "EMAIL_VERIFIED",
                ApprovalToken = approvalToken,
                InviteExpiresAt = inviteExpiresAt ?? DateTime.UtcNow.AddDays(7),
                IsAccountCreated = isAccountCreated,
                AccountType = accountType
            });

            await _context.SaveChangesAsync();
        }
    }
}
