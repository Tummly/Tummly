using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class LocationAssignedManagerTests
    {
        [Fact]
        public void MapByLocationId_UsesActiveLocationManagerMembership()
        {
            var user = new User { Id = 7, FullName = "Aisha Khan" };
            var membership = new RestaurantMembership
            {
                Id = 1,
                UserId = 7,
                User = user,
                PermissionRole = PermissionRoles.LocationManager,
                Status = MembershipStatus.Active,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson = "[10,20]",
            };

            var map = LocationAssignedManager.MapByLocationId([membership]);

            Assert.Equal(7, map[10].UserId);
            Assert.Equal("Aisha Khan", map[10].FullName);
            Assert.Equal(7, map[20].UserId);
            Assert.False(map.ContainsKey(99));
        }

        [Fact]
        public void MapByLocationId_KeepsLowestUserIdOnConflict()
        {
            var first = new RestaurantMembership
            {
                Id = 1,
                UserId = 3,
                User = new User { Id = 3, FullName = "First" },
                PermissionRole = PermissionRoles.LocationManager,
                Status = MembershipStatus.Active,
                NamedLocationIdsJson = "[10]",
            };
            var second = new RestaurantMembership
            {
                Id = 2,
                UserId = 9,
                User = new User { Id = 9, FullName = "Second" },
                PermissionRole = PermissionRoles.LocationManager,
                Status = MembershipStatus.Active,
                NamedLocationIdsJson = "[10]",
            };

            var map = LocationAssignedManager.MapByLocationId([second, first]);

            Assert.Equal(3, map[10].UserId);
            Assert.Equal("First", map[10].FullName);
        }
    }
}
