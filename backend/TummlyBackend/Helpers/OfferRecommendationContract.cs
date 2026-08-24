namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Offer recommendation allow-list + cache key (ticket 02).
    /// Cache TTL matches Home Recommended next step and Campaign recommendation.
    /// </summary>
    public static class OfferRecommendationContract
    {
        public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public const string TypeNone = "none";
        public const string TypePromote = "promote-this-offer";
        public const string TypeFix = "fix-this-offer";

        public static readonly HashSet<string> AllowedTypes =
            new(StringComparer.Ordinal)
            {
                TypePromote,
                TypeFix,
                TypeNone,
            };

        public static bool IsAllowedType(string type)
            => AllowedTypes.Contains(type);

        public static bool IsNone(string type)
            => string.Equals(type, TypeNone, StringComparison.Ordinal);

        public static string BuildCacheKey(
            int operatorUserId,
            int locationId,
            int offerId,
            string reportingPeriod
        )
        {
            var period = WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                reportingPeriod
            );
            return $"offer-recommendation:{operatorUserId}:{locationId}:{offerId}:{period}";
        }
    }
}
