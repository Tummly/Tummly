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
