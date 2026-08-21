namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Home Recommended next step allow-list + cache key contract (ticket 01).
    /// Cache TTL and key shape match Campaigns recommendation behaviour.
    /// </summary>
    public static class HomeRecommendationContract
    {
        public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public static readonly HashSet<string> AllowedTypes =
            new(StringComparer.Ordinal)
            {
                "review-open-feedback",
                "thank-or-follow-guest",
                "promote-or-fix-offer",
                "thank-recent-guests",
                "re-engage",
                "recovery-follow-up",
                "none",
            };

        public static readonly HashSet<string> NativeTypes =
            new(StringComparer.Ordinal)
            {
                "review-open-feedback",
                "thank-or-follow-guest",
                "promote-or-fix-offer",
            };

        public static readonly HashSet<string> CampaignTypes =
            new(StringComparer.Ordinal)
            {
                "thank-recent-guests",
                "re-engage",
                "recovery-follow-up",
            };

        public static bool IsAllowedType(string type)
            => AllowedTypes.Contains(type);

        public static bool IsNativeType(string type)
            => NativeTypes.Contains(type);

        public static bool IsCampaignType(string type)
            => CampaignTypes.Contains(type);

        /// <summary>
        /// Builds a stable cache key from operator/location/selection identity.
        /// Named presets ignore exact from/to (UI recomputes those every call);
        /// custom uses day granularity so clock drift does not bust the key.
        /// </summary>
        public static string BuildCacheKey(
            int operatorUserId,
            int locationId,
            string preset,
            DateTime? fromUtc,
            DateTime? toUtc
        )
        {
            var normalized = NormalizePreset(preset);

            if (string.Equals(normalized, "custom", StringComparison.Ordinal)
                && fromUtc is not null
                && toUtc is not null)
            {
                var fromDay = fromUtc.Value.Date.ToString("yyyy-MM-dd");
                var toDay = toUtc.Value.Date.ToString("yyyy-MM-dd");
                return $"home-recommendation:{operatorUserId}:{locationId}:custom:{fromDay}:{toDay}";
            }

            return $"home-recommendation:{operatorUserId}:{locationId}:{normalized}";
        }

        public static string NormalizePreset(string? preset)
        {
            var key = (preset ?? "last7").Trim().ToLowerInvariant();
            return key.Length == 0 ? "last7" : key;
        }
    }
}
