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
        private const int NonApproverId = 44;
        private const int InactiveApproverId = 55;
        private const int LockedApproverId = 66;

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

        private static readonly string[] CanonicalStatuses =
        [
            "VerifiedPublicRecord",
            "VerifiedCredential",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "Roadmap",
            "QaPending",
            "QaPending",
            "QaPending",
            "QaPending",
            "VerifiedPublicRecord",
            "QaPending",
            "QaPending",
        ];

        private static readonly string[] CanonicalStatusLabels =
        [
            "Registered",
            "Self-assessment completed",
            "On our roadmap",
            "On our roadmap",
            "On our roadmap",
            "On our roadmap",
            "Application planned",
            "On our roadmap",
            "Under review",
            "Under review",
            "Under review",
            "Under review",
            "Verified public record",
            "Under review",
            "Under review",
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
        private readonly FakeTrustEvidenceLinkChecker _evidenceLinks = new();
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
                _clock,
                _evidenceLinks
            );
            TrustIndicatorCatalogSeed.EnsureSeededAsync(_context).GetAwaiter().GetResult();
            _context.Admins.AddRange(
                NewAdmin(EditorId, canApprove: true),
                NewAdmin(PublisherId, canApprove: true),
                NewAdmin(ThirdAdminId, canApprove: true),
                NewAdmin(NonApproverId, canApprove: false),
                NewAdmin(InactiveApproverId, canApprove: true, isActive: false),
                NewAdmin(LockedApproverId, canApprove: true, isLocked: true)
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task Seed_Creates15InOrderAllOff()
        {
            var view = await _sut.GetAdminViewAsync(EditorId);

            Assert.Equal(Enumerable.Range(1, 15).ToList(), view.Draft.Select(i => i.ItemNumber).ToList());
            Assert.Equal(CanonicalTitles.ToList(), view.Draft.Select(i => i.Title).ToList());
            Assert.Equal(CanonicalClasses.ToList(), view.Draft.Select(i => i.EvidenceClass).ToList());
            Assert.Equal(CanonicalStatuses.ToList(), view.Draft.Select(i => i.Values.Status).ToList());
            Assert.Equal(CanonicalStatusLabels.ToList(), view.Draft.Select(i => i.Values.StatusLabel).ToList());
            Assert.All(view.Draft, item =>
            {
                Assert.False(item.Values.Enabled);
                Assert.Equal("None", item.Values.CtaType);
                Assert.Null(item.Values.CtaTarget);
            });
            Assert.Null(view.Published);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Seed_BackfillsOnlyBlankRows()
        {
            // A row nobody edited: blank copy, defaulted status, and switched on by an earlier admin.
            var blank = await _context.TrustIndicators.SingleAsync(r => r.ItemNumber == 3);
            blank.Status = TrustIndicatorStatus.QaPending;
            blank.StatusLabel = string.Empty;
            blank.Tooltip = string.Empty;
            blank.IconKey = null;
            blank.AltText = string.Empty;
            blank.Enabled = true;
            blank.CtaType = TrustCtaType.InternalPage;
            // A row an admin wrote by hand: its copy must survive the seed.
            var edited = await _context.TrustIndicators.SingleAsync(r => r.ItemNumber == 4);
            edited.Status = TrustIndicatorStatus.QaPending;
            edited.StatusLabel = string.Empty;
            edited.Tooltip = "Our own note";
            await _context.SaveChangesAsync();

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);

            var view = await _sut.GetAdminViewAsync(EditorId);
            var backfilled = view.Draft[2];
            Assert.Equal("Roadmap", backfilled.Values.Status);
            Assert.Equal("On our roadmap", backfilled.Values.StatusLabel);
            Assert.Equal("roadmap-cyber-essentials", backfilled.Values.IconKey);
            Assert.Equal("Cyber Essentials", backfilled.Values.AltText);
            Assert.True(backfilled.Values.Enabled);
            Assert.Equal("InternalPage", backfilled.Values.CtaType);
            var kept = view.Draft[3];
            Assert.Equal("QaPending", kept.Values.Status);
            Assert.Equal(string.Empty, kept.Values.StatusLabel);
            Assert.Equal("Our own note", kept.Values.Tooltip);
        }

        [Fact]
        public async Task Seed_DoesNotBackfillAdminEditedBlankRow()
        {
            // Blank copy, but an admin edited it (for example to set an evidence link): the seed leaves it alone.
            var row = await _context.TrustIndicators.SingleAsync(r => r.ItemNumber == 5);
            row.Status = TrustIndicatorStatus.QaPending;
            row.StatusLabel = string.Empty;
            row.Tooltip = string.Empty;
            row.EvidenceUrl = "https://custom.example/iso-27001";
            row.LastEditedByAdminUserId = EditorId;
            await _context.SaveChangesAsync();
            var before = TrustIndicatorRules.ToValues(row);

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);

            Assert.Equal(before, TrustIndicatorRules.ToValues(row));
            Assert.Equal(EditorId, row.LastEditedByAdminUserId);
            Assert.Equal(TrustIndicatorStatus.QaPending, row.Status);
            Assert.Equal("https://custom.example/iso-27001", row.EvidenceUrl);
        }

        [Fact]
        public async Task Seed_IsIdempotent()
        {
            var before = (await _sut.GetAdminViewAsync(EditorId)).Draft;

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);
            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);

            Assert.Equal(15, await _context.TrustIndicators.CountAsync());
            Assert.Equal(1, await _context.TrustIndicatorDraftStates.CountAsync());
            Assert.Equal(before, (await _sut.GetAdminViewAsync(EditorId)).Draft);
        }

        [Fact]
        public async Task Seed_InsertsOnlyMissingItems_AndKeepsEdits()
        {
            Succeeded(await EditAsync(3, Seeded(3) with { Tooltip = "Working towards Cyber Essentials" }));
            var row = await _context.TrustIndicators.SingleAsync(r => r.ItemNumber == 7);
            _context.TrustIndicators.Remove(row);
            await _context.SaveChangesAsync();

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(_context);

            var view = await _sut.GetAdminViewAsync(EditorId);
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
            Assert.Null((await _sut.GetAdminViewAsync(EditorId)).Published);
        }

        [Fact]
        public async Task DraftEdit_WithNoChange_WritesNoAuditRow()
        {
            var current = (await _sut.GetAdminViewAsync(EditorId)).Draft[0].Values;

            var item = Succeeded(await EditAsync(1, current));

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
            var view = await _sut.GetAdminViewAsync(EditorId);
            Assert.False(view.Draft[0].Values.Enabled);
            Assert.Empty(await AuditAsync(AdminAuditActions.TrustIndicatorDraftEdit));
            Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
        }

        [Fact]
        public async Task DraftEdit_AfterPublish_DoesNotChangePublicOutput()
        {
            await PublishValidAsync();

            Succeeded(await EditAsync(1, Seeded(1)));

            var view = await _sut.GetAdminViewAsync(EditorId);
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

            Succeeded(await EditAsync(2, Seeded(2) with { Tooltip = "Reviewed" }, PublisherId));

            var view = await _sut.GetAdminViewAsync(EditorId);
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

            var view = await _sut.GetAdminViewAsync(EditorId);
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
            Succeeded(await EditAsync(1, Seeded(1, enabled: true) with
            {
                CtaType = "ExternalProof",
                CtaTarget = "https://ico.example/proof",
                CtaLabel = "View registration",
                EvidenceUrl = null,
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
            Succeeded(await EditAsync(3, Seeded(3, enabled: true) with
            {
                CtaType = "InternalPage",
                CtaTarget = "/security",
                CtaLabel = "Read more",
            }));
            Succeeded(await EditAsync(9, Seeded(9, enabled: true) with { AltText = "Verified by our team" }));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-roadmap");

            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains(
                outcome.Details!,
                detail => detail.StartsWith("Item 03: the call to action", StringComparison.Ordinal)
            );
            Assert.Contains(
                "Item 09: QaPending items cannot use verified or certified wording.",
                outcome.Details!
            );
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_EnabledItemPastExpiry_IsRefused()
        {
            Succeeded(await EditAsync(13, Seeded(13, enabled: true) with
            {
                ExpiresOn = new DateOnly(2026, 10, 27),
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
        public async Task Publish_EvidenceLinkFailureBlocksPublish()
        {
            await PublishValidAsync();
            const string url = "https://ico.example/item13";
            _evidenceLinks.Results[url] = new TrustEvidenceLinkResult(false, "HTTP 404");
            Succeeded(await EditAsync(13, Seeded(13, enabled: true) with { EvidenceUrl = url }));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-dead-link");

            Assert.False(outcome.Succeeded);
            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains("Item 13: evidence link failed (HTTP 404).", outcome.Details!);
            Assert.Single(await _context.TrustIndicatorPublications.ToListAsync());
            var refused = Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
            Assert.Contains("Item 13: evidence link failed (HTTP 404).", refused.DetailJson);
        }

        [Fact]
        public async Task Publish_DisabledItemsAreNotChecked()
        {
            const string disabledUrl = "https://ico.example/item13-disabled";
            _evidenceLinks.Results[disabledUrl] = new TrustEvidenceLinkResult(false, "HTTP 404");
            Succeeded(await EditAsync(13, Seeded(13) with { EvidenceUrl = disabledUrl }));
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var published = Succeeded(await PublishAsync(PublisherId, "key-disabled-link"));

            Assert.Equal(1, published.Version);
            Assert.DoesNotContain(disabledUrl, _evidenceLinks.CheckedUrls);
            Assert.Contains("https://ico.example/proof", _evidenceLinks.CheckedUrls);
        }

        [Fact]
        public async Task Publish_KeepsPriorVersionUntouched_WhenNewVersionIsCreated()
        {
            await PublishValidAsync();
            Succeeded(await EditAsync(1, Seeded(1)));
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
            Succeeded(await EditAsync(2, Seeded(2, enabled: true) with
            {
                EvidenceUrl = "https://ico.example/completion",
            }));
            Succeeded(await EditAsync(13, Seeded(13, enabled: true) with
            {
                ExpiresOn = new DateOnly(2026, 10, 28),
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
            Succeeded(await EditAsync(1, Seeded(1)));

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

        [Fact]
        public async Task Publish_WithNoChangesSinceLastVersion_IsRefused()
        {
            await PublishValidAsync();
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-no-change");

            Assert.Equal("no_changes", outcome.Code);
            Assert.Single(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_SelfAssessmentWithCertifiedWording_IsRefused()
        {
            Succeeded(
                await EditAsync(
                    2,
                    Seeded(2, enabled: true) with
                    {
                        EvidenceUrl = "https://ico.example/completion",
                        Scope = "Certified organisational self-assessment",
                    }
                )
            );
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(PublisherId, "key-certified");

            Assert.Equal("publish_gate_failed", outcome.Code);
            Assert.Contains(outcome.Details!, detail => detail.StartsWith("Item 02"));
        }

        [Fact]
        public async Task Publish_NonApproverRefusedAndAudited()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(NonApproverId, "key-non-approver");

            Assert.False(outcome.Succeeded);
            Assert.Equal("approver_required", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
            var refused = Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
            Assert.Equal(NonApproverId, refused.ActorAdminUserId);
            Assert.Contains("approver_required", refused.DetailJson);
        }

        [Fact]
        public async Task Publish_InactiveApproverRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(InactiveApproverId, "key-inactive");

            Assert.Equal("approver_required", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_LockedApproverRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(LockedApproverId, "key-locked-approver");

            Assert.Equal("approver_required", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
            var refused = Assert.Single(await AuditAsync(AdminAuditActions.TrustIndicatorRefused));
            Assert.Equal(LockedApproverId, refused.ActorAdminUserId);
            Assert.Contains("approver_required", refused.DetailJson);
        }

        [Fact]
        public async Task Publish_ApproverWhoSubmittedRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var outcome = await PublishAsync(EditorId, "key-submitter-approver");

            Assert.Equal("self_approval_denied", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_ApproverWhoLastEditedRefused()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof"), EditorId));
            Succeeded(await _sut.SubmitDraftAsync(PublisherId));

            var outcome = await PublishAsync(EditorId, "key-last-editor-approver");

            Assert.Equal("self_approval_denied", outcome.Code);
            Assert.Empty(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task Publish_SeparateApproverPublishes()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));

            var published = Succeeded(await PublishAsync(PublisherId, "key-separate"));

            Assert.Equal(PublisherId, published.ApprovedByAdminUserId);
            Assert.Equal(EditorId, published.SubmittedByAdminUserId);
        }

        [Fact]
        public async Task Publish_IdempotentReplay_ReturnsExistingVersion_BeforeApproverCheck()
        {
            Succeeded(await EditAsync(1, ProofLink("https://ico.example/proof")));
            Succeeded(await _sut.SubmitDraftAsync(EditorId));
            Succeeded(await PublishAsync(PublisherId, "key-replay-rbac"));

            var replay = Succeeded(await PublishAsync(NonApproverId, "key-replay-rbac"));

            Assert.Equal(1, replay.Version);
            Assert.Single(await _context.TrustIndicatorPublications.ToListAsync());
        }

        [Fact]
        public async Task AdminView_ReportsViewer()
        {
            var approver = (await _sut.GetAdminViewAsync(PublisherId)).Viewer;
            var nonApprover = (await _sut.GetAdminViewAsync(NonApproverId)).Viewer;
            var inactive = (await _sut.GetAdminViewAsync(InactiveApproverId)).Viewer;
            var locked = (await _sut.GetAdminViewAsync(LockedApproverId)).Viewer;
            var unknown = (await _sut.GetAdminViewAsync(999)).Viewer;

            Assert.Equal(new TrustIndicatorViewerDto(PublisherId, true), approver);
            Assert.Equal(new TrustIndicatorViewerDto(NonApproverId, false), nonApprover);
            Assert.Equal(new TrustIndicatorViewerDto(InactiveApproverId, false), inactive);
            Assert.Equal(new TrustIndicatorViewerDto(LockedApproverId, false), locked);
            Assert.Equal(new TrustIndicatorViewerDto(999, false), unknown);
        }

        /// <summary>The approved seed copy as editable values, so a test edits from a valid draft.</summary>
        private static TrustIndicatorEditableValues Seeded(int itemNumber, bool enabled = false)
        {
            var seed = TrustIndicatorCatalogSeed.Items.Single(item => item.ItemNumber == itemNumber);
            return new TrustIndicatorEditableValues
            {
                Enabled = enabled,
                CtaType = "None",
                Status = seed.Status.ToString(),
                StatusLabel = seed.StatusLabel,
                IconKey = seed.IconKey,
                AltText = seed.Title,
                Tooltip = seed.Tooltip,
                Issuer = seed.Issuer,
                EvidenceUrl = seed.EvidenceUrl,
                IssuedOn = seed.IssuedOn,
                Scope = seed.Scope,
            };
        }

        private static TrustIndicatorEditableValues ProofLink(string url) =>
            Seeded(1, enabled: true) with
            {
                CtaType = "ExternalProof",
                CtaTarget = url,
                CtaLabel = "View registration",
                EvidenceUrl = url,
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

        private static Admin NewAdmin(
            int id,
            bool canApprove,
            bool isActive = true,
            bool isLocked = false
        ) =>
            new()
            {
                Id = id,
                FullName = $"Trust admin {id}",
                Email = $"admin{id}@tummly.test",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = isActive,
                IsLocked = isLocked,
                CanApproveTrustIndicators = canApprove,
                CreatedAt = DateTime.UtcNow,
            };

        private async Task<List<AdminAuditEvent>> AuditAsync(string action) =>
            await _context.AdminAuditEvents.Where(e => e.Action == action).ToListAsync();
    }
}
