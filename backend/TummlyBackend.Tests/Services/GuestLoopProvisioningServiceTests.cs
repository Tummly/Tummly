using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Exceptions;
using TummlyBackend.Models;
using TummlyBackend.Billing.Pricebook;
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
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                    ["JwtSettings:Secret"] = "test-secret-key-that-is-long-enough-for-hmac-sha256",
                })
                .Build();

            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration
            );

            var qrCodeProvisioning = new QrCodeProvisioningService(
                _context,
                smartGuestLink
            );

            var packDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(packDir))
            {
                packDir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            _service = new GuestLoopProvisioningService(
                _context,
                qrCodeProvisioning,
                configuration,
                PricebookCatalog.LoadFromDirectory(packDir)
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
        public async Task ProvisionAsync_CreatesEntitiesAndFiveDefaultQrCodes()
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
            var qrCodes = await _context.QrCodes
                .Where(q => q.RestaurantLocationId == location.Id)
                .ToListAsync();

            Assert.Equal("owner@example.com", user.Email);
            Assert.Equal("+447911123456", user.PhoneNumber);
            Assert.True(user.TermsAccepted);
            Assert.Equal("The Golden Fork", restaurant.Name);
            Assert.Equal(restaurant.Id, guestLoop.RestaurantId);
            Assert.True(trialRequest.IsAccountCreated);

            Assert.Equal(5, qrCodes.Count);
            Assert.All(qrCodes, q => Assert.Equal(32, q.Token.Length));
            Assert.All(qrCodes, q => Assert.Equal(QrCodeStatus.Active, q.Status));
            Assert.Equal(5, qrCodes.Select(q => q.QrType).Distinct().Count());
            Assert.Contains(qrCodes, q => q.QrType == QrType.SmartGuest);
            Assert.Contains(qrCodes, q => q.QrType == QrType.CounterCard);
            Assert.Contains(qrCodes, q => q.QrType == QrType.PackagingSticker);
            Assert.Contains(qrCodes, q => q.QrType == QrType.DeliveryInsert);
            Assert.Contains(qrCodes, q => q.QrType == QrType.WindowSticker);

            var membership = await _context.RestaurantMemberships.SingleAsync();
            Assert.Equal(user.Id, membership.UserId);
            Assert.Equal(restaurant.Id, membership.RestaurantId);
            Assert.Equal(PermissionRoles.Owner, membership.PermissionRole);
            Assert.Equal(LocationScopeKind.AllLocations, membership.LocationScope);
            Assert.Equal(MembershipStatus.Active, membership.Status);
            Assert.Equal("[]", membership.NamedLocationIdsJson);

            var billingAccount = await _context.BillingAccounts.SingleAsync();
            Assert.Equal(restaurant.Id, billingAccount.RestaurantId);
            Assert.Equal(BillingSubscriptionPlans.Pilot, billingAccount.SubscriptionPlan);
            Assert.Equal(BillingStatuses.Pilot, billingAccount.BillingStatus);
            Assert.Null(billingAccount.BillingCycle);
            Assert.Null(billingAccount.RevolutCustomerId);
            Assert.Equal(StarterKitStates.Unused, billingAccount.StarterKitState);
            Assert.Equal("TUMMLY-UK-GBP-2026-08-V3", billingAccount.ContractedPricebookId);
        }

        [Fact]
        public async Task GenerateActivationCodeAsync_SetsHash_WhenAccountExists()
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

            await _service.GenerateActivationCodeAsync("provision-token");

            var user = await _context.Users.SingleAsync();

            Assert.NotNull(user.ActivationCodeHash);
            Assert.NotNull(user.ActivationCodeEncrypted);
            Assert.Null(user.ActivatedAt);
            Assert.Null(user.ActivationExpiresAt);
        }

        [Fact]
        public async Task GenerateActivationCodeAsync_IsIdempotent()
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

            await _service.GenerateActivationCodeAsync("provision-token");
            var firstHash =
                (await _context.Users.SingleAsync()).ActivationCodeHash;

            await _service.GenerateActivationCodeAsync("provision-token");
            var secondHash =
                (await _context.Users.SingleAsync()).ActivationCodeHash;

            Assert.Equal(firstHash, secondHash);
        }

        [Fact]
        public async Task ProvisionAsync_GeneratesUniqueQrCodeTokensAcrossLocations()
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

            var locationIds = await _context.RestaurantLocations
                .Select(x => x.Id)
                .ToListAsync();

            var tokens = await _context.QrCodes
                .Select(q => q.Token)
                .ToListAsync();

            Assert.Equal(2, locationIds.Count);
            Assert.Equal(10, tokens.Count);
            Assert.Equal(tokens.Distinct().Count(), tokens.Count);
        }

        [Fact]
        public async Task ProvisionAsync_CreatesAccountWithoutPhone_WhenNoneProvided()
        {
            await SeedTrialRequestAsync("no-phone-token", mobile: "");

            await _service.ProvisionAsync(new CompleteSetupDto
            {
                Token = "no-phone-token",
                Password = "password123",
                ConfirmPassword = "password123",
                FullName = "Alex Owner",
                GroupName = "The Golden Fork",
                BusinessCategory = "takeaway",
                Locations =
                [
                    new CompleteSetupDto.LocationItem
                    {
                        LocationName = "Main",
                        Address = "1 High Street",
                        Postcode = "M1 4AB",
                    }
                ]
            });

            var user = await _context.Users.SingleAsync();
            var restaurant = await _context.Restaurants.SingleAsync();

            Assert.Equal(string.Empty, user.PhoneNumber);
            Assert.Null(restaurant.PublicPhoneNumber);
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
            string accountType = "Single",
            string mobile = "07911123456"
        )
        {
            _context.TrialRequests.Add(new TrialRequest
            {
                BusinessName = "The Golden Fork",
                BusinessCategory = "takeaway",
                Locations = "1",
                FullName = "Alex Owner",
                Email = "owner@example.com",
                Mobile = mobile,
                MainLocation = "42 Market Street, Leeds",
                TownCity = "Leeds",
                Postcode = "LS1 1AA",
                Role = "Owner",
                Goal = "Grow repeat visits",
                TermsAccepted = true,
                IsEmailVerified = true,
                IsApproved = isApproved,
                Status = isApproved ? TrialRequestStatus.Approved : TrialRequestStatus.EmailVerified,
                ApprovalToken = approvalToken,
                InviteExpiresAt = inviteExpiresAt ?? DateTime.UtcNow.AddDays(7),
                IsAccountCreated = isAccountCreated,
                AccountType = accountType
            });

            await _context.SaveChangesAsync();
        }
    }
}
