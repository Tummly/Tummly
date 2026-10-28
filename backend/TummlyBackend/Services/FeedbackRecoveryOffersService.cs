using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackRecoveryOffersService : IFeedbackRecoveryOffersService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;
        private readonly IGuestContactSuppressionService _suppressions;
        private readonly IGuestResponseEmailDeliveryWork _emailDelivery;
        private readonly IOfferIssueService _offerIssues;
        private readonly IGuestReplyLock _replyLock;
        private readonly IGuestReplySmsSendStep _smsSend;

        public FeedbackRecoveryOffersService(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            IGuestResponseEmailDeliveryWork emailDelivery,
            IOfferIssueService offerIssues,
            IGuestReplyLock replyLock,
            IGuestReplySmsSendStep smsSend
        )
        {
            _context = context;
            _suppressions = suppressions;
            _emailDelivery = emailDelivery;
            _offerIssues = offerIssues;
            _replyLock = replyLock;
            _smsSend = smsSend;
        }

        public async Task<SendAndIssueFeedbackRecoveryOfferResultDto?> SendAndIssueAsync(
            int feedbackId,
            int authorUserId,
            SendAndIssueFeedbackRecoveryOfferRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (!FeedbackGuestResponseMapping.TryParseChannel(
                    request.Channel,
                    out var channel
                ))
            {
                throw new ArgumentException("Channel must be email or sms.");
            }

            if (!FeedbackGuestResponseMapping.TryParseIntent(
                    request.Intent,
                    out var intent
                )
                || intent != FeedbackRecoveryIntent.RespondWithRecoveryOffer)
            {
                throw new ArgumentException(
                    "Intent must be respond_with_recovery_offer."
                );
            }

            var purpose = (request.Purpose ?? string.Empty).Trim();
            if (purpose != "include_a_recovery_offer")
            {
                throw new ArgumentException(
                    "Purpose must be include_a_recovery_offer."
                );
            }

            var content = FeedbackGuestResponseComposer.ValidateContent(
                channel,
                request.Subject,
                request.Body
            );

            // Client one-off offer payload is ignored after cutover — catalog
            // attach on Feedback is the source of truth (ticket 05).
            _ = request.Offer;

            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == authorUserId, cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.RestaurantLocation)
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback == null)
            {
                return null;
            }

            if (feedback.WorkflowStatus == FeedbackWorkflowStatus.Resolved)
            {
                throw new FeedbackAlreadyResolvedException();
            }

            FeedbackGuestResponseComposer.EnsureChannelMatchesContact(
                feedback,
                channel
            );

            var decision = await ServiceReplyEligibility.EvaluateAsync(
                _context,
                _suppressions,
                feedback,
                channel,
                cancellationToken
            );
            if (decision != ServiceReplyDecision.Allowed)
            {
                throw new ArgumentException(
                    ServiceReplyEligibility.RefusalMessage(decision)
                );
            }

            if (feedback.RecoveryOfferId is not int catalogOfferId)
            {
                throw new ArgumentException(
                    "A Recovery catalog offer attach is required before send."
                );
            }

            if (feedback.LocationGuestId is not int locationGuestId)
            {
                throw new ArgumentException(
                    "Feedback must be linked to a Location Guest before Recovery send."
                );
            }

            // Held until the send finishes (the reply is saved and the email
            // notified under the lock). Repeat checks run under it.
            await using var replyLease = await _replyLock.TryAcquireAsync(
                _context,
                feedbackId,
                cancellationToken
            ) ?? throw new GuestReplyLockUnavailableException();

            var issuedAt = DateTime.UtcNow;

            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedbackId,
                channel,
                content.Body,
                issuedAt,
                cancellationToken
            );
            await GuestReplyRules.EnsureNoOpenRecoveryOfferAsync(
                _context,
                feedbackId,
                issuedAt,
                cancellationToken
            );

            var isSms = channel == FeedbackGuestResponseChannel.Sms;
            if (isSms)
            {
                _smsSend.EnsureBillingLive();
                await _smsSend.EnsureOperatorBillingAllowsSendAsync(
                    feedback.RestaurantLocationId,
                    cancellationToken
                );
            }

            OfferIssue? issue = null;
            FeedbackGuestResponse? guestResponse = null;
            GuestReplySmsReservation? reservation = null;

            // Stage Offer issue + guest-response, then one SaveChanges so both
            // facts commit together (ADR 0026 / ticket 05 atomic Send). SMS
            // reserves credits for the final text (claim code included) before
            // that save, and the send happens only after it.
            for (var attempt = 1; ; attempt++)
            {
                DetachIfTracked(issue);
                DetachIfTracked(guestResponse);

                issue = await _offerIssues.StageIssueOnRecoverySendAsync(
                    catalogOfferId,
                    locationGuestId,
                    feedbackId,
                    issuedAt,
                    cancellationToken
                );

                if (issue == null)
                {
                    throw new ArgumentException(
                        "Recovery catalog offer must be Active and the Location must be active."
                    );
                }

                var bodyForChannel = isSms
                    ? AppendClaimCodeText(content.Body, issue.ClaimCode)
                    : content.Body;

                if (isSms)
                {
                    // The reserve saves the shared context. Keep the staged Offer
                    // out of that save so a refused reserve leaves no issue behind.
                    DetachIfTracked(issue);
                    reservation = await _smsSend.ReserveAsync(
                        feedback,
                        string.Empty,
                        bodyForChannel,
                        cancellationToken
                    );
                    _context.OfferIssues.Add(issue);
                }

                guestResponse = FeedbackGuestResponseComposer.Build(
                    feedback,
                    channel,
                    intent,
                    new FeedbackGuestResponseComposer.ValidatedContent(
                        content.Subject,
                        bodyForChannel
                    ),
                    purpose,
                    request.Tone,
                    request.IncludeNotes,
                    authorUserId,
                    author.FullName,
                    issuedAt
                );

                guestResponse.EmailDeliveryStatus =
                    isSms
                        ? GuestResponseEmailDeliveryStatus.NotApplicable
                        : GuestResponseEmailDeliveryStatus.Pending;
                guestResponse.DeliveryState = GuestReplyDeliveryState.Pending;

                _context.FeedbackGuestResponses.Add(guestResponse);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    break;
                }
                catch (DbUpdateException)
                {
                    DetachIfTracked(issue);
                    DetachIfTracked(guestResponse);
                    if (reservation != null)
                    {
                        await _smsSend.ReleaseFailedAsync(reservation, cancellationToken);
                        reservation = null;
                    }

                    if (attempt >= OfferIssueService.MaxCodeAttempts)
                    {
                        throw new OfferIssueCodeAllocationException();
                    }
                }
            }

            if (isSms)
            {
                await SendSmsAndSettleAsync(
                    feedback,
                    issue!,
                    guestResponse!,
                    reservation!,
                    cancellationToken
                );
            }

            if (
                guestResponse!.EmailDeliveryStatus
                == GuestResponseEmailDeliveryStatus.Pending
            )
            {
                await _emailDelivery.NotifyAsync(
                    guestResponse.Id,
                    cancellationToken
                );
            }

            return new SendAndIssueFeedbackRecoveryOfferResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                GuestResponse = ToGuestResponseItemDto(guestResponse),
                RecoveryOffer = ToOfferItemDtoFromIssue(
                    issue!,
                    author.FullName,
                    intent
                ),
            };
        }

        public async Task<IReadOnlyList<FeedbackRecoveryOfferItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var oneOffRows = await _context.FeedbackRecoveryOffers
                .AsNoTracking()
                .Where(r => r.FeedbackId == feedbackId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            var issueRows = await _context.OfferIssues
                .AsNoTracking()
                .Where(
                    i =>
                        i.FeedbackId == feedbackId
                        && i.Source == OfferIssueSources.Recovery
                )
                .OrderByDescending(i => i.IssuedAtUtc)
                .ThenByDescending(i => i.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            // Author chrome for catalog issues: match the guest-response fact
            // written in the same atomic Send (same IssuedAtUtc / CreatedAt).
            var authorByIssuedAt = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(
                    r =>
                        r.FeedbackId == feedbackId
                        && r.Intent
                            == FeedbackRecoveryIntent.RespondWithRecoveryOffer
                )
                .Select(r => new { r.CreatedAt, r.AuthorDisplayName })
                .ToListAsync(cancellationToken);

            var authorLookup = authorByIssuedAt
                .GroupBy(row => row.CreatedAt)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().AuthorDisplayName
                );

            string AuthorForIssue(DateTime issuedAtUtc)
            {
                if (authorLookup.TryGetValue(issuedAtUtc, out var exact))
                {
                    return exact;
                }

                // Atomic Send uses the same instant; fall back to nearest
                // recovery guest-response author when clocks drift in tests.
                return authorByIssuedAt
                    .OrderBy(row => Math.Abs((row.CreatedAt - issuedAtUtc).Ticks))
                    .Select(row => row.AuthorDisplayName)
                    .FirstOrDefault()
                    ?? string.Empty;
            }

            var fromOneOffs = oneOffRows.Select(ToOfferItemDto);
            var fromIssues = issueRows.Select(
                issue =>
                    ToOfferItemDtoFromIssue(
                        issue,
                        AuthorForIssue(issue.IssuedAtUtc),
                        FeedbackRecoveryIntent.RespondWithRecoveryOffer
                    )
            );

            return fromOneOffs
                .Concat(fromIssues)
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(MaxListLimit)
                .ToList();
        }

        /// <summary>
        /// Sends the saved Offer SMS. A refused send cancels the Offer so a retry is
        /// allowed. An unknown outcome keeps the Offer, because the guest may hold the
        /// claim code. Both record the reply and throw. An accepted send is saved.
        /// </summary>
        private async Task SendSmsAndSettleAsync(
            Feedback feedback,
            OfferIssue issue,
            FeedbackGuestResponse guestResponse,
            GuestReplySmsReservation reservation,
            CancellationToken cancellationToken
        )
        {
            var outcome = await _smsSend.SendAndSettleAsync(
                reservation,
                feedback,
                cancellationToken
            );
            var now = DateTime.UtcNow;

            switch (outcome.Kind)
            {
                case GuestReplySmsOutcomeKind.Failed:
                    // Cancel the Claim and move the reply to Failed in one save that
                    // ignores the request token. If the save fails it is logged and
                    // the failed result is still returned.
                    issue.CancelledAtUtc = now;
                    await _smsSend.SaveFailedReplyAsync(guestResponse, now);

                    throw new GuestReplySmsSendFailedException();

                case GuestReplySmsOutcomeKind.Unknown:
                    // Keep the Claim open. The save ignores cancellation so the
                    // record is kept.
                    _smsSend.ApplyUnknownOutcome(guestResponse, reservation, now);
                    await _smsSend.SaveRecordedUnknownReplyAsync(reservation);

                    throw new GuestReplySmsSendFailedException();
            }

            _smsSend.ApplyAccepted(guestResponse, reservation, outcome.Accepted!, now);
            await _smsSend.SaveAcceptedReplyAsync(
                reservation,
                outcome.Settled,
                guestResponse
            );
        }

        private void DetachIfTracked(object? entity)
        {
            if (entity == null)
            {
                return;
            }

            var entry = _context.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// SMS carries Claim code as plain text only (no QR).
        /// </summary>
        internal static string AppendClaimCodeText(string body, string claimCode)
        {
            var trimmed = body.TrimEnd();
            if (trimmed.Contains(claimCode, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return $"{trimmed}\n\nOffer claim code: {claimCode}";
        }

        private static FeedbackGuestResponseItemDto ToGuestResponseItemDto(
            FeedbackGuestResponse row
        )
        {
            return new FeedbackGuestResponseItemDto
            {
                Id = row.Id,
                Channel = FeedbackGuestResponseMapping.ToWireChannel(row.Channel),
                Intent = FeedbackGuestResponseMapping.ToWireIntent(row.Intent),
                MaskedDestination = row.MaskedDestination,
                Subject = row.Subject,
                Body = row.Body,
                AuthorDisplayName = row.AuthorDisplayName,
                CreatedAt = row.CreatedAt,
                DeliveryState = GuestReplyDeliveryStateMachine.ToDto(row.DeliveryState),
                DeliveryStateAt = row.DeliveryStateAtUtc,
            };
        }

        private static FeedbackRecoveryOfferItemDto ToOfferItemDto(
            FeedbackRecoveryOffer row
        )
        {
            return new FeedbackRecoveryOfferItemDto
            {
                Id = row.Id,
                OfferType = FeedbackRecoveryOfferMapping.ToWireOfferType(row.OfferType),
                Title = row.Title,
                Description = row.Description,
                Validity = FeedbackRecoveryOfferMapping.ToWireValidity(row.Validity),
                ExpiryAt = row.ExpiryAt,
                DiscountPercentage = row.DiscountPercentage,
                DiscountAmount = row.DiscountAmount,
                FreeItemText = row.FreeItemText,
                PurchaseRequirement =
                    FeedbackRecoveryOfferMapping.ToWirePurchaseRequirement(
                        row.PurchaseRequirement
                    ),
                MinimumSpend = row.MinimumSpend,
                AdditionalExclusions = row.AdditionalExclusions,
                ReplacementItemText = row.ReplacementItemText,
                RedemptionCode = row.RedemptionCode,
                StaffInstructions = row.StaffInstructions,
                Intent = FeedbackInternalActionMapping.ToWireIntent(row.Intent),
                AuthorDisplayName = row.AuthorDisplayName,
                CreatedAt = row.CreatedAt,
            };
        }

        /// <summary>
        /// Wire-compat map from catalog Offer issue → recoveryOffer response
        /// shape for Send result and Feedback activity / details (ticket 06).
        /// </summary>
        private static FeedbackRecoveryOfferItemDto ToOfferItemDtoFromIssue(
            OfferIssue issue,
            string authorDisplayName,
            FeedbackRecoveryIntent intent
        )
        {
            return new FeedbackRecoveryOfferItemDto
            {
                Id = issue.Id,
                OfferType = CatalogOfferMapping.ToWireOfferType(issue.OfferType),
                Title = issue.Title,
                Description = issue.Description,
                Validity = CatalogOfferMapping.ToWireValidity(issue.Validity),
                ExpiryAt = issue.ExpiryAtUtc,
                DiscountPercentage = issue.DiscountPercentage,
                DiscountAmount = issue.DiscountAmount,
                FreeItemText = issue.FreeItemText,
                PurchaseRequirement =
                    CatalogOfferMapping.ToWirePurchaseRequirement(
                        issue.PurchaseRequirement
                    ),
                MinimumSpend = issue.MinimumSpend,
                AdditionalExclusions = issue.AdditionalExclusions,
                ReplacementItemText = issue.ReplacementItemText,
                RedemptionCode = issue.ClaimCode,
                RedemptionStatus = issue.RedeemedAtUtc == null
                    ? "not_redeemed"
                    : "redeemed",
                StaffInstructions = issue.StaffInstructions,
                Intent = FeedbackInternalActionMapping.ToWireIntent(intent),
                AuthorDisplayName = authorDisplayName,
                CreatedAt = issue.IssuedAtUtc,
            };
        }
    }
}
