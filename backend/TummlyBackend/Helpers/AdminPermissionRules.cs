namespace TummlyBackend.Helpers
{
    public static class AdminPermissionRules
    {
        public static readonly IReadOnlySet<string> RestaurantWideAreas =
            new HashSet<string>
            {
                OperatorAreaIds.AccountWorkspace,
                OperatorAreaIds.TeamPermissions,
                OperatorAreaIds.BillingCredits,
                OperatorAreaIds.PrivacyConsent,
            };

        public static bool IsLegal(string areaId, PermissionLevel level)
        {
            if (!OperatorAreaIds.All.Contains(areaId))
            {
                return false;
            }

            if (level == PermissionLevel.NoAccess)
            {
                return areaId is not (
                    OperatorAreaIds.AccountWorkspace
                    or OperatorAreaIds.TeamPermissions
                );
            }

            if (level == PermissionLevel.Scoped)
            {
                return !RestaurantWideAreas.Contains(areaId);
            }

            return level is PermissionLevel.View or PermissionLevel.Manage;
        }
    }
}
