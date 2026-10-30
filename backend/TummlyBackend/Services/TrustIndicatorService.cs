using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.TrustIndicators;

namespace TummlyBackend.Services
{
    public sealed class TrustIndicatorService : ITrustIndicatorService
    {
        private const int MaxIdempotencyKeyLength = 128;
        private const string PublicStatusVerified = "Verified";
        private const string PublicStatusRoadmap = "Roadmap";
        private const string PublicStatusUnderReview = "UnderReview";
        private const string PublicStatusUnderReviewLabel = "Under review";

        private static readonly JsonSerializerOptions SnapshotOptions = new(
            JsonSerializerDefaults.Web
        );

        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;
        private readonly TimeProvider _clock;
        private readonly ITrustEvidenceLinkChecker _evidenceLinks;

        public TrustIndicatorService(
            ApplicationDbContext context,
            IAdminAuditService audit,
            TimeProvider clock,
            ITrustEvidenceLinkChecker evidenceLinks
        )
        {
            _context = context;
            _audit = audit;
            _clock = clock;
            _evidenceLinks = evidenceLinks;
        }

        public async Task<AdminTrustIndicatorsViewDto> GetAdminViewAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.TrustIndicators
                .AsNoTracking()
                .OrderBy(row => row.ItemNumber)
                .ToListAsync(cancellationToken);
            var state = await _context.TrustIndicatorDraftStates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == TrustIndicatorRules.DraftStateId,
                    cancellationToken
                );
            var publication = await _context.TrustIndicatorPublications
                .AsNoTracking()
                .OrderByDescending(row => row.Version)
                .FirstOrDefaultAsync(cancellationToken);

            var canApprove = await IsApproverAsync(actorAdminUserId, cancellationToken);

