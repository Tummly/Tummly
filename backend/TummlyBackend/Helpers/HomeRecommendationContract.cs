namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Home Recommended next step allow-list + cache key contract (ticket 01).
    /// Cache TTL and key shape match Campaigns recommendation behaviour.
    /// </summary>
    public static class HomeRecommendationContract
    {
        public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

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

        public static readonly HashSet<string> AllowedTypes =
            new(
                NativeTypes.Concat(CampaignTypes).Append("none"),
                StringComparer.Ordinal
            );

        /// <summary>
        /// Home performance presets plus custom — closed wire set.
        /// </summary>
        public static readonly HashSet<string> AllowedOverviewDatePresets =
            new(StringComparer.Ordinal)
            {
                "last7",
                "last30",
                "thisMonth",
                "custom",
            };

        /// <summary>
        /// Domain primary CTA kinds for Home-native types.
        /// </summary>
        public static readonly HashSet<string> AllowedDomainActionKinds =
            new(StringComparer.Ordinal)
            {
                "open-feedback",
                "open-guest",
                "open-offer",
            };

        public static bool IsAllowedType(string type)
            => AllowedTypes.Contains(type);

        public static bool IsNativeType(string type)
            => NativeTypes.Contains(type);

        public static bool IsCampaignType(string type)
            => CampaignTypes.Contains(type);

        public static bool IsAllowedOverviewDatePreset(string preset)
            => AllowedOverviewDatePresets.Contains(preset);

        public static bool IsAllowedDomainActionKind(string kind)
            => AllowedDomainActionKinds.Contains(kind);

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

        /// <summary>
        /// Home has no all-time window — from/to are always required after normalize.
        /// </summary>
        public static void EnsureResolvedWindow(
            string preset,
            DateTime? fromUtc,
            DateTime? toUtc
        )
        {
            var normalized = NormalizePreset(preset);
            if (!IsAllowedOverviewDatePreset(normalized))
            {
                throw new ArgumentException(
                    $"overviewDatePreset '{preset}' is not in the Home recommendation allow-list."
                );
            }

            if (fromUtc is null || toUtc is null)
            {
                throw new ArgumentException(
                    "from and to are required for the Home performance window."
                );
            }

            if (fromUtc > toUtc)
            {
                throw new ArgumentException("from must be on or before to.");
            }
        }
    }
}
