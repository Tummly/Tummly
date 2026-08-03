using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class FeedbackRecoveryDraftsService
        : IFeedbackRecoveryDraftsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackRecoveryDraftProvider _provider;

        public FeedbackRecoveryDraftsService(
            ApplicationDbContext context,
            IFeedbackRecoveryDraftProvider provider
        )
        {
            _context = context;
            _provider = provider;
        }

        public async Task<PrepareFeedbackRecoveryDraftResultDto?> PrepareAsync(
            int feedbackId,
            string channel,
            string purpose,
            string tone,
            string? includeNotes,
            string mode,
            string? currentBody,
            string? currentSubject,
            string? confirmedInternalActionCategory = null,
            string? confirmedInternalActionNote = null,
            FeedbackRecoveryOfferPayloadDto? confirmedOffer = null,
            CancellationToken cancellationToken = default
        )
        {
            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.RestaurantLocation)
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback is null)
            {
                return null;
            }

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);
            string? sentiment = null;
            IReadOnlyList<string> issueTags = Array.Empty<string>();
            if (classification.ClassificationStatus == "Succeeded")
            {
                sentiment = classification.Sentiment;
                issueTags = classification.DetectedTags ?? Array.Empty<string>();
            }

            var notes = string.IsNullOrWhiteSpace(includeNotes)
                ? null
                : includeNotes.Trim();

            var confirmedCategory =
                string.IsNullOrWhiteSpace(confirmedInternalActionCategory)
                    ? null
                    : confirmedInternalActionCategory.Trim();
            var confirmedNote =
                string.IsNullOrWhiteSpace(confirmedInternalActionNote)
                    ? null
                    : confirmedInternalActionNote.Trim();

            FeedbackRecoveryDraftConfirmedOffer? offerContext = null;
            if (confirmedOffer != null)
            {
                offerContext = new FeedbackRecoveryDraftConfirmedOffer(
                    OfferType: confirmedOffer.OfferType,
                    Title: confirmedOffer.Title,
                    Description: confirmedOffer.Description,
                    Validity: confirmedOffer.Validity,
                    ExpiryDate: confirmedOffer.ExpiryDate,
                    DiscountPercentage: confirmedOffer.DiscountPercentage,
                    DiscountAmount: confirmedOffer.DiscountAmount,
                    FreeItemText: confirmedOffer.FreeItemText,
                    PurchaseRequirement: confirmedOffer.PurchaseRequirement,
                    MinimumSpend: confirmedOffer.MinimumSpend,
                    AdditionalExclusions: confirmedOffer.AdditionalExclusions,
                    ReplacementItemText: confirmedOffer.ReplacementItemText
                );
            }

            // Adapter inputs exclude raw email/phone (resolve at send only).
            var input = new FeedbackRecoveryDraftInput(
                FeedbackComment: feedback.Comment,
                Sentiment: sentiment,
                IssueTags: issueTags,
                GuestDisplayName: feedback.GuestName,
                LocationName:
                    feedback.RestaurantLocation?.LocationName ?? string.Empty,
                Channel: channel,
                Purpose: purpose,
                Tone: tone,
                IncludeNotes: notes,
                Mode: mode,
                CurrentBody: currentBody,
                CurrentSubject: currentSubject,
                ConfirmedInternalActionCategory: confirmedCategory,
                ConfirmedInternalActionNote: confirmedNote,
                ConfirmedOffer: offerContext
            );

            FeedbackRecoveryDraftResult result;
            try
            {
                result = await _provider.DraftAsync(input, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new PrepareFeedbackRecoveryDraftResultDto
                {
                    Success = false,
                    Retryable = true,
                    Message = "We could not prepare a draft.",
                };
            }

            return result switch
            {
                FeedbackRecoveryDraftResult.Succeeded succeeded =>
                    new PrepareFeedbackRecoveryDraftResultDto
                    {
                        Success = true,
                        Body = succeeded.Body,
                        Subject = succeeded.Subject,
                        Channel = succeeded.Channel,
                        Retryable = false,
                    },
                FeedbackRecoveryDraftResult.Failed failed =>
                    new PrepareFeedbackRecoveryDraftResultDto
                    {
                        Success = false,
                        Retryable = failed.Retryable,
                        Message = "We could not prepare a draft.",
                    },
                _ => new PrepareFeedbackRecoveryDraftResultDto
                {
                    Success = false,
                    Retryable = true,
                    Message = "We could not prepare a draft.",
                },
            };
        }
    }
}
