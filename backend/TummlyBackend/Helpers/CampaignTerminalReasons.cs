using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stable Campaign terminal reason codes written at fire close (Needs attention copy).
    /// </summary>
    public static class CampaignTerminalReasons
    {
        public const string SoftLocked = "soft-locked";
        public const string WorkspacePaused = "workspace-paused";
        public const string LocationInactive = "location-inactive";
        public const string LocationMissing = "location-missing";
        public const string ChannelMissing = "channel-missing";
        public const string EligibilityInvalid = "eligibility-invalid";
        public const string ZeroEligible = "zero-eligible";
        public const string CreditHoldExhausted = "credit-hold-exhausted";
        public const string NoAcceptedSends = "no-accepted-sends";
        public const string SettleFailed = "settle-failed";
        public const string CloseFailed = "close-failed";
        public const string MidSendStop = "mid-send-stop";

        public static string? FromSendStartGate(CampaignSendStartGateResult gate)
        {
            return gate switch
            {
                CampaignSendStartGateResult.SoftLocked => SoftLocked,
                CampaignSendStartGateResult.Blocked blocked => FromBlockedMessage(
                    blocked.Message
                ),
                _ => null,
            };
        }

        public static string FromBlockedMessage(string? message)
        {
            var text = (message ?? string.Empty).Trim();
            if (text.Contains("paused", StringComparison.OrdinalIgnoreCase))
            {
                return WorkspacePaused;
            }

            if (text.Contains("not active", StringComparison.OrdinalIgnoreCase))
            {
                return LocationInactive;
            }

            if (text.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return LocationMissing;
            }

            return MidSendStop;
        }
    }
}
