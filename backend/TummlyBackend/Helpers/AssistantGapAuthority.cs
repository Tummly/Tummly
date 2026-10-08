namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Open Gap resume authority: Continue, Cancel, Keep (Retrieve / Refuse),
    /// or Drop for a true replace create. Callers must not clear
    /// DraftInterviewJson on ordinary fills.
    /// </summary>
    public enum AssistantGapAuthorityDecision
    {
        ContinueGap = 0,
        Cancel = 1,
        KeepGapAnswerRetrieveOrRefuse = 2,
        DropForNewCreate = 3,
    }

    public static class AssistantGapAuthority
    {
        public static AssistantGapAuthorityDecision Decide(
            AssistantGapState gapState,
            string userMessage
        )
        {
            if (AssistantFlowControl.IsClearCancel(userMessage))
            {
                return AssistantGapAuthorityDecision.Cancel;
            }

            if (AssistantGapAsk.LooksLikeKeepGapAnswer(userMessage))
            {
                return AssistantGapAuthorityDecision.KeepGapAnswerRetrieveOrRefuse;
            }

            // Create-target and advisory Gaps always resume; chip paraphrases
            // and advisory choices must not early-drop on Detect needles.
            if (gapState.Kind == AssistantGapTurn.KindCreateTarget
                || AssistantGapTurn.IsAdvisoryGap(gapState)
                || gapState.Kind is AssistantGapTurn.KindOfferReplaceConfirm
                    or AssistantGapTurn.KindOfferRemoveConfirm)
            {
                return AssistantGapAuthorityDecision.ContinueGap;
            }

            var detected = AssistantCreateTargets.Detect(userMessage);
            if (detected.Count >= 2)
            {
                return AssistantGapAuthorityDecision.DropForNewCreate;
            }

            if (detected.Count == 1)
            {
                if (IsContinuationFill(gapState, userMessage, detected[0]))
                {
                    return AssistantGapAuthorityDecision.ContinueGap;
                }

                var gapTarget = CreateTargetForTask(gapState.AssistantTask);
                if (gapTarget is not null
                    && string.Equals(
                        detected[0],
                        gapTarget,
                        StringComparison.Ordinal
                    ))
                {
                    return AssistantGapAuthorityDecision.ContinueGap;
                }

                return AssistantGapAuthorityDecision.DropForNewCreate;
            }

            // No Detect hit. Drop only when Classify names a different create
            // surface than the open Gap (true replace). Same-surface wording
            // and ordinary fills Continue.
            var task = AssistantTaskClassification.Classify(userMessage);
            if (task is not (
                AssistantTask.CreateCampaignDraft
                or AssistantTask.CreateCampaignWithOffer
                or AssistantTask.OfferPath
                or AssistantTask.RecoveryPath
            ))
            {
                return AssistantGapAuthorityDecision.ContinueGap;
            }

            var classifiedTarget = CreateTargetForTask(task);
            var openTarget = CreateTargetForTask(gapState.AssistantTask);
            if (classifiedTarget is null || openTarget is null)
            {
                return AssistantGapAuthorityDecision.ContinueGap;
            }

            if (string.Equals(classifiedTarget, openTarget, StringComparison.Ordinal)
                || IsContinuationFill(gapState, userMessage, classifiedTarget))
            {
                return AssistantGapAuthorityDecision.ContinueGap;
            }

            return AssistantGapAuthorityDecision.DropForNewCreate;
        }

        /// <summary>
        /// Fill for the open Gap kind (Offer terms, bind choice, location on
        /// the same create target) even when Detect names a create noun.
        /// </summary>
        private static bool IsContinuationFill(
            AssistantGapState gapState,
            string userMessage,
            string detectedTarget
        )
        {
            if (gapState.Kind == AssistantGapTurn.KindOfferTerms)
            {
                var lower = userMessage.Trim().ToLowerInvariant();
                if (AssistantOfferPathTerms.LooksLikeValidityFollowUp(lower))
                {
                    return true;
                }

                var gapTarget = CreateTargetForTask(gapState.AssistantTask);
                if (gapTarget is not null
                    && string.Equals(
                        detectedTarget,
                        gapTarget,
                        StringComparison.Ordinal
                    ))
                {
                    return true;
                }

                // Combined create stores Offer-terms under Campaign task.
                if (string.Equals(
                        gapState.AssistantTask,
                        AssistantTask.CreateCampaignWithOffer,
                        StringComparison.Ordinal
                    )
                    && string.Equals(
                        detectedTarget,
                        AssistantCreateTargets.Offer,
                        StringComparison.Ordinal
                    ))
                {
                    return true;
                }

                return false;
            }

            if (gapState.Kind == AssistantGapTurn.KindFeedback)
            {
                return string.Equals(
                    detectedTarget,
                    AssistantCreateTargets.Recovery,
                    StringComparison.Ordinal
                );
            }

            if (AssistantGapTurn.IsBindKind(gapState.Kind)
                || gapState.Kind is AssistantGapTurn.KindCampaignTitle
                    or AssistantGapTurn.KindEmptyChannelAudience)
            {
                return AssistantCampaignDraftBind.ResolveNamedChoice(
                        gapState.Options,
                        userMessage
                    )
                    is not null;
            }

            if (gapState.Kind == AssistantGapTurn.KindLocation)
            {
                var gapTarget = CreateTargetForTask(gapState.AssistantTask);
                return gapTarget is not null
                    && string.Equals(
                        detectedTarget,
                        gapTarget,
                        StringComparison.Ordinal
                    );
            }

            return false;
        }

        private static string? CreateTargetForTask(string? assistantTask)
            => assistantTask switch
            {
                AssistantTask.CreateCampaignDraft => AssistantCreateTargets.Campaign,
                AssistantTask.CreateCampaignWithOffer => AssistantCreateTargets.Campaign,
                AssistantTask.OfferPath => AssistantCreateTargets.Offer,
                AssistantTask.RecoveryPath => AssistantCreateTargets.Recovery,
                _ => null,
            };
    }
}
