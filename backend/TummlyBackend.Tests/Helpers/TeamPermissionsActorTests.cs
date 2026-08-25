using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class TeamPermissionsActorTests
    {
        [Fact]
        public void ViewActor_HasNoRowActions()
        {
            var target = new RestaurantMembership
            {
                UserId = 2,
                PermissionRole = PermissionRoles.Staff,
                Status = MembershipStatus.Active,
            };

            var actions = TeamPermissionsActor.ActionsFor(
                PermissionRoles.Admin,
                actorCanManage: false,
                actorUserId: 1,
                accountOwnerUserId: 9,
                target
            );

            Assert.Empty(actions);
        }

        [Fact]
        public void Owner_CannotWriteSelfOrAccountOwner()
        {
            var self = new RestaurantMembership
            {
                UserId = 1,
                PermissionRole = PermissionRoles.Admin,
                Status = MembershipStatus.Active,
            };
            var owner = new RestaurantMembership
            {
                UserId = 9,
                PermissionRole = PermissionRoles.Owner,
                Status = MembershipStatus.Active,
            };

            Assert.False(
                TeamPermissionsActor.MayWriteTarget(
                    PermissionRoles.Owner,
                    true,
                    1,
                    9,
                    self
                )
            );
            Assert.False(
                TeamPermissionsActor.MayWriteTarget(
                    PermissionRoles.Owner,
                    true,
                    1,
                    9,
                    owner
                )
            );
        }

        [Fact]
        public void Admin_CannotWriteAdminTarget()
        {
            var target = new RestaurantMembership
            {
                UserId = 2,
                PermissionRole = PermissionRoles.Admin,
                Status = MembershipStatus.Active,
            };

            Assert.False(
                TeamPermissionsActor.MayWriteTarget(
                    PermissionRoles.Admin,
                    true,
                    1,
                    9,
                    target
                )
            );
        }
    }
}
