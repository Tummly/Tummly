using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// When Azure live answer fails or mis-labels create, fall back to local
    /// Classify + question-first grounded copy (same shape as Fake).
    /// </summary>
    public static class AssistantLiveAnswerResolve
    {
        public static AssistantLiveAnswerResult Resolve(
            AssistantLiveAnswerResult providerResult,
            string userMessage,
            string ownedLocationName,
            string periodPhrase,
            AssistantRetrievedEvidence evidence,
            bool allowLocalRetrieveFallback
        )
        {
            if (AssistantAskIntent.IsHelpCentreAsk(userMessage))
            {
                return providerResult;
            }

            var localTask = AssistantTaskClassification.Classify(userMessage);
            if (providerResult is AssistantLiveAnswerResult.Succeeded inventedOffer
                && string.Equals(
                    inventedOffer.AssistantTask,
                    AssistantTask.CreateCampaignWithOffer,
                    StringComparison.Ordinal
                )
                && localTask != AssistantTask.CreateCampaignWithOffer
                && localTask != AssistantTask.OfferPath
                && !AssistantTaskClassification.NamesOfferBenefit(userMessage))
            {
                providerResult = inventedOffer with
                {
                    AssistantTask = localTask == AssistantTask.CreateCampaignDraft
                        ? AssistantTask.CreateCampaignDraft
                        : AssistantTask.Retrieve,
                    OfferTerms = null,
                };
            }

            if (IsCreateOrRecoveryTask(localTask))
            {
                if (providerResult is AssistantLiveAnswerResult.Failed)
                {
                    return CreateTaskStub(localTask);
                }

                // Azure sometimes returns Retrieve for bare create asks.
                // Prefer local CreateCampaignDraft only (not with-offer) so an
                // intentional Retrieve on combined create still skips persist.
                if (providerResult is AssistantLiveAnswerResult.Succeeded succeeded
                    && string.Equals(
                        succeeded.AssistantTask,
                        AssistantTask.Retrieve,
                        StringComparison.Ordinal
                    )
                    && localTask == AssistantTask.CreateCampaignDraft)
                {
                    return CreateTaskStub(localTask);
                }

                // Same for Offer path mutate follow-ups ("change the offer to 15%"):
                // local Classify is OfferPath; Azure may still emit Retrieve.
                if (providerResult is AssistantLiveAnswerResult.Succeeded offerSucceeded
                    && string.Equals(
                        offerSucceeded.AssistantTask,
                        AssistantTask.Retrieve,
                        StringComparison.Ordinal
                    )
                    && localTask == AssistantTask.OfferPath)
                {
                    return CreateTaskStub(localTask);
                }

                // Azure often mislabels offer-mutate follow-ups as Campaign+Offer
                // create ("change offer to 85%"). Persist follows assistantTask and
                // would mint a second Campaign Draft with Email defaults.
                if (providerResult is AssistantLiveAnswerResult.Succeeded offerMutateLabeled
                    && localTask == AssistantTask.OfferPath
                    && (
                        string.Equals(
                            offerMutateLabeled.AssistantTask,
                            AssistantTask.CreateCampaignWithOffer,
                            StringComparison.Ordinal
                        )
                        || string.Equals(
                            offerMutateLabeled.AssistantTask,
                            AssistantTask.CreateCampaignDraft,
                            StringComparison.Ordinal
                        )
                    ))
                {
                    return offerMutateLabeled with
                    {
                        AssistantTask = AssistantTask.OfferPath,
                    };
                }

                // Azure sometimes labels a Campaign+Offer ask as plain
                // create-campaign-draft (QA: thanking guests + 10% off). Persist
                // follows assistantTask, so upgrade to the local combined task
                // while keeping the model title/body until persist overwrites.
                if (providerResult is AssistantLiveAnswerResult.Succeeded draftLabeled
                    && string.Equals(
                        draftLabeled.AssistantTask,
                        AssistantTask.CreateCampaignDraft,
                        StringComparison.Ordinal
                    )
                    && localTask == AssistantTask.CreateCampaignWithOffer)
                {
                    return draftLabeled with
                    {
                        AssistantTask = AssistantTask.CreateCampaignWithOffer,
                    };
                }

                // Campaign audience/channel mutate must not become combined create.
                if (providerResult is AssistantLiveAnswerResult.Succeeded campaignMutateLabeled
                    && localTask == AssistantTask.CreateCampaignDraft
                    && AssistantPriorDraftAuthority.LooksLikeMutateCampaignFamily(userMessage)
                    && string.Equals(
                        campaignMutateLabeled.AssistantTask,
                        AssistantTask.CreateCampaignWithOffer,
                        StringComparison.Ordinal
                    ))
                {
                    return campaignMutateLabeled with
                    {
                        AssistantTask = AssistantTask.CreateCampaignDraft,
                    };
                }

                // Recovery follow-ups must not mint Campaign/Offer drafts.
                if (providerResult is AssistantLiveAnswerResult.Succeeded recoveryLabeled
                    && localTask == AssistantTask.RecoveryPath
                    && (
                        string.Equals(
                            recoveryLabeled.AssistantTask,
                            AssistantTask.CreateCampaignWithOffer,
                            StringComparison.Ordinal
                        )
                        || string.Equals(
                            recoveryLabeled.AssistantTask,
                            AssistantTask.CreateCampaignDraft,
                            StringComparison.Ordinal
                        )
                        || string.Equals(
                            recoveryLabeled.AssistantTask,
                            AssistantTask.OfferPath,
                            StringComparison.Ordinal
                        )
                    ))
                {
                    return recoveryLabeled with
                    {
                        AssistantTask = AssistantTask.RecoveryPath,
                    };
                }
            }

            if (providerResult is AssistantLiveAnswerResult.Failed
                && allowLocalRetrieveFallback)
            {
                return AssistantLiveAnswerCopy.GroundedFromEvidence(
                    userMessage,
                    ownedLocationName,
                    periodPhrase,
                    evidence
                ) with
                {
                    AssistantTask = AssistantTask.Retrieve,
                };
            }

            return providerResult;
        }

        private static bool IsCreateOrRecoveryTask(string task)
            => task is AssistantTask.CreateCampaignDraft
                or AssistantTask.CreateCampaignWithOffer
                or AssistantTask.OfferPath
                or AssistantTask.RecoveryPath;

        private static AssistantLiveAnswerResult.Succeeded CreateTaskStub(string task)
            => new(
                AssistantMessageClass.Grounded,
                TitleFor(task),
                BodyFor(task),
                [],
                task
            );

        private static string TitleFor(string task)
            => task switch
            {
                AssistantTask.CreateCampaignWithOffer => "Campaign Draft with Offer",
                AssistantTask.OfferPath => "Offers catalog Draft",
                AssistantTask.RecoveryPath => "Feedback recovery",
                _ => "Campaign Draft",
            };

        private static string BodyFor(string task)
            => task switch
            {
                AssistantTask.CreateCampaignWithOffer => "Create Campaign with Offer.",
                AssistantTask.OfferPath => "Offer path.",
                AssistantTask.RecoveryPath => "Prepare Feedback recovery.",
                _ => "Create Campaign Draft.",
            };
    }
}
