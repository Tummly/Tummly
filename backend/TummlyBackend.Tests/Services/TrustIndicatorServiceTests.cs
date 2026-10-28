using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using TummlyBackend.TrustIndicators;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class TrustIndicatorServiceTests
    {
        private const int EditorId = 11;
        private const int PublisherId = 22;
        private const int ThirdAdminId = 33;

        private static readonly string[] CanonicalTitles =
        [
            "ICO Data Protection Registration",
            "ICO Data Protection Essentials",
            "Cyber Essentials",
            "Cyber Essentials Plus",
            "ISO/IEC 27001",
            "IASME Cyber Assurance Level One",
            "Good Business Charter",
            "ISO/IEC 27701:2025",
            "UK GDPR & Data Protection",
            "Cookie & Privacy Choices",
            "Permission-Based Marketing",
            "Guest Data Rights",
            "UK Registered Company",
            "Encrypted HTTPS Connections",
            "Human-Controlled AI",
        ];

        private static readonly string[] CanonicalClasses =
        [
            "VerifiedPublicRecord",
            "VerifiedSelfAssessment",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "OwnControlQaPending",
            "OwnControlQaPending",
            "OwnControlQaPending",
            "OwnControlQaPending",
            "VerifiedPublicRecord",
            "OwnControlQaPending",
            "OwnControlQaPending",
        ];

        private readonly ApplicationDbContext _context;
        private readonly AdjustableTimeProvider _clock;
        private readonly TrustIndicatorService _sut;

        public TrustIndicatorServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero));
            _sut = new TrustIndicatorService(
                _context,
                new AdminAuditService(_context, _clock),
                _clock
            );
            TrustIndicatorCatalogSeed.EnsureSeededAsync(_context).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Seed_CreatesFifteenCanonicalItems_AllDisabledWithoutEvidence()
        {
            var view = await _sut.GetAdminViewAsync();

            Assert.Equal(Enumerable.Range(1, 15).ToList(), view.Draft.Select(i => i.ItemNumber).ToList());
            Assert.Equal(CanonicalTitles.ToList(), view.Draft.Select(i => i.Title).ToList());
            Assert.Equal(CanonicalClasses.ToList(), view.Draft.Select(i => i.EvidenceClass).ToList());
            Assert.All(view.Draft, item =>
            {
                Assert.False(item.Values.Enabled);
                Assert.Equal("None", item.Values.CtaType);
                Assert.Null(item.Values.CtaTarget);
                Assert.Null(item.Values.EvidenceUrl);
            });
            Assert.Null(view.Published);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Seed_InsertsOnlyMissingItems_AndKeepsEdits()
        {
            Succeeded(await EditAsync(3, Plain() with { Tooltip = "Working towards Cyber Essentials" }));
            var row = await _context.TrustIndicators.SingleAsync(r => r.ItemNumber == 7);
            _context.TrustIndicators.Remove(row);
            await _context.SaveChangesAsync();

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);

            var view = await _sut.GetAdminViewAsync();
            Assert.Equal(15, view.Draft.Count);
            Assert.Equal("Good Business Charter", view.Draft[6].Title);
            Assert.Equal("Working towards Cyber Essentials", view.Draft[2].Values.Tooltip);
        }

        [Fact]
        public async Task DraftEdit_ChangesDraftOnly_AndAuditsBeforeAndAfter()
        {
            var item = Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));

            Assert.True(item.Values.Enabled);
            Assert.Equal("ExternalProof", item.Values.CtaType);
            Assert.Equal(EditorId, item.LastEditedByAdminUserId);

            var audit = Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorDraftEdit));
            Assert.Equal(AdminAuditTargetTypes.TrustIndicatorItem, audit.TargetType);
            Assert.Equal("1", audit.TargetId);
            Assert.Equal(EditorId, audit.ActorAdminUserId);
            Assert.Contains("\"Enabled\":false", audit.DetailJson);
            Assert.Contains("\"Enabled\":true", audit.DetailJson);
            Assert.Contains("https://ico.example/proof", audit.DetailJson);
            Assert.Null((await _sut.GetAdminViewAsync()).Published);
        }

        [Fact]
        public async Task DraftEdit_WithNoChange_WritesNoAuditRow()
        {
            var item = Succeeded(await EditAsync(1, Plain()));

            Assert.Null(item.LastEditedByAdminUserId);
            Assert.Empty(await AuditAsync(AdminAuditActions.TrustIndicatorDraftEdit));
        }

        [Fact]
        public async Task DraftEdit_InvalidLink_IsRefused_AndDraftStaysUnchanged()
        {
            var outcome = await EditAsync(1, ProofLink("http://insecure.example/proof"));

            Assert.False(outcome.Succeeded);
            Assert.Equal("validation_failed", outcome.Code);
            Assert.Contains("External proof CTA needs an https link.", outcome.Details!);
            var view = await _sut.GetAdminViewAsync();
            Assert.False(view.Draft[0].Values.Enabled);
            Assert.Empty(await AuditAsync(AdminAuditActions.TrustIndicatorDraftEdit));
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
        }

        [Fact]
        public async Task DraftEdit_AfterPublish_DoesNotChangePublicOutput()
        {
            await PublishValidAsync();

            Succeeded(await EditAsync(1, Plain()));

            var view = await _sut.GetAdminViewAsync();
            Assert.False(view.Draft[0].Values.Enabled);
            Assert.True(view.Published!.Items[0].Values.Enabled);
            var publicItems = await _sut.GetPublicAsync();
            var registration = Assert.Single(publicItems);
            Assert.Equal(1, registration.ItemNumber);
            Assert.Equal("https://ico.example/proof", registration.CtaTarget);
        }

        [Fact]
        public async Task Submit_RecordsSubmitter_AndAnyLaterEditClearsIt()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            var review = Succeeded(await _sut.SubmitDraftAsync(EditorId));
            Assert.Equal(EditorId, review.SubmittedByAdminUserId);
            Assert.NotNull(review.SubmittedAtUtc);
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorSubmit));

            Succeeded(await EditAsync(2, Plain() with { Tooltip = "Reviewed" }, PublisherId));

            var view = await _sut.GetAdminViewAsync();
            Assert.Null(view.Review.SubmittedByAdminUserId);
            Assert.Null(view.Review.SubmittedAtUtc);
        }

        [Fact]
        public async Task Publish_WithoutSubmission_IsRefused_NotSubmitted()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));

            var outcome = await PublishAsync(PublisherId, "key-not-submitted");

            Assert.Equal("not_submitted", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
        }

        [Fact]
        public async Task Publish_BySubmitter_IsDenied_SelfApprovalDenied()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(EditorId, "key-self");

            Assert.Equal("self_approval_denied", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task SecondSubmit_ByAnotherAdmin_IsRefused_SoFirstSubmitterCannotPublish()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(PublisherId));

            var second = await _sut.SubmitDraftAsync(ThirdAdminId);
            var denied = await PublishAsync(PublisherId, "key-first-submitter");

            Assert.Equal("already_submitted", second.Code);
            Assert.Equal("self_approval_denied", denied.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_ByLastDraftEditor_IsDenied_EvenWhenSomeoneElseSubmitted()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(PublisherId));

            var denied = await PublishAsync(EditorId, "key-last-editor");
            var allowed = Succeeded(await PublishAsync(ThirdAdminId, "key-third"));

            Assert.Equal("self_approval_denied", denied.Code);
            Assert.Equal(1, allowed.Version);
            Assert.Equal(ThirdAdminId, allowed.PublishedByAdminUserId);
        }

        [Fact]
        public async Task Publish_ByDifferentAdmin_CreatesVersion_RecordsApproverAndSubmitter()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var published = Succeeded(await PublishAsync(PublisherId, "key-1"));

            Assert.Equal(1, published.Version);
            Assert.Equal(PublisherId, published.PublishedByAdminUserId);
            Assert.Equal(PublisherId, published.ApprovedByAdminUserId);
            Assert.Equal(EditorId, published.SubmittedByAdminUserId);
            Assert.Equal("Evidence checked", published.Reason);
            Assert.Equal(15, published.Items.Count);

            var view = await _sut.GetAdminViewAsync();
            Assert.Null(view.Review.SubmittedByAdminUserId);
            Assert.Equal(1, view.Published!.Version);

            var audit = Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorPublish));
            Assert.Equal("1", audit.TargetId);
            Assert.Equal(PublisherId, audit.ActorAdminUserId);
        }

        [Fact]
        public async Task Publish_SameKeyTwice_ReturnsSameVersion_AndCreatesNothingNew()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var first = Succeeded(await PublishAsync(PublisherId, "key-repeat"));
            var second = Succeeded(await PublishAsync(PublisherId, "key-repeat"));

            Assert.Equal(first.Version, second.Version);
            Assert.Single(await _context.TrustIndicatorPublications.ToListAsync());
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorPublish));
        }

        [Fact]
        public async Task Publish_WithoutIdempotencyKey_IsRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await _sut.PublishAsync(PublisherId, "  ", "Evidence checked");

            Assert.Equal("idempotency_key_required", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_WithoutReason_IsRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await _sut.PublishAsync(PublisherId, "key-no-reason", "   ");

            Assert.Equal("validation_failed", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_EnabledExternalProofWithoutEvidenceUrl_IsRefused_AndPriorVersionStays()
        {
            await PublishValidAsync();
            Succeeded(await EditAsync(1, new TrustIndicatorEditableValues
            {
                Enabled = true,
                CtaType = "ExternalProof",
                CtaTarget = "https://ico.example/proof",
                AltText = string.Empty,
                Tooltip = string.Empty,
            }));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-no-evidence");

            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains(
                "Item 01: an enabled external proof CTA needs an evidence URL.",
                outcome.Details!
            );
            Assert.Single(await _context.TrustIndicatorPublications.ToListAsync());
            var registration = Assert.Single(await _sut.GetPublicAsync());
            Assert.Equal("https://ico.example/proof", registration.EvidenceUrl);
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
        }

        [Fact]
        public async Task Publish_RoadmapWithCta_OrVerifiedWording_IsRefused()
        {
            Succeeded(await EditAsync(3, new TrustIndicatorEditableValues
            {
                Enabled = false,
                CtaType = "InternalPage",
                CtaTarget = "/security",
                AltText = string.Empty,
                Tooltip = string.Empty,
            }));
            Succeeded(await EditAsync(9, Plain() with { Tooltip = "Verified by our team" }));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-roadmap");

            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains("Item 03: Roadmap items cannot have a CTA.", outcome.Details!);
            Assert.Contains(
                "Item 09: OwnControlQaPending items cannot use verified or certified wording.",
                outcome.Details!
            );
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_EnabledItemPastExpiry_IsRefused()
        {
            Succeeded(await EditAsync(13, new TrustIndicatorEditableValues
            {
                Enabled = true,
                CtaType = "None",
                ExpiresOn = new DateOnly(2026, 10, 27),
                AltText = string.Empty,
                Tooltip = string.Empty,
            }));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-expired");

            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains(
                "Item 13: evidence expired on 2026-10-27. Disable it or renew the evidence.",
                outcome.Details!
            );
        }

        [Fact]
        public async Task Publish_KeepsPriorVersionUntouched_WhenNewVersionIsCreated()
        {
            await PublishValidAsync();
            Succeeded(await EditAsync(1, Plain()));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var second = Succeeded(await PublishAsync(PublisherId, "key-v2"));

            Assert.Equal(2, second.Version);
            var versions = await _context.TrustIndicatorPublications
                .OrderBy(p => p.Version)
                .ToListAsync();
            Assert.Equal(2, versions.Count);
            var v1Items = JsonSerializer.Deserialize<List<TrustIndicatorItemDto>>(
                versions[0].SnapshotJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            )!;
            Assert.True(v1Items[0].Values.Enabled);
            Assert.False(second.Items[0].Values.Enabled);
        }

        [Fact]
        public async Task Public_BeforeAnyPublish_IsEmpty()
        {
            var items = await _sut.GetPublicAsync();

            Assert.Empty(items);
        }

        [Fact]
        public async Task Public_ShowsEnabledLiveItemsInOrder_AndDropsThemWhenTheyExpire()
        {
            Succeeded(await EditAsync(2, Plain(enabled: true)));
            Succeeded(await EditAsync(13, new TrustIndicatorEditableValues
            {
                Enabled = true,
                CtaType = "None",
                ExpiresOn = new DateOnly(2026, 10, 28),
                AltText = string.Empty,
                Tooltip = string.Empty,
            }));
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));
            Succeeded(await PublishAsync(PublisherId, "key-public"));

            var onExpiryDay = await _sut.GetPublicAsync();
            _clock.Advance(TimeSpan.FromDays(1));
            var dayAfterExpiry = await _sut.GetPublicAsync();

            Assert.Equal(new[] { 1, 2, 13 }, onExpiryDay.Select(i => i.ItemNumber).ToArray());
            Assert.Equal(new[] { 1, 2 }, dayAfterExpiry.Select(i => i.ItemNumber).ToArray());
        }

        [Fact]
        public async Task Public_IgnoresDraftEdits_UntilTheNextPublish()
        {
            await PublishValidAsync();
            Succeeded(await EditAsync(1, Plain()));

            var before = await _sut.GetPublicAsync();

            Assert.Single(before);
            Assert.Equal(1, before[0].ItemNumber);
        }

        [Fact]
        public async Task Publish_WhenAnItemIsMissing_IsRefused()
        {
            var rows = await _context.TrustIndicators.Where(r => r.ItemNumber == 15).ToListAsync();
            _context.TrustIndicators.RemoveRange(rows);
            await _context.SaveChangesAsync();
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-incomplete");

            Assert.Equal("draft_incomplete", outcome.Code);
        }

        private static TrustIndicatorEditableValues Plain(bool enabled = false) =>
            new()
            {
                Enabled = enabled,
                CtaType = "None",
                AltText = string.Empty,
                Tooltip = string.Empty,
            };

        private static TrustIndicatorEditableValues ProofLink(string url) =>
            new()
            {
                Enabled = true,
                CtaType = "ExternalProof",
                CtaTarget = url,
                EvidenceUrl = url,
                AltText = string.Empty,
                Tooltip = string.Empty,
            };

        private static T Succeeded<T>(TrustIndicatorOutcome<T> outcome)
        {
            Assert.True(
                outcome.Succeeded,
                $"{outcome.Code}: {outcome.Message} {string.Join("; ", outcome.Details ?? [])}"
            );
            return outcome.Value!;
        }

        private Task<TrustIndicatorOutcome<TrustIndicatorItemDto>> EditAsync(
            int itemNumber,
            TrustIndicatorEditableValues values,
            int actorId = EditorId
        ) => _sut.UpdateDraftItemAsync(actorId, itemNumber, values);

        private Task<TrustIndicatorOutcome<TrustIndicatorPublicationDto>> PublishAsync(
            int actorId,
            string key
        ) => _sut.PublishAsync(actorId, key, "Evidence checked");

        private async Task PublishValidAsync()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));
            Succeeded(await PublishAsync(PublisherId, $"key-{Guid.NewGuid():N}"));
        }

        private async Task<List<AdminAuditEvent>> AuditAsync(string action) =>
            await _context.AdminAuditEvents.Where(e => e.Action == action).ToListAsync();
    }
}
