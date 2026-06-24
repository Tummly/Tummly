namespace TummlyBackend.Helpers
{
    public static class UserAgentHelper
    {
        public static string Summarize(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown device";
            }

            var browser = ResolveBrowser(userAgent);
            var operatingSystem = ResolveOperatingSystem(userAgent);

            return $"{browser} on {operatingSystem}";
        }

        private static string ResolveBrowser(string userAgent)
        {
            if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            {
                return "Microsoft Edge";
            }

            if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            {
                return "Chrome";
            }

            if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            {
                return "Firefox";
            }

            if (
                userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase)
                && !userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "Safari";
            }

            return "Unknown browser";
        }

        private static string ResolveOperatingSystem(string userAgent)
        {
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            {
                return "Windows";
            }

            if (userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
            {
                return "macOS";
            }

            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            {
                return "Android";
            }

            if (
                userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "iOS";
            }

            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            {
                return "Linux";
            }

            return "Unknown OS";
        }
    }
}
