using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class PilotClockSyncTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly ApplicationDbContext _context;
        private readonly int _ownerId;
        private readonly int _memberId;
        private readonly int _removedMemberId;
        private readonly int _restaurantId;

        public PilotClockSyncTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(_options);

            _ownerId = AddUser("clock-owner@example.com");
            _memberId = AddUser("clock-member@example.com");
            _removedMemberId = AddUser("clock-removed@example.com");
            var owner = _context.Users.Single(row => row.Id == _ownerId);

            var restaurant = new Restaurant
            {
                Name = "Clock Cafe",
                AccountType = "Multi",
                OwnerUserId = _ownerId,
                BillingContactUserId = _ownerId,
                PrivacyContactUserId = _ownerId,
                SupportContactUserId = _ownerId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();
            _restaurantId = restaurant.Id;

            AddMembership(_ownerId, MembershipStatus.Active);
            AddMembership(_memberId, MembershipStatus.Active);
            AddMembership(_removedMemberId, MembershipStatus.Deactivated);
            owner.SelectedRestaurantId = _restaurantId;
            _context.SaveChanges();
        }

        [Fact]
        public async Task SetForRestaurant_SetsOwnerAndActiveMembers_NotDeactivated()
        {
            var endsAt = DateTime.UtcNow.AddDays(30);

            await PilotClockSync.SetForRestaurantAsync(
                _context,
                _restaurantId,
                endsAt,
                CancellationToken.None
            );
            await _context.SaveChangesAsync();

            var clocks = await ReadClocksAsync();
            Assert.Equal(endsAt, clocks[_ownerId]);
            Assert.Equal(endsAt, clocks[_memberId]);
            Assert.Null(clocks[_removedMemberId]);
        }

        [Fact]
        public async Task SetForRestaurant_DoesNotSave()
        {
            await PilotClockSync.SetForRestaurantAsync(
                _context,
                _restaurantId,
                DateTime.UtcNow.AddDays(30),
                CancellationToken.None
            );

            // A second context reads the store: nothing was saved yet.
            await using var reader = new ApplicationDbContext(_options);
            var stored = await reader.Users
                .AsNoTracking()
                .SingleAsync(row => row.Id == _ownerId);
            Assert.Null(stored.ActivationExpiresAt);
        }

        [Fact]
        public async Task ClearForRestaurant_ClearsOwnerAndActiveMembers()
        {
            await PilotClockSync.SetForRestaurantAsync(
                _context,
                _restaurantId,
                DateTime.UtcNow.AddDays(30),
                CancellationToken.None
            );
            await _context.SaveChangesAsync();

            await PilotClockSync.ClearForRestaurantAsync(
                _context,
                _restaurantId,
                CancellationToken.None
            );
            await _context.SaveChangesAsync();

            var clocks = await ReadClocksAsync();
            Assert.Null(clocks[_ownerId]);
            Assert.Null(clocks[_memberId]);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<Dictionary<int, DateTime?>> ReadClocksAsync()
        {
            await using var reader = new ApplicationDbContext(_options);
            return await reader.Users
                .AsNoTracking()
                .ToDictionaryAsync(row => row.Id, row => row.ActivationExpiresAt);
        }

        private int AddUser(string email)
        {
            var user = new User
            {
                FullName = "Clock User",
                Email = email,
                PasswordHash = "x",
                PhoneNumber = "5551234567",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user.Id;
        }

        private void AddMembership(int userId, MembershipStatus status)
        {
            _context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = userId,
                    RestaurantId = _restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = status,
                }
            );
            _context.SaveChanges();
        }
    }
}
