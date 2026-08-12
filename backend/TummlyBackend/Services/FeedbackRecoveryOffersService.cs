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
        private readonly IGuestResponseEmailDeliveryWork _emailDelivery;
        private readonly IOfferIssueService _offerIssues;

        public FeedbackRecoveryOffersService(
            ApplicationDbContext context,
            IGuestResponseEmailDeliveryWork emailDelivery,
            IOfferIssueService offerIssues
        )
        {
            _context = context;
            _emailDelivery = emailDelivery;
            _offerIssues = offerIssues;
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
            await EnsureOffersAllowedAsync(feedback, cancellationToken);

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

            var issuedAt = DateTime.UtcNow;

            OfferIssue? issue = null;
            FeedbackGuestResponse? guestResponse = null;

            // Stage Offer issue + guest-response, then one SaveChanges so both
            // facts commit together (ADR 0026 / ticket 05 atomic Send).
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
                        "Recovery catalog offer must be Active and the guest must not be opted out."
                    );
                }

                var bodyForChannel = channel == FeedbackGuestResponseChannel.Sms
                    ? AppendClaimCodeText(content.Body, issue.ClaimCode)
                    : content.Body;

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
                    channel == FeedbackGuestResponseChannel.Email
                        ? GuestResponseEmailDeliveryStatus.Pending
                        : GuestResponseEmailDeliveryStatus.NotApplicable;

                _context.FeedbackGuestResponses.Add(guestResponse);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    break;
                }
                catch (DbUpdateException)
                {
                    if (attempt >= OfferIssueService.MaxCodeAttempts)
                    {
                        throw new OfferIssueCodeAllocationException();
                    }
                }
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

            var fromOneOffs = oneOffRows.Select(ToOfferItemDto);
            var fromIssues = issueRows.Select(
                issue =>
                    ToOfferItemDtoFromIssue(
                        issue,
                        authorLookup.TryGetValue(
                            issue.IssuedAtUtc,
                            out var authorDisplayName
                        )
                            ? authorDisplayName
                            : string.Empty,
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

        private async Task EnsureOffersAllowedAsync(
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            if (feedback.LocationGuestId is not { } locationGuestId)
            {
                return;
            }

            var optedOut = await _context.LocationGuests
                .AsNoTracking()
                .Where(g => g.Id == locationGuestId)
                .Select(g => g.OffersOptOut)
                .FirstOrDefaultAsync(cancellationToken);

            if (optedOut)
            {
                throw new ArgumentException(
                    "Guest has opted out of offers."
                );
            }
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
                StaffInstructions = issue.StaffInstructions,
                Intent = FeedbackInternalActionMapping.ToWireIntent(intent),
                AuthorDisplayName = authorDisplayName,
                CreatedAt = issue.IssuedAtUtc,
            };
        }
    }
}
