using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Derives Feedback details activity from submission time, notes (including
    /// soft-deletes), classification correction facts, Detected Tags change
    /// facts, workflow-status change facts, close-out facts, guest-response
    /// facts, internal-action facts, and recovery-completion facts (not a
    /// separate event store).
    /// Body edits and same-to-same status no-ops do not produce history rows.
    /// Status-change rows referenced by a close-out or recovery completion emit
    /// feedback_closed_out / recovery_completed instead of a bare
    /// workflow_status_changed beat.
    /// </summary>
    public static class FeedbackActivityHistory
    {
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteActivityFactDto> noteFacts,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null,
            IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>? workflowChangesNewestFirst = null,
            IReadOnlyList<FeedbackCloseOutItemDto>? closeOutsNewestFirst = null,
            IReadOnlyList<FeedbackGuestResponseItemDto>? guestResponsesNewestFirst = null,
            IReadOnlyList<FeedbackRecoveryCompletionItemDto>? recoveryCompletionsNewestFirst = null,
            IReadOnlyList<FeedbackInternalActionItemDto>? internalActionsNewestFirst = null,
            IReadOnlyList<FeedbackRecoveryOfferItemDto>? recoveryOffersNewestFirst = null,
            IReadOnlyList<FeedbackDetectedTagsChangeItemDto>? detectedTagsChangesNewestFirst = null
        )
        {
            var corrections = correctionsNewestFirst
                ?? Array.Empty<FeedbackClassificationCorrectionItemDto>();
            var workflowChanges = workflowChangesNewestFirst
                ?? Array.Empty<FeedbackWorkflowStatusChangeItemDto>();
            var closeOuts = closeOutsNewestFirst
                ?? Array.Empty<FeedbackCloseOutItemDto>();
            var guestResponses = guestResponsesNewestFirst
                ?? Array.Empty<FeedbackGuestResponseItemDto>();
            var recoveryCompletions = recoveryCompletionsNewestFirst
                ?? Array.Empty<FeedbackRecoveryCompletionItemDto>();
            var internalActions = internalActionsNewestFirst
                ?? Array.Empty<FeedbackInternalActionItemDto>();
            var recoveryOffers = recoveryOffersNewestFirst
                ?? Array.Empty<FeedbackRecoveryOfferItemDto>();
            var detectedTagsChanges = detectedTagsChangesNewestFirst
                ?? Array.Empty<FeedbackDetectedTagsChangeItemDto>();

            var linkedStatusChangeIds = closeOuts
                .Select(c => c.WorkflowStatusChangeId)
                .Concat(recoveryCompletions.Select(c => c.WorkflowStatusChangeId))
                .ToHashSet();

            var events = new List<FeedbackActivityEventDto>(
                1
                    + (noteFacts.Count * 2)
                    + corrections.Count
                    + detectedTagsChanges.Count
                    + workflowChanges.Count
                    + closeOuts.Count
                    + guestResponses.Count
                    + internalActions.Count
                    + recoveryOffers.Count
                    + recoveryCompletions.Count
            )
            {
                new FeedbackActivityEventDto
                {
                    Kind = "feedback_received",
                    At = feedbackCreatedAt,
                },
            };

            var noteEvents = new List<FeedbackActivityEventDto>(noteFacts.Count * 2);
            foreach (var note in noteFacts)
            {
                noteEvents.Add(
                    new FeedbackActivityEventDto
                    {
                        Kind = "note_added",
                        At = note.CreatedAt,
                        ActorDisplayName = note.AuthorDisplayName,
                    }
                );

                if (note.DeletedAt is { } deletedAt)
                {
                    noteEvents.Add(
                        new FeedbackActivityEventDto
                        {
                            Kind = "note_deleted",
                            At = deletedAt,
                            ActorDisplayName = note.DeletedByDisplayName,
                        }
                    );
                }
            }

            var correctionEvents = corrections
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            var detectedTagsEvents = detectedTagsChanges
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            var workflowEvents = workflowChanges
                .Where(c => !linkedStatusChangeIds.Contains(c.Id))
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            var closeOutEvents = closeOuts
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            var guestResponseEvents = guestResponses
                .OrderBy(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .Select(ToActivityEvent);

            var internalActionEvents = internalActions
                .OrderBy(a => a.CreatedAt)
                .ThenBy(a => a.Id)
                .Select(ToActivityEvent);

            var recoveryOfferEvents = recoveryOffers
                .OrderBy(o => o.CreatedAt)
                .ThenBy(o => o.Id)
                .Select(ToActivityEvent);

            var recoveryCompletionEvents = recoveryCompletions
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            events.AddRange(
                noteEvents
                    .Concat(correctionEvents)
                    .Concat(detectedTagsEvents)
                    .Concat(workflowEvents)
                    .Concat(closeOutEvents)
                    .Concat(guestResponseEvents)
                    .Concat(internalActionEvents)
                    .Concat(recoveryOfferEvents)
                    .Concat(recoveryCompletionEvents)
                    .OrderBy(e => e.At)
                    .ThenBy(e => e.Kind)
            );

            return events;
        }

        /// <summary>
        /// Backward-compatible overload for callers that only have visible notes
        /// (no soft-delete facts). Prefer the activity-facts overload.
        /// </summary>
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteItemDto> notesNewestFirst,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null,
            IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>? workflowChangesNewestFirst = null,
            IReadOnlyList<FeedbackCloseOutItemDto>? closeOutsNewestFirst = null,
            IReadOnlyList<FeedbackGuestResponseItemDto>? guestResponsesNewestFirst = null,
            IReadOnlyList<FeedbackRecoveryCompletionItemDto>? recoveryCompletionsNewestFirst = null,
            IReadOnlyList<FeedbackInternalActionItemDto>? internalActionsNewestFirst = null,
            IReadOnlyList<FeedbackRecoveryOfferItemDto>? recoveryOffersNewestFirst = null,
            IReadOnlyList<FeedbackDetectedTagsChangeItemDto>? detectedTagsChangesNewestFirst = null
        )
        {
            var facts = notesNewestFirst
                .Select(n => new FeedbackInternalNoteActivityFactDto
                {
                    Id = n.Id,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                })
                .ToList();

            return Derive(
                feedbackCreatedAt,
                facts,
                correctionsNewestFirst,
                workflowChangesNewestFirst,
                closeOutsNewestFirst,
                guestResponsesNewestFirst,
                recoveryCompletionsNewestFirst,
                internalActionsNewestFirst,
                recoveryOffersNewestFirst,
                detectedTagsChangesNewestFirst
            );
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackClassificationCorrectionItemDto correction
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "classification_corrected",
                At = correction.CreatedAt,
                ActorDisplayName = correction.AuthorDisplayName,
                FromSentiment = correction.FromSentiment,
                ToSentiment = correction.ToSentiment,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackDetectedTagsChangeItemDto change
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "detected_tags_updated",
                At = change.CreatedAt,
                ActorDisplayName = change.AuthorDisplayName,
                FromSentiment = change.FromSentiment,
                ToSentiment = change.ToSentiment,
                FromDetectedTags = change.FromDetectedTags,
                ToDetectedTags = change.ToDetectedTags,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackWorkflowStatusChangeItemDto change
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "workflow_status_changed",
                At = change.CreatedAt,
                ActorDisplayName = change.AuthorDisplayName,
                FromWorkflowStatus = change.FromWorkflowStatus,
                ToWorkflowStatus = change.ToWorkflowStatus,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackCloseOutItemDto closeOut
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "feedback_closed_out",
                At = closeOut.CreatedAt,
                ActorDisplayName = closeOut.AuthorDisplayName,
                FromWorkflowStatus = closeOut.FromWorkflowStatus,
                ToWorkflowStatus = closeOut.ToWorkflowStatus,
                CloseOutIntent = closeOut.Intent,
                CloseOutReason = closeOut.Reason,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackGuestResponseItemDto guestResponse
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "guest_response_sent",
                At = guestResponse.CreatedAt,
                ActorDisplayName = guestResponse.AuthorDisplayName,
                Channel = guestResponse.Channel,
                MaskedDestination = guestResponse.MaskedDestination,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackInternalActionItemDto internalAction
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "internal_action_recorded",
                At = internalAction.CreatedAt,
                ActorDisplayName = internalAction.AuthorDisplayName,
                Category = internalAction.Category,
                CategoryLabel = internalAction.CategoryLabel,
                Note = internalAction.Note,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackRecoveryOfferItemDto recoveryOffer
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "recovery_offer_issued",
                At = recoveryOffer.CreatedAt,
                ActorDisplayName = recoveryOffer.AuthorDisplayName,
                OfferType = recoveryOffer.OfferType,
                OfferTitle = recoveryOffer.Title,
                OfferValidity = recoveryOffer.Validity,
                OfferExpiryAt = recoveryOffer.ExpiryAt,
                RedemptionCode = recoveryOffer.RedemptionCode,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackRecoveryCompletionItemDto completion
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "recovery_completed",
                At = completion.CreatedAt,
                ActorDisplayName = completion.AuthorDisplayName,
                FromWorkflowStatus = completion.FromWorkflowStatus,
                ToWorkflowStatus = completion.ToWorkflowStatus,
                RecoveryIntent = completion.Intent,
            };
        }
    }
}
