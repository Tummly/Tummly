using System.Text.Json;
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

        private static readonly JsonSerializerOptions SnapshotOptions = new(
            JsonSerializerDefaults.Web
        );

        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;
        private readonly TimeProvider _clock;

        public TrustIndicatorService(
            ApplicationDbContext context,
            IAdminAuditService audit,
            TimeProvider clock
        )
        {
            _context = context;
            _audit = audit;
            _clock = clock;
        }

        public async Task<AdminTrustIndicatorsViewDto> GetAdminViewAsync(
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

            return new AdminTrustIndicatorsViewDto(
                rows.Select(ToItemDto).ToList(),
                ToReviewDto(state),
                publication == null ? null : ToPublicationDto(publication)
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
            var normalized = TrustIndicatorRules.Normalize(request, errors);
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

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorDraftEdit,
                    ActorIdentity: $"admin:{actorAdminUserId}",
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
            var now = _clock.GetUtcNow().UtcDateTime;
            state.SubmittedByAdminUserId = actorAdminUserId;
            state.SubmittedAtUtc = now;

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorSubmit,
                    ActorIdentity: $"admin:{actorAdminUserId}",
                    TargetType: AdminAuditTargetTypes.TrustIndicatorDraft,
                    TargetId: "draft",
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new { lastEditedByAdminUserId = state.LastEditedByAdminUserId }
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
                return new(ToPublicationDto(replay));
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

            // Four-eyes (ADM-15-03): the publisher must not be the submitter or the last draft editor.
            if (
                actorAdminUserId == submittedBy
                || actorAdminUserId == state.LastEditedByAdminUserId
            )
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

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.TrustIndicatorPublish,
                    ActorIdentity: $"admin:{actorAdminUserId}",
                    TargetType: AdminAuditTargetTypes.TrustIndicatorPublication,
                    TargetId: version.ToString(),
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            version,
                            previousVersion,
                            submittedByAdminUserId = submittedBy,
                            reason = trimmedReason,
                        }
                    )
                )
            );

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
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

            var today = TrustIndicatorRules.LondonToday(_clock.GetUtcNow().UtcDateTime);
            var items = JsonSerializer.Deserialize<List<TrustIndicatorItemDto>>(
                snapshot,
                SnapshotOptions
            ) ?? [];

            return items
                .Where(item => TrustIndicatorRules.IsLive(
                    item.Values.Enabled,
                    item.Values.ExpiresOn,
                    today
                ))
                .OrderBy(item => item.ItemNumber)
                .Select(ToPublicDto)
                .ToList();
        }

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
                    ActorIdentity: $"admin:{actorAdminUserId}",
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

        private static TrustIndicatorReviewDto ToReviewDto(TrustIndicatorDraftState? state) =>
            new(
                state?.LastEditedByAdminUserId,
                state?.LastEditedAtUtc,
                state?.SubmittedByAdminUserId,
                state?.SubmittedAtUtc
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

        private static PublicTrustIndicatorDto ToPublicDto(TrustIndicatorItemDto item) =>
            new(
                item.ItemNumber,
                item.Title,
                item.EvidenceClass,
                item.Values.IconKey,
                item.Values.ImageUrl,
                item.Values.AltText ?? string.Empty,
                item.Values.Tooltip ?? string.Empty,
                item.Values.CtaType ?? nameof(TrustCtaType.None),
                item.Values.CtaTarget,
                item.Values.EvidenceUrl,
                item.Values.Issuer,
                item.Values.IssuedOn,
                item.Values.ExpiresOn,
                item.Values.Scope
            );
    }
}
