using System.Security.Cryptography;
using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stable identity of a Home campaign recommendation for Draft reuse.
    /// Counts and AI wording are not part of it. The recommendation type, goal,
    /// audience, channel, stance and attached Offer are.
    /// </summary>
    public static class CampaignRecommendationDraftKey
    {
        public const string Prefix = "rec-v1:";

        public static string Compute(
            string recommendationType,
            string? goalId,
            string? audienceKey,
            string? channel,
            string? offerStance,
            int? offerId
        )
        {
            var canonical = string.Join(
                "|",
                recommendationType,
                goalId ?? string.Empty,
                audienceKey ?? string.Empty,
                channel ?? string.Empty,
                offerStance ?? string.Empty,
                offerId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            );
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Prefix + Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
