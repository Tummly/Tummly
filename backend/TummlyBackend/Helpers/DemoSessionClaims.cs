using System.Security.Claims;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// JWT claim for public /product-demo sessions (Track F sandbox).
    /// </summary>
    public static class DemoSessionClaims
    {
        public const string ClaimType = "demo_session";
        public const string ClaimValue = "true";

        public const string SessionIdClaimType = "demo_session_id";

        /// <summary>
        /// Stable ProductDemoSeed Table Tent token for public guest-entry QR.
        /// </summary>
        public const string MaveroTableTentToken = "demo-mavero-table-tent";

        public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(8);

        /// <summary>
        /// The only Assistant prompt a demo session may send. Must match
        /// DEMO_TOUR_AI_PROMPT in src/lib/productDemo/demoTourSession.ts.
        /// </summary>
        public const string TourAssistantPrompt =
            "Summarise feedback from the last 30 days and tell me what needs attention.";

        public static bool IsDemoSession(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return string.Equals(
                user.FindFirstValue(ClaimType),
                ClaimValue,
                StringComparison.Ordinal
            );
        }

        public static Guid? TryGetSessionId(ClaimsPrincipal? user)
        {
            if (!IsDemoSession(user))
            {
                return null;
            }

            var raw = user!.FindFirstValue(SessionIdClaimType);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
