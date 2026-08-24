using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Campaign Email / send-test From display name resolve (ticket 10 / lock 04).
    /// Guest response email does not use this helper.
    /// </summary>
    public static class CampaignSenderDisplayName
    {
        public static string Resolve(
            Restaurant? restaurant,
            string locationName
        )
        {
            var sender = restaurant?.DefaultCampaignSenderName?.Trim();
            if (!string.IsNullOrWhiteSpace(sender))
            {
                return sender;
            }

            var workspace = restaurant?.Name?.Trim();
            if (!string.IsNullOrWhiteSpace(workspace))
            {
                return workspace;
            }

            return locationName?.Trim() ?? string.Empty;
        }
    }
}
