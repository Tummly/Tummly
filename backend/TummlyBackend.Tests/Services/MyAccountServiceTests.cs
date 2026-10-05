using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class MyAccountServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MyAccountService _service;
        private readonly TrackingEmailService _emailService = new();
        private readonly TrackingOperatorNotificationsService _notifications = new();

        public MyAccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new MyAccountService(
                _context,
                _emailService,
                _notifications,
                NullLogger<MyAccountService>.Instance
            );
        }

        [Fact]
        public async Task GetSnapshotAsync_IncludesLocationAccess_ForOwner()
        {
            var user = await SeedUserAsync("Alex Morgan", "owner@tummly.test");
            var restaurant = await SeedRestaurantAsync(user.Id, "The Green Kitchen");
            await SeedLocationAsync(restaurant.Id, "Borough");
            await SeedLocationAsync(restaurant.Id, "Soho");
            await SeedMembershipAsync(
                user.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );
            user.SelectedRestaurantId = restaurant.Id;
            await _context.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(user.Id);

            Assert.Equal("Alex Morgan", snapshot.FullName);
            Assert.Equal("owner@tummly.test", snapshot.Email);
            Assert.Equal("Owner", snapshot.Role);
            Assert.Equal("The Green Kitchen", snapshot.Organisation);
            Assert.False(snapshot.TwoFactorEnabled);
            Assert.Equal(2, snapshot.LocationAccess.Count);
            Assert.All(
                snapshot.LocationAccess,
                row => Assert.Equal("Full access", row.AccessLabel)
            );
            Assert.Contains(snapshot.LocationAccess, row => row.LocationName == "Borough");
            Assert.Contains(snapshot.LocationAccess, row => row.LocationName == "Soho");
        }

        [Fact]
        public async Task GetSnapshotAsync_FiltersNamedListLocations()
        {
            var owner = await SeedUserAsync("Owner User", "owner-named@tummly.test");
            var user = await SeedUserAsync("Sam Staff", "staff@tummly.test");
            var restaurant = await SeedRestaurantAsync(owner.Id, "Named Org");

            var borough = await SeedLocationAsync(restaurant.Id, "Borough");
            await SeedLocationAsync(restaurant.Id, "Soho");
            await SeedMembershipAsync(
                user.Id,
                restaurant.Id,
                PermissionRoles.LocationManager,
                LocationScopeKind.NamedList,
                $"[{borough.Id}]"
            );
            user.SelectedRestaurantId = restaurant.Id;
            await _context.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(user.Id);

            Assert.Equal("Location Manager", snapshot.Role);
            Assert.Single(snapshot.LocationAccess);
            Assert.Equal("Borough", snapshot.LocationAccess[0].LocationName);
        }

        [Fact]
        public async Task UpdateProfileAsync_SavesEditableFields()
        {
            var user = await SeedUserAsync("Old Name", "edit@tummly.test");

            var snapshot = await _service.UpdateProfileAsync(
                user.Id,
                new UpdateMyAccountProfileDto
                {
                    FullName = "New Name",
                    JobTitle = "Operations Manager",
                    PhoneNumber = "07911123456",
                }
            );

            Assert.Equal("New Name", snapshot.FullName);
            Assert.Equal("Operations Manager", snapshot.JobTitle);
            Assert.Equal("+447911123456", snapshot.PhoneNumber);

            var reloaded = await _context.Users.SingleAsync(row => row.Id == user.Id);
            Assert.Equal("New Name", reloaded.FullName);
            Assert.Equal("Operations Manager", reloaded.JobTitle);
            Assert.Equal("+447911123456", reloaded.PhoneNumber);
            Assert.Equal("edit@tummly.test", reloaded.Email);
        }

        [Fact]
        public async Task UpdateProfileAsync_DoesNotChangeEmail()
        {
            var user = await SeedUserAsync("Keep Email", "keep@tummly.test");

            await _service.UpdateProfileAsync(
                user.Id,
                new UpdateMyAccountProfileDto
                {
                    FullName = "Keep Email",
                    JobTitle = null,
                    PhoneNumber = null,
                }
            );

            var reloaded = await _context.Users.SingleAsync(row => row.Id == user.Id);
            Assert.Equal("keep@tummly.test", reloaded.Email);
        }

        [Fact]
        public async Task ChangePasswordAsync_RejectsWrongCurrentPassword()
        {
            var user = await SeedUserAsync("Alex Morgan", "pwd@tummly.test");

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ChangePasswordAsync(
                    user.Id,
                    new ChangePasswordDto
                    {
                        CurrentPassword = "WrongPassword1!",
                        NewPassword = "NewPassword1!",
                        ConfirmNewPassword = "NewPassword1!",
                    }
                )
            );

            Assert.Contains("Current password", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_RejectsConfirmMismatch()
        {
            var user = await SeedUserAsync("Alex Morgan", "mismatch@tummly.test");

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ChangePasswordAsync(
                    user.Id,
                    new ChangePasswordDto
                    {
                        CurrentPassword = "OldPassword1!",
                        NewPassword = "NewPassword1!",
                        ConfirmNewPassword = "OtherPassword1!",
                    }
                )
            );

            Assert.Contains("do not match", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_Succeeds_AndNotifies()
        {
            var user = await SeedUserAsync("Alex Morgan", "ok@tummly.test");

            await _service.ChangePasswordAsync(
                user.Id,
                new ChangePasswordDto
                {
                    CurrentPassword = "OldPassword1!",
                    NewPassword = "NewPassword1!",
                    ConfirmNewPassword = "NewPassword1!",
                }
            );

            var reloaded = await _context.Users.SingleAsync(row => row.Id == user.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", reloaded.PasswordHash));
            Assert.Single(_emailService.PasswordChangedEmails);
            Assert.Equal("ok@tummly.test", _emailService.PasswordChangedEmails[0].Email);
            Assert.Single(_notifications.Produced);
            Assert.Equal("password-changed", _notifications.Produced[0].Type);
        }

        private async Task<User> SeedUserAsync(string fullName, string email)
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                PhoneNumber = string.Empty,
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<Restaurant> SeedRestaurantAsync(int ownerUserId, string name)
        {
            var restaurant = new Restaurant
            {
                Name = name,
                OwnerUserId = ownerUserId,
                BillingContactUserId = ownerUserId,
                PrivacyContactUserId = ownerUserId,
                SupportContactUserId = ownerUserId,
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }

        private async Task<RestaurantLocation> SeedLocationAsync(
            int restaurantId,
            string locationName
        )
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = locationName,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        private async Task SeedMembershipAsync(
            int userId,
            int restaurantId,
            string permissionRole,
            LocationScopeKind scope,
            string namedJson
        )
        {
            _context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = userId,
                    RestaurantId = restaurantId,
                    PermissionRole = permissionRole,
                    LocationScope = scope,
                    NamedLocationIdsJson = namedJson,
                    Status = MembershipStatus.Active,
                }
            );
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string FirstName)> PasswordChangedEmails { get; } = [];

            public override Task SendPasswordChangedEmailAsync(
                string toEmail,
                string firstName
            )
            {
                PasswordChangedEmails.Add((toEmail, firstName));
                return Task.CompletedTask;
            }
        }
    }
}
