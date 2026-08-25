namespace TummlyBackend.Helpers
{
    public static class PermissionLevelWire
    {
        public const string NoAccess = "No access";
        public const string View = "View";
        public const string Scoped = "Scoped";
        public const string Manage = "Manage";

        public static string Format(PermissionLevel level)
        {
            return level switch
            {
                PermissionLevel.View => View,
                PermissionLevel.Scoped => Scoped,
                PermissionLevel.Manage => Manage,
                _ => NoAccess,
            };
        }

        public static bool TryParse(string? raw, out PermissionLevel level)
        {
            switch (raw)
            {
                case NoAccess:
                    level = PermissionLevel.NoAccess;
                    return true;
                case View:
                    level = PermissionLevel.View;
                    return true;
                case Scoped:
                    level = PermissionLevel.Scoped;
                    return true;
                case Manage:
                    level = PermissionLevel.Manage;
                    return true;
                default:
                    level = PermissionLevel.NoAccess;
                    return false;
            }
        }
    }
}
