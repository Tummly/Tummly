using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class CatalogOfferStatusTests
    {
        [Theory]
        [InlineData("archived", CatalogOfferValidity.Days30AfterIssue, null, "archived")]
        [InlineData("paused", CatalogOfferValidity.Days30AfterIssue, null, "paused")]
        [InlineData("draft", CatalogOfferValidity.Days30AfterIssue, null, "draft")]
        [InlineData("active", CatalogOfferValidity.Days30AfterIssue, null, "active")]
        public void ResolveEffectiveStatus_StoredPrecedence(
            string stored,
            CatalogOfferValidity validity,
            string? expiryIso,
            string expected
        )
        {
            DateOnly? expiry = expiryIso == null
                ? null
                : DateOnly.Parse(expiryIso);
            var today = new DateOnly(2026, 8, 11);
            Assert.Equal(
                expected,
                CatalogOfferStatus.ResolveEffectiveStatus(
                    stored,
                    validity,
                    expiry,
                    today
                )
            );
        }

        [Fact]
        public void ResolveEffectiveStatus_FixedDatePast_IsExpired()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.Equal(
                "expired",
                CatalogOfferStatus.ResolveEffectiveStatus(
                    "active",
                    CatalogOfferValidity.ChooseExpiryDate,
                    new DateOnly(2026, 8, 10),
                    today
                )
            );
        }

        [Fact]
        public void ResolveEffectiveStatus_FixedDateToday_IsActive()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.Equal(
                "active",
                CatalogOfferStatus.ResolveEffectiveStatus(
                    "active",
                    CatalogOfferValidity.ChooseExpiryDate,
                    today,
                    today
                )
            );
        }

        [Fact]
        public void VenueLocalToday_AppliesOffset()
        {
            var utc = new DateTime(2026, 8, 11, 22, 0, 0, DateTimeKind.Utc);
            Assert.Equal(
                new DateOnly(2026, 8, 12),
                CatalogOfferStatus.VenueLocalToday(utc, utcOffsetMinutes: 180)
            );
        }

        [Fact]
        public void IsNeedsAttention_WithinSevenDays_NotClosed()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.True(
                CatalogOfferStatus.IsNeedsAttentionRule(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(7),
                    "active",
                    today
                )
            );
            Assert.False(
                CatalogOfferStatus.IsNeedsAttentionRule(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(8),
                    "active",
                    today
                )
            );
            Assert.False(
                CatalogOfferStatus.IsNeedsAttentionRule(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(3),
                    "paused",
                    today
                )
            );
        }

        [Fact]
        public void IsNeedsAttention_OpenVoid_RequiresLiveAttach()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.False(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.Days30AfterIssue,
                    customExpiryDate: null,
                    effectiveStatus: "active",
                    venueLocalToday: today,
                    hasOpenVoidRequest: true,
                    liveAttachCount: 0
                )
            );
            Assert.True(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.Days30AfterIssue,
                    customExpiryDate: null,
                    effectiveStatus: "active",
                    venueLocalToday: today,
                    hasOpenVoidRequest: true,
                    liveAttachCount: 1
                )
            );
            Assert.False(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.Days30AfterIssue,
                    customExpiryDate: null,
                    effectiveStatus: "active",
                    venueLocalToday: today,
                    hasOpenVoidRequest: false,
                    liveAttachCount: 0
                )
            );
        }

        [Fact]
        public void IsNeedsAttention_ExpiringRequiresLiveAttach()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.False(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(2),
                    "active",
                    today,
                    hasOpenVoidRequest: false,
                    liveAttachCount: 0
                )
            );
            Assert.True(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(2),
                    "active",
                    today,
                    hasOpenVoidRequest: false,
                    liveAttachCount: 1
                )
            );
        }

        [Fact]
        public void IsNeedsAttention_DraftNeverQualifies()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.False(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(2),
                    "draft",
                    today,
                    hasOpenVoidRequest: true,
                    liveAttachCount: 1
                )
            );
        }

        [Fact]
        public void IsNeedsAttention_ExpiringOrVoid_BothPaths()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.True(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(2),
                    "active",
                    today,
                    hasOpenVoidRequest: false,
                    liveAttachCount: 1
                )
            );
            Assert.True(
                CatalogOfferStatus.IsNeedsAttention(
                    CatalogOfferValidity.ChooseExpiryDate,
                    today.AddDays(2),
                    "active",
                    today,
                    hasOpenVoidRequest: true,
                    liveAttachCount: 1
                )
            );
        }

        [Theory]
        [InlineData("draft", 0, "draft")]
        [InlineData("draft", 1, "active")]
        [InlineData("active", 0, "draft")]
        [InlineData("active", 2, "active")]
        [InlineData("paused", 0, "paused")]
        [InlineData("archived", 1, "archived")]
        public void ResolveStoredStatusFromLiveAttachCount_OpenDraftActive(
            string stored,
            int attaches,
            string expected
        )
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.Equal(
                expected,
                CatalogOfferStatus.ResolveStoredStatusFromLiveAttachCount(
                    stored,
                    CatalogOfferValidity.Days30AfterIssue,
                    customExpiryDate: null,
                    today,
                    attaches
                )
            );
        }

        [Fact]
        public void ResolveStoredStatusFromLiveAttachCount_ExpiredActive_Unchanged()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.Equal(
                "active",
                CatalogOfferStatus.ResolveStoredStatusFromLiveAttachCount(
                    "active",
                    CatalogOfferValidity.ChooseExpiryDate,
                    new DateOnly(2026, 8, 10),
                    today,
                    rawLiveAttachCount: 0
                )
            );
        }

        [Theory]
        [InlineData(0, "draft")]
        [InlineData(1, "active")]
        public void ResolveResumeStoredStatus(int attaches, string expected)
        {
            Assert.Equal(
                expected,
                CatalogOfferStatus.ResolveResumeStoredStatus(attaches)
            );
        }

        [Fact]
        public void IsAttachable_AllowsDraftAndActive()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.True(
                CatalogOfferStatus.IsAttachable(
                    "draft",
                    CatalogOfferValidity.Days30AfterIssue,
                    null,
                    today
                )
            );
            Assert.True(
                CatalogOfferStatus.IsAttachable(
                    "active",
                    CatalogOfferValidity.Days30AfterIssue,
                    null,
                    today
                )
            );
            Assert.False(
                CatalogOfferStatus.IsAttachable(
                    "paused",
                    CatalogOfferValidity.Days30AfterIssue,
                    null,
                    today
                )
            );
        }

        [Fact]
        public void IsAttachable_RejectsPastFixedExpiryDraftAndActive()
        {
            var today = new DateOnly(2026, 8, 11);
            Assert.False(
                CatalogOfferStatus.IsAttachable(
                    "draft",
                    CatalogOfferValidity.ChooseExpiryDate,
                    new DateOnly(2026, 8, 10),
                    today
                )
            );
            Assert.False(
                CatalogOfferStatus.IsAttachable(
                    "active",
                    CatalogOfferValidity.ChooseExpiryDate,
                    new DateOnly(2026, 8, 10),
                    today
                )
            );
        }

        [Theory]
        [InlineData("draft", "active", 0, true)]
        [InlineData("active", "active", 0, true)]
        [InlineData("active", "active", 1, false)]
        [InlineData("active", "paused", 0, false)]
        public void MatchesView_Drafts(
            string stored,
            string effective,
            int attaches,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                CatalogOfferStatus.MatchesView(
                    "drafts",
                    stored,
                    effective,
                    attaches
                )
            );
        }

        [Theory]
        [InlineData("active", "active", 1, true)]
        [InlineData("draft", "draft", 1, false)]
        [InlineData("active", "active", 0, false)]
        [InlineData("active", "expired", 2, false)]
        public void MatchesView_InFlight(
            string stored,
            string effective,
            int attaches,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                CatalogOfferStatus.MatchesView(
                    "in-flight",
                    stored,
                    effective,
                    attaches
                )
            );
        }

        [Fact]
        public void BuildDuplicateTitle_TruncatesToMax()
        {
            var longTitle = new string('a', 60);
            var copy = CatalogOfferStatus.BuildDuplicateTitle(longTitle, 60);
            Assert.Equal(60, copy.Length);
            Assert.Equal(new string('a', 60), copy);
        }

        [Fact]
        public void BuildDuplicateTitle_AppendsSuffix()
        {
            Assert.Equal(
                "Lunch deal (copy)",
                CatalogOfferStatus.BuildDuplicateTitle("Lunch deal", 60)
            );
        }
    }
}
