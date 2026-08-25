using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class TeamPermissionsActor
    {
        public static readonly string[] InviteAndAssignRoles =
        [
            PermissionRoles.Admin,
            PermissionRoles.AreaManager,
            PermissionRoles.LocationManager,
            PermissionRoles.Marketing,
            PermissionRoles.Staff,
            PermissionRoles.BillingAdmin,
            PermissionRoles.ReportingOnly,
        ];

        public static bool IsBelowAdmin(string permissionRole)
        {
            return permissionRole is
                PermissionRoles.AreaManager
                or PermissionRoles.LocationManager
                or PermissionRoles.Marketing
                or PermissionRoles.Staff
                or PermissionRoles.BillingAdmin
                or PermissionRoles.ReportingOnly;
        }

        public static bool ActorMayReachPage(string permissionRole)
        {
            return permissionRole is PermissionRoles.Owner or PermissionRoles.Admin;
        }

        public static bool MayWriteTarget(
            string actorRole,
            bool actorCanManage,
            int actorUserId,
            int accountOwnerUserId,
            RestaurantMembership target
        )
        {
            if (!actorCanManage)
            {
                return false;
            }

            if (target.UserId == actorUserId)
            {
                return false;
            }

            if (target.UserId == accountOwnerUserId)
            {
                return false;
            }

            if (actorRole == PermissionRoles.Admin)
            {
                return IsBelowAdmin(target.PermissionRole);
            }

            return actorRole == PermissionRoles.Owner;
        }

        public static bool MayAssignRole(
            string actorRole,
            string newRole
        )
        {
            if (newRole == PermissionRoles.Owner)
            {
                return false;
            }

            if (actorRole == PermissionRoles.Admin)
            {
                return IsBelowAdmin(newRole);
            }

            return actorRole == PermissionRoles.Owner
                && InviteAndAssignRoles.Contains(newRole);
        }

        public static IReadOnlyList<string> ActionsFor(
            string actorRole,
            bool actorCanManage,
            int actorUserId,
            int accountOwnerUserId,
            RestaurantMembership target
        )
        {
            if (
                !MayWriteTarget(
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    accountOwnerUserId,
                    target
                )
            )
            {
                return [];
            }

            if (target.Status == MembershipStatus.Deactivated)
            {
                return ["reactivate", "remove"];
            }

            var actions = new List<string> { "change-role" };
            if (target.PermissionRole != PermissionRoles.Owner)
            {
                actions.Add("change-location");
            }

            actions.Add("deactivate");
            return actions;
        }
    }
}
