using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;
using TummlyBackend.TrustIndicators;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class TrustIndicatorRulesTests
    {
        private static readonly DateOnly Today = new(2026, 10, 28);

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
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    CtaType = "None",
                    CtaTarget = "https://example.com/ignored",
                    CtaLabel = "   ",
                    EvidenceUrl = "   ",
                    IconKey = "  ",
                    AltText = null,
                    Tooltip = " Hover text ",
                },
                errors
            );

            Assert.Empty(errors);
            Assert.Null(result.CtaTarget);
            Assert.Null(result.CtaLabel);
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
                TrustEvidenceClass.VerifiedPublicRecord,
                Draft("VerifiedPublicRecord") with
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
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with { CtaType = "InternalPage", CtaTarget = "//evil.example" },
                errors
            );

            Assert.Contains("Internal page CTA needs a path that starts with /.", errors);
        }

        [Fact]
        public void Normalize_ImageNeedsAltText()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    ImageUrl = "https://cdn.example/badge.png",
                    ImageRightsNote = "Licensed for this site",
                    AltText = "  ",
                },
                errors
            );

            Assert.Contains("Alt text is required when an image is set.", errors);
        }

        [Fact]
        public void Normalize_ImageNeedsRightsNote()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    ImageUrl = "https://cdn.example/badge.png",
                    AltText = "Badge",
                    ImageRightsNote = "   ",
                },
                errors
            );

            Assert.Contains("Image rights note is required when an image is set.", errors);
        }

        [Fact]
        public void Normalize_RoadmapRejectsImage()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.Roadmap,
                Draft("Roadmap") with
                {
                    ImageUrl = "https://cdn.example/badge.png",
                    AltText = "Badge",
                    ImageRightsNote = "Licensed for this site",
                },
                errors
            );

            Assert.Contains("Roadmap items cannot have an image.", errors);
        }

        [Fact]
        public void Normalize_RejectsUnknownCtaType()
        {
            var errors = new List<string>();

            var result = TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with { CtaType = "ExternalProof, None" },
                errors
            );

            Assert.Contains("CTA type must be None, ExternalProof or InternalPage.", errors);
            Assert.Equal("None", result.CtaType);
        }

        [Fact]
        public void Normalize_RejectsUnknownStatus()
        {
            var unknown = new List<string>();
            var wrongCase = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("Bogus"),
                unknown
            );
            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("qapending"),
                wrongCase
            );

            Assert.Contains(
                "Status must be one of VerifiedPublicRecord, VerifiedCredential, Roadmap, QaPending, QaPassed or UnderReview.",
                unknown
            );
            Assert.Contains(
                "Status must be one of VerifiedPublicRecord, VerifiedCredential, Roadmap, QaPending, QaPassed or UnderReview.",
                wrongCase
            );
        }

        [Fact]
        public void Normalize_RejectsStatusNotAllowedForClass()
        {
            var roadmapPassed = new List<string>();
            var publicRecordCredential = new List<string>();
            var ownControlRoadmap = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.Roadmap,
                Draft("QaPassed"),
                roadmapPassed
            );
            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.VerifiedPublicRecord,
                Draft("VerifiedCredential"),
                publicRecordCredential
            );
            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("Roadmap"),
                ownControlRoadmap
            );

            Assert.Contains("Status QaPassed is not allowed for Roadmap items.", roadmapPassed);
            Assert.Contains(
                "Status VerifiedCredential is not allowed for VerifiedPublicRecord items.",
                publicRecordCredential
            );
            Assert.Contains(
                "Status Roadmap is not allowed for OwnControlQaPending items.",
                ownControlRoadmap
            );
        }

        [Fact]
        public void Normalize_CookieSettingsNullsTarget()
        {
            var errors = new List<string>();

            var result = TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    CtaType = "CookieSettings",
                    CtaTarget = "/privacy",
                    CtaLabel = "Manage cookie choices",
                },
                errors
            );

            Assert.Empty(errors);
            Assert.Equal("CookieSettings", result.CtaType);
            Assert.Null(result.CtaTarget);
            Assert.Equal("Manage cookie choices", result.CtaLabel);
        }

        [Fact]
        public void Normalize_LabelLimits()
        {
            var errors = new List<string>();

            var result = TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    StatusLabel = new string('s', 61),
                    CtaType = "CookieSettings",
                    CtaLabel = new string('c', 61),
                    ImageUrl = "https://cdn.example/badge.png",
                    AltText = "Badge",
                    ImageRightsNote = new string('r', 301),
                },
                errors
            );

            Assert.Contains("Status label must be 60 characters or fewer.", errors);
            Assert.Contains("CTA label must be 60 characters or fewer.", errors);
            Assert.Contains("Image rights note must be 300 characters or fewer.", errors);
            Assert.Equal(new string('c', 61), result.CtaLabel);
        }

        [Fact]
        public void Normalize_AcceptsLabelsAtTheirLimits()
        {
            var errors = new List<string>();

            TrustIndicatorRules.Normalize(
                TrustEvidenceClass.OwnControlQaPending,
                Draft("QaPending") with
                {
                    StatusLabel = new string('s', 60),
                    CtaLabel = new string('c', 60),
                },
                errors
            );

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("VerifiedPublicRecord", "VerifiedPublicRecord", "Verified")]
        [InlineData("VerifiedPublicRecord", "UnderReview", "UnderReview")]
        [InlineData("VerifiedSelfAssessment", "VerifiedCredential", "Verified")]
        [InlineData("VerifiedSelfAssessment", "UnderReview", "UnderReview")]
        [InlineData("Roadmap", "Roadmap", "Roadmap")]
        [InlineData("OwnControlQaPending", "QaPending", "UnderReview")]
        [InlineData("OwnControlQaPending", "QaPassed", "Verified")]
        public void PublicStatus_MapsEveryCanonicalValue(
            string evidenceClass,
            string status,
            string expected
        )
        {
            Assert.Equal(
                expected,
                TrustIndicatorRules.PublicStatus(Enum.Parse<TrustEvidenceClass>(evidenceClass), status)
            );
        }

        [Fact]
        public void PublicStatus_NullForMissingUnknownOrWrongClass()
        {
            Assert.Null(TrustIndicatorRules.PublicStatus(TrustEvidenceClass.VerifiedPublicRecord, null));
            Assert.Null(TrustIndicatorRules.PublicStatus(TrustEvidenceClass.VerifiedPublicRecord, "Bogus"));
            Assert.Null(
                TrustIndicatorRules.PublicStatus(TrustEvidenceClass.VerifiedPublicRecord, "verifiedpublicrecord")
            );
            Assert.Null(TrustIndicatorRules.PublicStatus(TrustEvidenceClass.VerifiedPublicRecord, "Roadmap"));
            Assert.Null(TrustIndicatorRules.PublicStatus(TrustEvidenceClass.Roadmap, "QaPassed"));
            Assert.Null(
                TrustIndicatorRules.PublicStatus(TrustEvidenceClass.OwnControlQaPending, "VerifiedPublicRecord")
            );
        }

        [Fact]
        public void PublicCtaAllowed_NeedsPublicStatusVerified()
        {
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPending", TrustCtaType.InternalPage, "/privacy")
                )
            );
            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "/privacy")
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_ExternalProofOnlyForProofClasses()
        {
            const string proof = "https://ico.example/x";

            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta("VerifiedPublicRecord", TrustCtaType.ExternalProof, proof, proof)
                )
            );
            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedSelfAssessment,
                    Cta("VerifiedCredential", TrustCtaType.ExternalProof, proof, proof)
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.ExternalProof, proof, proof)
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_ExternalProofTargetMustEqualEvidenceUrl()
        {
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta(
                        "VerifiedPublicRecord",
                        TrustCtaType.ExternalProof,
                        "https://ico.example/other",
                        "https://ico.example/x"
                    )
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta("VerifiedPublicRecord", TrustCtaType.ExternalProof, "https://ico.example/x", null)
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta(
                        "VerifiedPublicRecord",
                        TrustCtaType.ExternalProof,
                        "http://ico.example/x",
                        "http://ico.example/x"
                    )
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_InternalPageOnlyForOwnControlWithRootPath()
        {
            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "/privacy")
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "//evil.example")
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "https://ico.example")
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta("VerifiedPublicRecord", TrustCtaType.InternalPage, "/privacy")
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_CookieSettingsOnlyForOwnControlWithNoTarget()
        {
            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.CookieSettings, null)
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.CookieSettings, "/privacy")
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.VerifiedPublicRecord,
                    Cta("VerifiedPublicRecord", TrustCtaType.CookieSettings, null)
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_NeedsLabelOfOneToSixtyCharacters()
        {
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "/privacy", label: null)
                )
            );
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "/privacy", label: new string('x', 61))
                )
            );
            Assert.True(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.InternalPage, "/privacy", label: new string('x', 60))
                )
            );
        }

        [Fact]
        public void PublicCtaAllowed_NoneIsNotACta()
        {
            Assert.False(
                TrustIndicatorRules.PublicCtaAllowed(
                    TrustEvidenceClass.OwnControlQaPending,
                    Cta("QaPassed", TrustCtaType.None, null)
                )
            );
        }

        [Fact]
        public void PublishGate_EnabledVerifiedNeedsEvidenceUrlAndIssuer()
        {
            var item = SeedRow(1, enabled: true);
            item.EvidenceUrl = null;
            item.Issuer = null;

            var errors = Gate(item);

            Assert.Contains("Item 01: a verified item needs an https evidence URL.", errors);
            Assert.Contains("Item 01: a verified item needs an issuer.", errors);
            Assert.Empty(Gate(SeedRow(1, enabled: true)));
        }

        [Fact]
        public void PublishGate_Item02WithoutEvidenceUrlCannotBeEnabled()
        {
            // The seed has no completion record URL yet, so item 02 cannot go live.
            var errors = Gate(SeedRow(2, enabled: true));

            Assert.Equal(new[] { "Item 02: a verified item needs an https evidence URL." }, errors);
        }

        [Fact]
        public void PublishGate_CtaThatPublicWouldDropIsAnError()
        {
            var registration = SeedRow(1, enabled: true);
            registration.CtaType = TrustCtaType.InternalPage;
            registration.CtaTarget = "/privacy";
            registration.CtaLabel = "Read more";
            var cookies = SeedRow(10, enabled: true);
            cookies.CtaType = TrustCtaType.CookieSettings;
            cookies.CtaLabel = "Manage cookie choices";

            var errors = Gate(registration, cookies);

            Assert.Equal(
                new[]
                {
                    "Item 01: the call to action does not meet the public rules.",
                    "Item 10: the call to action does not meet the public rules.",
                },
                errors
            );
        }

        [Fact]
        public void PublishGate_RoadmapStatusLabelCannotSayCertified()
        {
            var roadmap = SeedRow(3, enabled: true);
            roadmap.StatusLabel = "Certified roadmap item";

            Assert.Equal(
                new[] { "Item 03: Roadmap items cannot use verified or certified wording." },
                Gate(roadmap)
            );
        }

        [Fact]
        public void PublishGate_StatusLabelIsRequiredExceptForUnderReview()
        {
            var registration = SeedRow(1, enabled: true);
            registration.StatusLabel = string.Empty;

            Assert.Equal(new[] { "Item 01: a status label is required." }, Gate(registration));
        }

        [Fact]
        public void PublishGate_AllSeedRowsEnabledPassExcept02()
        {
            var rows = TrustIndicatorCatalogSeed.Items
                .Select(item => SeedRow(item.ItemNumber, enabled: true))
                .ToList();

            var errors = TrustIndicatorRules.PublishGateErrors(rows, Today);

            var only = Assert.Single(errors);
            Assert.StartsWith("Item 02: ", only);
        }

        private static TrustIndicatorEditableValues Draft(string status) =>
            new()
            {
                Enabled = false,
                CtaType = "None",
                Status = status,
                StatusLabel = "Under review",
                AltText = string.Empty,
                Tooltip = string.Empty,
            };

        private static TrustIndicatorEditableValues Cta(
            string status,
            TrustCtaType type,
            string? target,
            string? evidenceUrl = null,
            string? label = "View record"
        ) =>
            new()
            {
                Enabled = true,
                Status = status,
                CtaType = type.ToString(),
                CtaTarget = target,
                EvidenceUrl = evidenceUrl,
                CtaLabel = label,
            };

        private static TrustIndicator SeedRow(int itemNumber, bool enabled = false)
        {
            var row = TrustIndicatorCatalogSeed.CreateRow(
                TrustIndicatorCatalogSeed.Items.Single(item => item.ItemNumber == itemNumber),
                DateTime.UtcNow
            );
            row.Enabled = enabled;
            return row;
        }

        private static IReadOnlyList<string> Gate(params TrustIndicator[] items) =>
            TrustIndicatorRules.PublishGateErrors(items, Today);
    }
}
