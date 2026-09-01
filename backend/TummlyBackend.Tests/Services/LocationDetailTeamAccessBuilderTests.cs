using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationDetailTeamAccessBuilderTests
    {
        [Fact]
        public void HasAccessToLocation_OwnerWithAllLocations_IsInScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );

            Assert.True(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void HasAccessToLocation_AdminWithAllLocations_IsInScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.Admin,
                LocationScopeKind.AllLocations,
                "[]"
            );

            Assert.True(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void HasAccessToLocation_MarketingWithAllLocations_IsOutOfScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.Marketing,
                LocationScopeKind.AllLocations,
                "[]"
            );

            Assert.False(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void HasAccessToLocation_NamedListIncludingLocation_IsInScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.LocationManager,
                LocationScopeKind.NamedList,
                "[10,11]"
            );

            Assert.True(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void HasAccessToLocation_NamedListExcludingLocation_IsOutOfScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.LocationManager,
                LocationScopeKind.NamedList,
                "[11]"
            );

            Assert.False(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void HasAccessToLocation_AreaManagerWithNamedListIncludingLocation_IsInScope()
        {
            var membership = ActiveMembership(
                PermissionRoles.AreaManager,
                LocationScopeKind.NamedList,
                "[10,11]"
            );

            Assert.True(
                LocationDetailTeamAccessBuilder.HasAccessToLocation(
                    membership,
                    locationId: 10
                )
            );
        }

        [Fact]
        public void Build_OrdersByNameAndMapsAccessLabel()
        {
            var inScope = ActiveMembership(
                PermissionRoles.LocationManager,
                LocationScopeKind.NamedList,
                "[10]",
                userId: 2,
                fullName: "Zara"
            );
            inScope.Id = 22;
            var outOfScope = ActiveMembership(
                PermissionRoles.LocationManager,
                LocationScopeKind.NamedList,
                "[99]",
                userId: 3,
                fullName: "Excluded"
            );
            outOfScope.Id = 33;

            var rows = LocationDetailTeamAccessBuilder.Build(
                [inScope],
                locationId: 10,
                new Dictionary<int, string> { [10] = "Active Camden" },
                new Dictionary<int, DateTime>()
            );

            Assert.Single(rows);
            Assert.Equal(22, rows[0].MembershipId);
            Assert.Equal("Zara", rows[0].Name);
            Assert.Equal("Location Manager", rows[0].Role);
            Assert.Equal("Active Camden only", rows[0].AccessLabel);
            Assert.Null(rows[0].LastActiveAt);
        }

        private static RestaurantMembership ActiveMembership(
            string role,
            LocationScopeKind scope,
            string namedJson,
            int userId = 1,
            string fullName = "Member"
        )
        {
            return new RestaurantMembership
            {
                Id = userId,
                UserId = userId,
                RestaurantId = 1,
                PermissionRole = role,
                LocationScope = scope,
                NamedLocationIdsJson = namedJson,
                Status = MembershipStatus.Active,
                User = new User
                {
                    Id = userId,
                    FullName = fullName,
                    Email = $"member-{userId}@example.com",
                },
            };
        }
    }
}