            return new AdminTrustIndicatorsViewDto(
                rows.Select(ToItemDto).ToList(),
                ToReviewDto(state),
                publication == null ? null : ToPublicationDto(publication),
                new TrustIndicatorViewerDto(actorAdminUserId, canApprove)
            );
        }

        public async Task<TrustIndicatorOutcome<TrustIndicatorItemDto>> UpdateDraftItemAsync(
            int actorAdminUserId,
            int itemNumber,
            TrustIndicatorEditableValues request,
            CancellationToken cancellationToken = default
        )
        {
            var row = await _context.TrustIndicators.FirstOrDefaultAsync(
                r => r.ItemNumber == itemNumber,
                cancellationToken
            );
            if (row == null)
            {
                return TrustIndicatorOutcome<TrustIndicatorItemDto>.Fail(
                    "not_found",
                    "Trust indicator not found."
                );
            }

            var errors = new List<string>();
            var normalized = TrustIndicatorRules.Normalize(row.EvidenceClass, request, errors);
            if (errors.Count > 0)
            {
                return await RefuseAsync<TrustIndicatorItemDto>(
                    actorAdminUserId,
                    "draft_edit",
                    "validation_failed",
                    "The draft item is not valid.",
                    errors,
                    AdminAuditTargetTypes.TrustIndicatorItem,
                    itemNumber.ToString(),
                    cancellationToken
                );
            }

            var before = TrustIndicatorRules.ToValues(row);
            if (normalized == before)
            {
                return new(ToItemDto(row));
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            TrustIndicatorRules.Apply(row, normalized);
            row.LastEditedByAdminUserId = actorAdminUserId;
            row.LastEditedAtUtc = now;
            row.UpdatedAtUtc = now;

            var state = await LoadOrCreateStateAsync(cancellationToken);
            var submissionCleared = state.SubmittedByAdminUserId != null;
            state.LastEditedByAdminUserId = actorAdminUserId;
            state.LastEditedAtUtc = now;
            state.SubmittedByAdminUserId = null;
            state.SubmittedAtUtc = null;
            state.SubmittedRevision = null;

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorDraftEdit,
                    ActorIdentity: await ActorIdentityAsync(actorAdminUserId, cancellationToken),
                    TargetType: AdminAuditTargetTypes.TrustIndicatorItem,
                    TargetId: itemNumber.ToString(),
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            itemNumber,
                            before,
                            after = normalized,
                            submissionCleared,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return new(ToItemDto(row));
        }

        public async Task<TrustIndicatorOutcome<TrustIndicatorReviewDto>> SubmitDraftAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        )
        {
            var state = await LoadOrCreateStateAsync(cancellationToken);
            if (
                state.SubmittedByAdminUserId is { } existingSubmitter
                && existingSubmitter != actorAdminUserId
            )
            {
                // Overwriting the submitter would let the first submitter publish
                // their own submission (four-eyes). An edit clears the submission.
                return TrustIndicatorOutcome<TrustIndicatorReviewDto>.Fail(
                    "already_submitted",
                    "Another admin has already submitted this draft. Publish it or edit it first."
                );
            }

            var rows = await _context.TrustIndicators
                .OrderBy(row => row.ItemNumber)
                .ToListAsync(cancellationToken);
            var revision = DraftRevisionOf(rows);
            var now = _clock.GetUtcNow().UtcDateTime;
            state.SubmittedByAdminUserId = actorAdminUserId;
            state.SubmittedAtUtc = now;
            state.SubmittedRevision = revision;

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorSubmit,
                    ActorIdentity: await ActorIdentityAsync(actorAdminUserId, cancellationToken),
                    TargetType: AdminAuditTargetTypes.TrustIndicatorDraft,
                    TargetId: "draft",
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            lastEditedByAdminUserId = state.LastEditedByAdminUserId,
                            draftRevision = revision,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return new(ToReviewDto(state));
        }

        /// <summary>
        /// Approve and publish in one save: the new version, the review cleared and the audit row
        /// commit together, or nothing does. Refusals change nothing except an audit row.
        /// </summary>
        public async Task<TrustIndicatorOutcome<TrustIndicatorPublicationDto>> PublishAsync(
            int actorAdminUserId,
            string? idempotencyKey,
            string? reason,
            string? draftRevision,
            CancellationToken cancellationToken = default
        )
        {
            var key = idempotencyKey?.Trim() ?? string.Empty;
            if (key.Length == 0)
            {
                return TrustIndicatorOutcome<TrustIndicatorPublicationDto>.Fail(
                    "idempotency_key_required",
                    "Idempotency-Key header is required."
                );
            }

            if (key.Length > MaxIdempotencyKeyLength)
            {
                return TrustIndicatorOutcome<TrustIndicatorPublicationDto>.Fail(
                    "idempotency_key_invalid",
                    "Idempotency-Key is too long."
                );
            }

            // A repeated key returns the version it already created and creates nothing new.
            var replay = await _context.TrustIndicatorPublications
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.IdempotencyKey == key, cancellationToken);
            if (replay != null)
            {
                // A key belongs to the admin who made the publication. Another admin's replay is refused.
                if (replay.PublishedByAdminUserId != actorAdminUserId)
                {
                    return await RefuseAsync<TrustIndicatorPublicationDto>(
                        actorAdminUserId,
                        "publish",
                        "idempotency_key_reused",
                        "This Idempotency-Key was used for a publish by another admin.",
                        null,
                        AdminAuditTargetTypes.TrustIndicatorPublication,
                        replay.Version.ToString(),
                        cancellationToken
                    );
                }

                return new(ToPublicationDto(replay));
            }

            if (!await IsApproverAsync(actorAdminUserId, cancellationToken))
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "approver_required",
                    "Only a trust indicator approver can publish.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var trimmedReason = reason?.Trim() ?? string.Empty;
            if (
                trimmedReason.Length == 0
                || trimmedReason.Length > TrustIndicatorRules.MaxReasonLength
            )
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "validation_failed",
                    "A publish reason is required (500 characters or fewer).",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var rows = await _context.TrustIndicators
                .OrderBy(row => row.ItemNumber)
                .ToListAsync(cancellationToken);
            var complete = rows.Count == TrustIndicatorRules.ItemCount
                && rows.Select(row => row.ItemNumber)
                    .SequenceEqual(Enumerable.Range(1, TrustIndicatorRules.ItemCount));
            if (!complete)
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "draft_incomplete",
                    "The draft must contain all 15 trust indicators.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var state = await LoadOrCreateStateAsync(cancellationToken);
            if (state.SubmittedByAdminUserId is not int submittedBy)
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "not_submitted",
                    "Submit the draft for review before publishing.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            // Four-eyes (ADM-15-03, CONTRACT §6): the publisher must not be the submitter, and must not have
            // edited any item since the latest publication (all time when nothing is published yet).
            var latestPublishedAtUtc = await _context.TrustIndicatorPublications
                .AsNoTracking()
                .OrderByDescending(row => row.Version)
                .Select(row => (DateTime?)row.PublishedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var editedSinceLastPublish = await _context.AdminAuditEvents
                .AsNoTracking()
                .AnyAsync(
                    row => row.Action == AdminAuditActions.TrustIndicatorDraftEdit
                        && row.ActorAdminUserId == actorAdminUserId
                        && row.Succeeded
                        && (latestPublishedAtUtc == null || row.OccurredAtUtc > latestPublishedAtUtc),
                    cancellationToken
                );
            if (actorAdminUserId == submittedBy || editedSinceLastPublish)
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "self_approval_denied",
                    "A second admin must publish a draft you submitted or edited.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            // The reviewer must have seen the submitted rows: the body revision, the stored revision and the
            // rows read now must all agree (CONTRACT §6). Any other case is a stale view or an out-of-band change.
            var currentRevision = DraftRevisionOf(rows);
            var storedRevision = state.SubmittedRevision;
            if (
                string.IsNullOrWhiteSpace(draftRevision)
                || storedRevision == null
                || !string.Equals(draftRevision.Trim(), storedRevision, StringComparison.Ordinal)
                || !string.Equals(storedRevision, currentRevision, StringComparison.Ordinal)
            )
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "draft_changed",
                    "The draft changed after it was submitted. Reload it and review it again.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var today = TrustIndicatorRules.LondonToday(_clock.GetUtcNow().UtcDateTime);
            var gateErrors = TrustIndicatorRules.PublishGateErrors(rows, today);
            if (gateErrors.Count > 0)
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "publish_gate_failed",
                    "The draft has items that cannot be published.",
                    gateErrors,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            // CONTRACT §5 item 6: only after the rule gate passes, and before the no-change check.
            var linkErrors = await EvidenceLinkErrorsAsync(rows, cancellationToken);
            if (linkErrors.Count > 0)
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "publish_gate_failed",
                    "The draft has items that cannot be published.",
                    linkErrors,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var latestSnapshot = await _context.TrustIndicatorPublications
                .AsNoTracking()
                .OrderByDescending(row => row.Version)
                .Select(row => row.SnapshotJson)
                .FirstOrDefaultAsync(cancellationToken);
            if (latestSnapshot != null && SamePublishedContent(latestSnapshot, rows))
            {
                return await RefuseAsync<TrustIndicatorPublicationDto>(
                    actorAdminUserId,
                    "publish",
                    "no_changes",
                    "The draft has no changes since the last published version.",
                    null,
                    AdminAuditTargetTypes.TrustIndicatorDraft,
                    "draft",
                    cancellationToken
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var previousVersion = await _context.TrustIndicatorPublications
                .MaxAsync(row => (int?)row.Version, cancellationToken) ?? 0;
            var version = previousVersion + 1;

            var publication = new TrustIndicatorPublication
            {
                Version = version,
                PublishedAtUtc = now,
                PublishedByAdminUserId = actorAdminUserId,
                ApprovedByAdminUserId = actorAdminUserId,
                SubmittedByAdminUserId = submittedBy,
                SubmittedAtUtc = state.SubmittedAtUtc,
                Reason = trimmedReason,
                IdempotencyKey = key,
                SnapshotJson = JsonSerializer.Serialize(
                    rows.Select(ToItemDto).ToList(),
                    SnapshotOptions
                ),
            };
            _context.TrustIndicatorPublications.Add(publication);

            state.SubmittedByAdminUserId = null;
            state.SubmittedAtUtc = null;
            state.SubmittedRevision = null;

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorPublish,
                    ActorIdentity: await ActorIdentityAsync(actorAdminUserId, cancellationToken),
                    TargetType: AdminAuditTargetTypes.TrustIndicatorPublication,
                    TargetId: version.ToString(),
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            version,
                            previousVersion,
                            submittedByAdminUserId = submittedBy,
                            draftRevision = currentRevision,
                            reason = trimmedReason,
                        }
                    )
                )
            );

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException { Number: 2601 or 2627 }
            )
            {
                // Another publish took the version or key first. Nothing from this attempt was saved.
                _context.ChangeTracker.Clear();
                return TrustIndicatorOutcome<TrustIndicatorPublicationDto>.Fail(
                    "publish_conflict",
                    "The draft could not be published. Reload and try again."
                );
            }

            return new(ToPublicationDto(publication));
        }

        /// <summary>
        /// Public read (CONTRACT §8). Reads the latest snapshot only and applies §2 and §3 to each item.
        /// A bad item is omitted; a bad CTA is dropped. An unreadable snapshot publishes nothing.
        /// </summary>
        public async Task<IReadOnlyList<PublicTrustIndicatorDto>> GetPublicAsync(
            CancellationToken cancellationToken = default
        )
        {
            var snapshot = await _context.TrustIndicatorPublications
                .AsNoTracking()
                .OrderByDescending(row => row.Version)
                .Select(row => row.SnapshotJson)
                .FirstOrDefaultAsync(cancellationToken);
            if (snapshot == null)
            {
                return [];
            }

            List<TrustIndicatorItemDto>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<TrustIndicatorItemDto>>(
                    snapshot,
                    SnapshotOptions
                );
            }
            catch (JsonException)
            {
                return [];
            }

            var today = TrustIndicatorRules.LondonToday(_clock.GetUtcNow().UtcDateTime);
            return (items ?? [])
                .Where(item => item?.Values != null
                    && TrustIndicatorRules.IsLive(item.Values.Enabled, item.Values.ExpiresOn, today))
                .OrderBy(item => item.ItemNumber)
                .Select(ToPublicDto)
                .OfType<PublicTrustIndicatorDto>()
                .ToList();
        }

        /// <summary>
        /// Checks the evidence link of each enabled item in scope, in item order. Returns one line per failed
        /// item. An unparseable link cannot be fetched, so it fails like a network error.
        /// </summary>
        private async Task<List<string>> EvidenceLinkErrorsAsync(
            IEnumerable<TrustIndicator> rows,
            CancellationToken cancellationToken
        )
        {
            var errors = new List<string>();
            foreach (var row in rows.OrderBy(item => item.ItemNumber))
            {
                if (!row.Enabled || string.IsNullOrWhiteSpace(row.EvidenceUrl) || !InLinkCheckScope(row))
                {
                    continue;
                }

                var label = $"Item {row.ItemNumber:D2}";
                if (!Uri.TryCreate(row.EvidenceUrl.Trim(), UriKind.Absolute, out var uri))
                {
                    errors.Add($"{label}: evidence link failed (network error).");
                    continue;
                }

                var result = await _evidenceLinks.CheckAsync(uri, cancellationToken);
                if (!result.Ok)
                {
                    errors.Add($"{label}: evidence link failed ({result.Detail}).");
                }
            }

            return errors;
        }

        /// <summary>
        /// CONTRACT §5.6: the link is checked for an item public as Verified, and for every self-assessment.
        /// A Roadmap, QaPending or UnderReview item is not checked.
        /// </summary>
        private static bool InLinkCheckScope(TrustIndicator row) =>
            TrustIndicatorRules.PublicStatus(row.EvidenceClass, row.Status.ToString()) == PublicStatusVerified
            || row.EvidenceClass == TrustEvidenceClass.VerifiedSelfAssessment;

        private async Task<TrustIndicatorOutcome<T>> RefuseAsync<T>(
            int actorAdminUserId,
            string operation,
            string code,
            string message,
            IReadOnlyList<string>? details,
            string targetType,
            string targetId,
            CancellationToken cancellationToken
        )
        {
            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorRefused,
                    ActorIdentity: await ActorIdentityAsync(actorAdminUserId, cancellationToken),
                    TargetType: targetType,
                    TargetId: targetId,
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new { operation, code, details = details ?? Array.Empty<string>() }
                    )
                )
            );
            await _context.SaveChangesAsync(cancellationToken);
            return TrustIndicatorOutcome<T>.Fail(code, message, details);
        }

        private async Task<TrustIndicatorDraftState> LoadOrCreateStateAsync(
            CancellationToken cancellationToken
        )
        {
            var state = await _context.TrustIndicatorDraftStates.FirstOrDefaultAsync(
                row => row.Id == TrustIndicatorRules.DraftStateId,
                cancellationToken
            );
            if (state == null)
            {
                state = new TrustIndicatorDraftState { Id = TrustIndicatorRules.DraftStateId };
                _context.TrustIndicatorDraftStates.Add(state);
            }

            return state;
        }

        private static TrustIndicatorItemDto ToItemDto(TrustIndicator row) =>
            new(
                row.ItemNumber,
                row.Title,
                row.EvidenceClass.ToString(),
                TrustIndicatorRules.ToValues(row),
                row.LastEditedByAdminUserId,
                row.LastEditedAtUtc
            );

        /// <summary>True when the draft content equals the latest published content (edit metadata ignored).</summary>
        private static bool SamePublishedContent(
            string latestSnapshotJson,
            IEnumerable<TrustIndicator> rows
        )
        {
            var published = JsonSerializer.Deserialize<List<TrustIndicatorItemDto>>(
                latestSnapshotJson,
                SnapshotOptions
            ) ?? [];
            string Key(int itemNumber, TrustIndicatorEditableValues values) =>
                $"{itemNumber}:{JsonSerializer.Serialize(values, SnapshotOptions)}";

            return published
                .OrderBy(item => item.ItemNumber)
                .Select(item => Key(item.ItemNumber, item.Values))
                .SequenceEqual(
                    rows.OrderBy(row => row.ItemNumber)
                        .Select(row => Key(row.ItemNumber, TrustIndicatorRules.ToValues(row)))
                );
        }

        /// <summary>
        /// CONTRACT §6 draft revision: lowercase hex SHA-256 of the editable values of the rows, in item order,
        /// serialized with the snapshot options.
        /// </summary>
        private static string DraftRevisionOf(IEnumerable<TrustIndicator> rows)
        {
            var canonical = JsonSerializer.Serialize(
                rows.OrderBy(row => row.ItemNumber).Select(TrustIndicatorRules.ToValues).ToList(),
                SnapshotOptions
            );
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
                .ToLowerInvariant();
        }

        /// <summary>
        /// True only for a usable Admin-role row (Role Admin, active, not locked) whose approver flag is set
        /// and whose latest trust.approver_grant / trust.approver_revoke audit row is a grant (CONTRACT §6).
        /// A flag with no grant row behind it, or with a later revoke, is not an approver. An unknown id is not
        /// an approver. The usability rule is the same as AdminStaffService.
        /// </summary>
        private async Task<bool> IsApproverAsync(
            int adminUserId,
            CancellationToken cancellationToken
        )
        {
            var usable = await _context.Admins
                .AsNoTracking()
                .AnyAsync(
                    row => row.Id == adminUserId
                        && row.Role == "Admin"
                        && row.IsActive
                        && !row.IsLocked
                        && row.CanApproveTrustIndicators,
                    cancellationToken
                );
            if (!usable)
            {
                return false;
            }

            var targetId = adminUserId.ToString();
            var latestAction = await _context.AdminAuditEvents
                .AsNoTracking()
                .Where(row => row.TargetId == targetId
                    && (row.Action == AdminAuditActions.TrustApproverGrant
                        || row.Action == AdminAuditActions.TrustApproverRevoke))
                .OrderByDescending(row => row.OccurredAtUtc)
                .Select(row => row.Action)
                .FirstOrDefaultAsync(cancellationToken);
            return latestAction == AdminAuditActions.TrustApproverGrant;
        }

        private async Task<string> ActorIdentityAsync(
            int adminUserId,
            CancellationToken cancellationToken
        )
        {
            // Same actor text as other admin audit rows: the staff email when known.
            var email = await _context.Admins
                .AsNoTracking()
                .Where(row => row.Id == adminUserId)
                .Select(row => row.Email)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(email) ? $"admin:{adminUserId}" : email;
        }

        private static TrustIndicatorReviewDto ToReviewDto(TrustIndicatorDraftState? state) =>
            new(
                state?.LastEditedByAdminUserId,
                state?.LastEditedAtUtc,
                state?.SubmittedByAdminUserId,
                state?.SubmittedAtUtc,
                // A revision means something only while a submission stands.
                state?.SubmittedByAdminUserId == null ? null : state.SubmittedRevision
            );

        private static TrustIndicatorPublicationDto ToPublicationDto(
            TrustIndicatorPublication publication
        )
        {
            var items = JsonSerializer.Deserialize<List<TrustIndicatorItemDto>>(
                publication.SnapshotJson,
                SnapshotOptions
            ) ?? [];
            return new TrustIndicatorPublicationDto(
                publication.Version,
                publication.PublishedAtUtc,
                publication.PublishedByAdminUserId,
                publication.ApprovedByAdminUserId,
                publication.SubmittedByAdminUserId,
                publication.SubmittedAtUtc,
                publication.Reason,
                items
            );
        }

        /// <summary>
        /// CONTRACT §2 and §3 for one live item. Null when the item must be omitted: an unknown class,
        /// a status that is missing or not allowed for the class, or a blank status label.
        /// </summary>
        private static PublicTrustIndicatorDto? ToPublicDto(TrustIndicatorItemDto item)
        {
            var values = item.Values;
            if (
                string.IsNullOrWhiteSpace(item.Title)
                || !TrustIndicatorRules.TryParseEvidenceClass(item.EvidenceClass, out var evidenceClass)
            )
            {
                return null;
            }

            var status = TrustIndicatorRules.PublicStatus(evidenceClass, values.Status);
            if (status == null)
            {
                return null;
            }

            var statusLabel = status == PublicStatusUnderReview
                ? PublicStatusUnderReviewLabel
                : values.StatusLabel?.Trim();
            if (string.IsNullOrEmpty(statusLabel))
            {
                return null;
            }

            var cta = status == PublicStatusVerified
                && TrustIndicatorRules.PublicCtaAllowed(evidenceClass, values)
                    ? ToPublicCta(values)
                    : null;

            return new PublicTrustIndicatorDto(
                item.ItemNumber,
                item.Title,
                status,
                statusLabel,
                values.IconKey,
                status == PublicStatusRoadmap ? null : values.ImageUrl,
                values.AltText ?? string.Empty,
                values.Tooltip ?? string.Empty,
                cta
            );
        }

        /// <summary>Only called after PublicCtaAllowed, so the type and label are present and exact.</summary>
        private static PublicTrustCtaDto ToPublicCta(TrustIndicatorEditableValues values)
        {
            var type = values.CtaType!.Trim();
            return new PublicTrustCtaDto(
                type,
                values.CtaLabel!.Trim(),
                type == nameof(TrustCtaType.CookieSettings) ? null : values.CtaTarget
            );
        }
    }
}
