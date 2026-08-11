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
