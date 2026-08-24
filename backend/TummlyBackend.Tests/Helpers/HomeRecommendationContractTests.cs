using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class HomeRecommendationContractTests
    {
        [Fact]
        public void AllowedTypes_MatchGrillingLock_WithoutReportsOrSetup()
        {
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "review-open-feedback",
                    "thank-or-follow-guest",
                    "promote-or-fix-offer",
                    "thank-recent-guests",
                    "re-engage",
                    "recovery-follow-up",
                    "none",
                },
                HomeRecommendationContract.AllowedTypes
            );
            Assert.False(HomeRecommendationContract.IsAllowedType("weekly-brief-ready"));
            Assert.False(HomeRecommendationContract.IsAllowedType("setup-checklist"));
            Assert.False(HomeRecommendationContract.IsAllowedType("quiet-time"));
        }

        [Fact]
        public void AllowedOverviewDatePresets_MatchHomePerformanceWindow()
        {
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "last7",
                    "last30",
                    "thisMonth",
                    "custom",
                },
                HomeRecommendationContract.AllowedOverviewDatePresets
            );
            Assert.False(
                HomeRecommendationContract.IsAllowedOverviewDatePreset("all-time")
            );
        }

        [Fact]
        public void AllowedDomainActionKinds_MatchCtaTable()
        {
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "open-feedback",
                    "open-guest",
                    "open-offer",
                },
                HomeRecommendationContract.AllowedDomainActionKinds
            );
            Assert.False(
                HomeRecommendationContract.IsAllowedDomainActionKind(
                    "open-campaign-draft"
                )
            );
        }

        [Fact]
        public void CacheTtl_IsThirtyMinutes_MatchingCampaigns()
        {
            Assert.Equal(TimeSpan.FromMinutes(30), HomeRecommendationContract.CacheTtl);
        }

        [Fact]
        public void BuildCacheKey_NamedPreset_IgnoresExactFromTo()
        {
            var key = HomeRecommendationContract.BuildCacheKey(
                operatorUserId: 7,
                locationId: 42,
                preset: "7days",
                fromUtc: new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
                toUtc: new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc)
            );
            Assert.Equal("home-recommendation:7:42:7days", key);
        }

        [Fact]
        public void BuildCacheKey_DifferentPeriods_DoNotCollide()
        {
            var seven = HomeRecommendationContract.BuildCacheKey(
                7,
                42,
                "7days",
                null,
                null
            );
            var thirty = HomeRecommendationContract.BuildCacheKey(
                7,
                42,
                "30days",
                null,
                null
            );
            Assert.Equal("home-recommendation:7:42:7days", seven);
            Assert.Equal("home-recommendation:7:42:30days", thirty);
            Assert.NotEqual(seven, thirty);
        }

        [Fact]
        public void BuildCacheKey_Custom_UsesUtcCalendarDays()
        {
            var key = HomeRecommendationContract.BuildCacheKey(
                operatorUserId: 7,
                locationId: 42,
                preset: "custom",
                fromUtc: new DateTime(2026, 7, 12, 10, 15, 0, DateTimeKind.Utc),
                toUtc: new DateTime(2026, 7, 18, 22, 45, 0, DateTimeKind.Utc)
            );
            Assert.Equal("home-recommendation:7:42:custom:2026-07-12:2026-07-18", key);
        }

        [Fact]
        public void EnsureResolvedWindow_RequiresFromAndTo()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                HomeRecommendationContract.EnsureResolvedWindow(
                    "last7",
                    fromUtc: null,
                    toUtc: null
                )
            );
            Assert.Contains("from and to are required", ex.Message);
        }

        [Fact]
        public void EnsureResolvedWindow_RejectsUnknownPreset()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                HomeRecommendationContract.EnsureResolvedWindow(
                    "all-time",
                    fromUtc: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    toUtc: new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc)
                )
            );
            Assert.Contains("allow-list", ex.Message);
        }

        [Theory]
        [InlineData("review-open-feedback", true, false)]
        [InlineData("thank-recent-guests", false, true)]
        [InlineData("none", false, false)]
        public void NativeAndCampaignPartitions(
            string type,
            bool isNative,
            bool isCampaign
        )
        {
            Assert.Equal(isNative, HomeRecommendationContract.IsNativeType(type));
            Assert.Equal(isCampaign, HomeRecommendationContract.IsCampaignType(type));
            Assert.True(HomeRecommendationContract.IsAllowedType(type));
        }
    }
}
