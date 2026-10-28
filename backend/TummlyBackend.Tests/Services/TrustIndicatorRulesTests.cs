using TummlyBackend.DTOs.Admin;
using TummlyBackend.TrustIndicators;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class TrustIndicatorRulesTests
    {
        [Fact]
        public void LondonToday_UsesTheLondonCalendarDay_NotUtc()
        {
            // 23:30 UTC on 30 June is already 1 July in London (BST).
            var today = TrustIndicatorRules.LondonToday(
                new DateTime(2026, 6, 30, 23, 30, 0, DateTimeKind.Utc)
            );

            Assert.Equal(new DateOnly(2026, 7, 1), today);
        }

        [Fact]
        public void IsLive_ExpiryDayIsStillLive_TheDayAfterIsNot()
        {
            var today = new DateOnly(2026, 10, 28);

            Assert.True(TrustIndicatorRules.IsLive(true, new DateOnly(2026, 10, 28), today));
            Assert.False(TrustIndicatorRules.IsLive(true, new DateOnly(2026, 10, 27), today));
            Assert.True(TrustIndicatorRules.IsLive(true, null, today));
        }

        [Fact]
        public void IsLive_DisabledIsNeverLive()
        {
            Assert.False(TrustIndicatorRules.IsLive(false, null, new DateOnly(2026, 10, 28)));
        }

        [Fact]
        public void Normalize_BlanksOptionalTextToNull_AndClearsCtaTargetForNone()
        {
            var errors = new List<string>();

            var result = TrustIndicatorRules.Normalize(
                new TrustIndicatorEditableValues
                {
                    Enabled = false,
                    CtaType = "None",
                    CtaTarget = "https://example.com/ignored",
                    EvidenceUrl = "   ",
                    IconKey = "  ",
                    AltText = null,
                    Tooltip = " Hover text ",
                },
                errors
            );

            Assert.Empty(errors);
            Assert.Null(result.CtaTarget);
            Assert.Null(result.EvidenceUrl);
            Assert.Null(result.IconKey);
            Assert.Equal(string.Empty, result.AltText);
            Assert.Equal("Hover text", result.Tooltip);
        }

        [Fact]
        public void Normalize_RejectsNonHttpsEvidenceAndExternalProofLinks()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                new TrustIndicatorEditableValues
                {
                    CtaType = "ExternalProof",
                    CtaTarget = "http://proof.example/x",
                    EvidenceUrl = "ftp://proof.example/x",
                },
                errors
            );

            Assert.Contains("External proof CTA needs an https link.", errors);
            Assert.Contains("Evidence URL must be an https link.", errors);
        }

        [Fact]
        public void Normalize_InternalPageMustBeARootPath()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                new TrustIndicatorEditableValues { CtaType = "InternalPage", CtaTarget = "//evil.example" },
                errors
            );

            Assert.Contains("Internal page CTA needs a path that starts with /.", errors);
        }

        [Fact]
        public void Normalize_ImageNeedsAltText()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                new TrustIndicatorEditableValues
                {
                    CtaType = "None",
                    ImageUrl = "https://cdn.example/badge.png",
                    AltText = "  ",
                },
                errors
            );

            Assert.Contains("Alt text is required when an image is set.", errors);
        }

        [Fact]
        public void Normalize_RejectsUnknownCtaType()
        {
            var errors = new List<string>();

            var result = TrustIndicatorRules.Normalize(
                new TrustIndicatorEditableValues { CtaType = "ExternalProof, None" },
                errors
            );

            Assert.Contains("CTA type must be None, ExternalProof or InternalPage.", errors);
            Assert.Equal("None", result.CtaType);
        }
    }
}
