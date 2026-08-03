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
        public const int MaxTitleLength = 60;
        public const int MaxDescriptionLength = 240;
        public const int MaxStaffInstructionsLength = 1000;
        public const int MaxListLimit = 100;
        public const int MaxCodeAttempts = 8;

        private readonly ApplicationDbContext _context;

        public FeedbackRecoveryOffersService(ApplicationDbContext context)
        {
            _context = context;
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

            var offerDto = request.Offer
                ?? throw new ArgumentException("Offer is required.");

            if (!FeedbackRecoveryOfferMapping.TryParseOfferType(
                    offerDto.OfferType,
                    out var offerType
                ))
            {
                throw new ArgumentException("Offer type is invalid.");
            }

            if (!FeedbackRecoveryOfferMapping.TryParseValidity(
                    offerDto.Validity,
                    out var validity
                ))
            {
                throw new ArgumentException("Offer validity is invalid.");
            }

            var title = (offerDto.Title ?? string.Empty).Trim();
            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                throw new ArgumentException(
                    $"Offer title is required (max {MaxTitleLength})."
                );
            }

            var description = (offerDto.Description ?? string.Empty).Trim();
            if (description.Length == 0 || description.Length > MaxDescriptionLength)
            {
                throw new ArgumentException(
                    $"Offer description is required (max {MaxDescriptionLength})."
                );
            }

            decimal? discountPercentage = null;
            decimal? discountAmount = null;
            string? freeItemText = null;
            FeedbackRecoveryOfferPurchaseRequirement? purchaseRequirement = null;
            decimal? minimumSpend = null;
            string? additionalExclusions = null;
            string? replacementItemText = null;
            DateOnly? customExpiryDate = null;

            if (offerType == FeedbackRecoveryOfferType.PercentageDiscount)
            {
                if (offerDto.DiscountPercentage is not { } pct || pct <= 0)
                {
                    throw new ArgumentException(
                        "Discount percentage must be greater than 0."
                    );
                }

                discountPercentage = pct;
            }
            else if (offerType == FeedbackRecoveryOfferType.FixedDiscount)
            {
                if (offerDto.DiscountAmount is not { } amount || amount <= 0)
                {
                    throw new ArgumentException(
                        "Discount amount must be greater than 0."
                    );
                }

                discountAmount = amount;
            }
            else if (offerType == FeedbackRecoveryOfferType.FreeItem)
            {
                freeItemText = (offerDto.FreeItemText ?? string.Empty).Trim();
                if (freeItemText.Length == 0)
                {
                    throw new ArgumentException("Free item text is required.");
                }

                if (!FeedbackRecoveryOfferMapping.TryParsePurchaseRequirement(
                        offerDto.PurchaseRequirement,
                        out var req
                    ))
                {
                    throw new ArgumentException(
                        "Purchase requirement is required for free item."
                    );
                }

                purchaseRequirement = req;
                if (req == FeedbackRecoveryOfferPurchaseRequirement.WithMinimumSpend)
                {
                    if (offerDto.MinimumSpend is not { } spend || spend <= 0)
                    {
                        throw new ArgumentException(
                            "Minimum spend must be greater than 0."
                        );
                    }

                    minimumSpend = spend;
                }

                additionalExclusions = string.IsNullOrWhiteSpace(
                    offerDto.AdditionalExclusions
                )
                    ? null
                    : offerDto.AdditionalExclusions.Trim();
            }
            else if (offerType == FeedbackRecoveryOfferType.ReplacementItem)
            {
                replacementItemText =
                    (offerDto.ReplacementItemText ?? string.Empty).Trim();
                if (replacementItemText.Length == 0)
                {
                    throw new ArgumentException(
                        "Replacement item text is required."
                    );
                }
            }

            if (validity == FeedbackRecoveryOfferValidity.ChooseExpiryDate)
            {
                if (!DateOnly.TryParse(offerDto.ExpiryDate, out var parsed))
                {
                    throw new ArgumentException(
                        "Expiry date is required when choosing an expiry date."
                    );
                }

                customExpiryDate = parsed;
            }

            var staffInstructions = string.IsNullOrWhiteSpace(
                offerDto.StaffInstructions
            )
                ? null
                : offerDto.StaffInstructions.Trim();
            if (
                staffInstructions != null
                && staffInstructions.Length > MaxStaffInstructionsLength
            )
            {
                throw new ArgumentException(
                    $"Staff instructions must be at most {MaxStaffInstructionsLength} characters."
                );
            }

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

            var issuedAt = DateTime.UtcNow;
            var expiryAt = FeedbackRecoveryOfferMapping.ComputeExpiryAt(
                validity,
                issuedAt,
                customExpiryDate
            );

            FeedbackGuestResponse guestResponse = null!;
            FeedbackRecoveryOffer recoveryOffer = null!;

            // Generate-check-insert with a bounded retry: the existence check
            // catches the vast majority of collisions cheaply, and the
            // DbUpdateException catch around SaveChangesAsync is a
            // defense-in-depth net for the residual check-then-insert race
            // window on databases that enforce the unique index (e.g. two
            // concurrent requests both passing the existence check for the
            // same code) — see GuestTaggingService for the same pattern.
            for (var attempt = 1; ; attempt++)
            {
                var redemptionCode = GenerateCandidateCode();

                var codeExists = await _context.FeedbackRecoveryOffers
                    .AsNoTracking()
                    .AnyAsync(
                        o => o.RedemptionCode == redemptionCode,
                        cancellationToken
                    );

                if (codeExists)
                {
                    if (attempt >= MaxCodeAttempts)
                    {
                        throw new FeedbackRecoveryOfferCodeAllocationException();
                    }

                    continue;
                }

                guestResponse = FeedbackGuestResponseComposer.Build(
                    feedback,
                    channel,
                    intent,
                    content,
                    purpose,
                    request.Tone,
                    request.IncludeNotes,
                    authorUserId,
                    author.FullName,
                    issuedAt
                );

                recoveryOffer = new FeedbackRecoveryOffer
                {
                    FeedbackId = feedbackId,
                    GuestResponse = guestResponse,
                    OfferType = offerType,
                    Title = title,
                    Description = description,
                    Validity = validity,
                    ExpiryAt = expiryAt,
                    DiscountPercentage = discountPercentage,
                    DiscountAmount = discountAmount,
                    FreeItemText = freeItemText,
                    PurchaseRequirement = purchaseRequirement,
                    MinimumSpend = minimumSpend,
                    AdditionalExclusions = additionalExclusions,
                    ReplacementItemText = replacementItemText,
                    RedemptionCode = redemptionCode,
                    StaffInstructions = staffInstructions,
                    Intent = intent,
                    AuthorUserId = authorUserId,
                    AuthorDisplayName = author.FullName,
                    CreatedAt = issuedAt,
                };

                _context.FeedbackGuestResponses.Add(guestResponse);
                _context.FeedbackRecoveryOffers.Add(recoveryOffer);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    break;
                }
                catch (DbUpdateException)
                {
                    DetachIfTracked(guestResponse);
                    DetachIfTracked(recoveryOffer);

                    if (attempt >= MaxCodeAttempts)
                    {
                        throw new FeedbackRecoveryOfferCodeAllocationException();
                    }
                }
            }

            return new SendAndIssueFeedbackRecoveryOfferResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                GuestResponse = ToGuestResponseItemDto(guestResponse),
                RecoveryOffer = ToOfferItemDto(recoveryOffer),
            };
        }

        public async Task<IReadOnlyList<FeedbackRecoveryOfferItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackRecoveryOffers
                .AsNoTracking()
                .Where(r => r.FeedbackId == feedbackId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows.Select(ToOfferItemDto).ToList();
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

        /// <summary>
        /// Generates a candidate redemption code. Overridable in tests to
        /// force unique-index collisions deterministically.
        /// </summary>
        protected virtual string GenerateCandidateCode()
        {
            return FeedbackRecoveryOfferMapping.GenerateRedemptionCode();
        }

        private void DetachIfTracked(object entity)
        {
            var entry = _context.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
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
    }
}
