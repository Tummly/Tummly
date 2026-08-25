using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class DefaultPermissionMatrix
    {
        private static readonly Dictionary<string, PermissionLevel> OwnerColumn =
            OperatorAreaIds.All.ToDictionary(
                id => id,
                _ => PermissionLevel.Manage
            );

        private static readonly IReadOnlyDictionary<
            string,
            IReadOnlyDictionary<string, PermissionLevel>
        > Cells = new Dictionary<string, IReadOnlyDictionary<string, PermissionLevel>>
        {
            [PermissionRoles.Owner] = OwnerColumn,
            [PermissionRoles.Admin] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.Manage,
                [OperatorAreaIds.Locations] = PermissionLevel.Manage,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.Manage,
                [OperatorAreaIds.Capture] = PermissionLevel.Manage,
                [OperatorAreaIds.Feedback] = PermissionLevel.Manage,
                [OperatorAreaIds.Guests] = PermissionLevel.Manage,
                [OperatorAreaIds.Campaigns] = PermissionLevel.Manage,
                [OperatorAreaIds.Offers] = PermissionLevel.Manage,
                [OperatorAreaIds.Reports] = PermissionLevel.Manage,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.Manage,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.View,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.Manage,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.Manage,
            },
            [PermissionRoles.AreaManager] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.Scoped,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.Scoped,
                [OperatorAreaIds.Feedback] = PermissionLevel.Scoped,
                [OperatorAreaIds.Guests] = PermissionLevel.Scoped,
                [OperatorAreaIds.Campaigns] = PermissionLevel.Scoped,
                [OperatorAreaIds.Offers] = PermissionLevel.Scoped,
                [OperatorAreaIds.Reports] = PermissionLevel.Scoped,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.Scoped,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.NoAccess,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.View,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.Scoped,
            },
            [PermissionRoles.LocationManager] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.Scoped,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.Scoped,
                [OperatorAreaIds.Feedback] = PermissionLevel.Scoped,
                [OperatorAreaIds.Guests] = PermissionLevel.Scoped,
                [OperatorAreaIds.Campaigns] = PermissionLevel.View,
                [OperatorAreaIds.Offers] = PermissionLevel.Scoped,
                [OperatorAreaIds.Reports] = PermissionLevel.Scoped,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.Scoped,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.NoAccess,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.View,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.Scoped,
            },
            [PermissionRoles.Marketing] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.Scoped,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.View,
                [OperatorAreaIds.Feedback] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Guests] = PermissionLevel.View,
                [OperatorAreaIds.Campaigns] = PermissionLevel.Scoped,
                [OperatorAreaIds.Offers] = PermissionLevel.Scoped,
                [OperatorAreaIds.Reports] = PermissionLevel.Scoped,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.NoAccess,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.View,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.View,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.Scoped,
            },
            [PermissionRoles.Staff] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.Scoped,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Feedback] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Guests] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Campaigns] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Offers] = PermissionLevel.Scoped,
                [OperatorAreaIds.Reports] = PermissionLevel.NoAccess,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.NoAccess,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.NoAccess,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.NoAccess,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.NoAccess,
            },
            [PermissionRoles.BillingAdmin] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.View,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Feedback] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Guests] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Campaigns] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Offers] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Reports] = PermissionLevel.View,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.Manage,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.Manage,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.View,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.View,
            },
            [PermissionRoles.ReportingOnly] = new Dictionary<string, PermissionLevel>
            {
                [OperatorAreaIds.AccountWorkspace] = PermissionLevel.View,
                [OperatorAreaIds.Locations] = PermissionLevel.Scoped,
                [OperatorAreaIds.TeamPermissions] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Capture] = PermissionLevel.View,
                [OperatorAreaIds.Feedback] = PermissionLevel.View,
                [OperatorAreaIds.Guests] = PermissionLevel.NoAccess,
                [OperatorAreaIds.Campaigns] = PermissionLevel.View,
                [OperatorAreaIds.Offers] = PermissionLevel.View,
                [OperatorAreaIds.Reports] = PermissionLevel.Scoped,
                [OperatorAreaIds.TummlyShop] = PermissionLevel.NoAccess,
                [OperatorAreaIds.BillingCredits] = PermissionLevel.NoAccess,
                [OperatorAreaIds.PrivacyConsent] = PermissionLevel.NoAccess,
                [OperatorAreaIds.AiAssistant] = PermissionLevel.View,
            },
        };

        public static PermissionLevel LevelFor(string permissionRole, string areaId)
        {
            if (
                Cells.TryGetValue(permissionRole, out var row)
                && row.TryGetValue(areaId, out var level)
            )
            {
                return level;
            }

            return PermissionLevel.NoAccess;
        }

        public static bool Meets(PermissionLevel cell, PermissionLevel required)
        {
            if (cell == PermissionLevel.NoAccess)
            {
                return false;
            }

            return required switch
            {
                PermissionLevel.View => true,
                PermissionLevel.Scoped =>
                    cell is PermissionLevel.Scoped or PermissionLevel.Manage,
                PermissionLevel.Manage => cell == PermissionLevel.Manage,
                _ => false,
            };
        }
    }
}
