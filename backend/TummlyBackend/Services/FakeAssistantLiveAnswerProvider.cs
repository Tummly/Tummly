using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
        /// Testing and local Fake twin — grounded from retrieved allow-list evidence.
        /// Emits one Assistant task so Testing never calls Azure.
    /// </summary>
    public sealed class FakeAssistantLiveAnswerProvider
        : IAssistantLiveAnswerProvider
    {
        private AssistantLiveAnswerResult? _forcedResult;
        private readonly Queue<AssistantLiveAnswerResult> _resultQueue = new();
        private Exception? _throwOnComplete;

        public AssistantLiveAnswerInput? LastInput { get; private set; }

        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        public void SucceedWith(
            AssistantMessageClass answerClass,
            string? title,
            string body,
            string assistantTask = AssistantTask.Retrieve,
            string? conversationTitle = null,
            AssistantOfferPathTermsState? offerTerms = null
        )
        {
            _throwOnComplete = null;
            _forcedResult = SucceededResult(
                answerClass,
                title,
                body,
                assistantTask,
                conversationTitle,
                offerTerms
            );
        }

        public void EnqueueSucceedWith(
            AssistantMessageClass answerClass,
            string? title,
            string body,
            string assistantTask = AssistantTask.Retrieve,
            string? conversationTitle = null,
            AssistantOfferPathTermsState? offerTerms = null
        )
        {
            _throwOnComplete = null;
            _resultQueue.Enqueue(
                SucceededResult(
                    answerClass,
                    title,
                    body,
                    assistantTask,
                    conversationTitle,
                    offerTerms
                )
            );
        }

        private static AssistantLiveAnswerResult.Succeeded SucceededResult(
            AssistantMessageClass answerClass,
            string? title,
            string body,
            string assistantTask,
            string? conversationTitle,
            AssistantOfferPathTermsState? offerTerms
        )
            => new(
                answerClass,
                title,
                body,
                [],
                assistantTask,
                conversationTitle,
                offerTerms
            );

        public void Fail(bool retryable = true)
        {
            _throwOnComplete = null;
            _forcedResult = new AssistantLiveAnswerResult.Failed(retryable);
        }

        public void ThrowOnComplete(Exception? exception = null)
        {
            _throwOnComplete =
                exception ?? new InvalidOperationException("Fake live answer boom");
        }

        public void ResetToCannedStub()
        {
            _throwOnComplete = null;
            Delay = TimeSpan.Zero;
            _forcedResult = null;
            _resultQueue.Clear();
        }

        public async Task<AssistantLiveAnswerResult> CompleteAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken = default
        )
        {
            LastInput = input;

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_throwOnComplete is not null)
            {
                throw _throwOnComplete;
            }

            if (_resultQueue.Count > 0)
            {
                return _resultQueue.Dequeue();
            }

            if (_forcedResult is not null)
            {
                return _forcedResult;
            }

            var task = AssistantTaskClassification.Classify(input.UserMessage);
            if (task == AssistantTask.CreateCampaignDraft)
            {
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Campaign Draft",
                    "Create Campaign Draft.",
                    [],
                    AssistantTask.CreateCampaignDraft
                );
            }

            if (task == AssistantTask.CreateCampaignWithOffer)
            {
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Campaign Draft with Offer",
                    "Create Campaign with Offer.",
                    [],
                    AssistantTask.CreateCampaignWithOffer
                );
            }

            if (task == AssistantTask.OfferPath)
            {
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Offers catalog Draft",
                    "Offer path.",
                    [],
                    AssistantTask.OfferPath
                );
            }

            if (task == AssistantTask.RecoveryPath)
            {
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Feedback recovery",
                    "Prepare Feedback recovery.",
                    [],
                    AssistantTask.RecoveryPath
                );
            }

            if (task == AssistantTask.Refuse)
            {
                var refuseKind = AssistantAskIntent.IsHelpCentreAsk(input.UserMessage)
                    ? AssistantAskKind.HelpCentre
                    : AssistantAskIntent.Classify(input.UserMessage);
                if (AssistantAskIntent.IsFullRefusal(refuseKind))
                {
                    return AssistantLiveAnswerCopy.Refusal(refuseKind);
                }
            }

            if (AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage))
            {
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    "Attention Retrieve",
                    "Attention Retrieve.",
                    [],
                    AssistantTask.Retrieve
                );
            }

            var productTopics = AssistantProductExpertTopics.Detect(input.UserMessage);
            if (productTopics.Count > 0
                && !AssistantAskIntent.IsHelpCentreAsk(input.UserMessage)
                && !AssistantProductExpertTopics.IsMixedRetrieve(input.UserMessage))
            {
                var canned = AssistantProductExpertTopics.Assemble(productTopics);
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    canned.Title,
                    canned.Body,
                    [],
                    AssistantTask.Retrieve,
                    canned.ConversationTitle
                );
            }

            if (input.CompareAll)
            {
                var compareAll = AssistantLiveAnswerCopy.CompareAllFromEvidence(
                    input.PeriodPhrase,
                    input.CompareLocations ?? [],
                    input.FailedLocationNames ?? [],
                    input.NotStartedLocationNames ?? []
                );
                return compareAll with { AssistantTask = AssistantTask.Retrieve };
            }

            if (input.CompareLocations is { Count: >= 2 })
            {
                var compare = AssistantLiveAnswerCopy.CompareFromEvidence(
                    input.UserMessage,
                    input.PeriodPhrase,
                    input.CompareLocations,
                    input.Evidence,
                    input.DroppedUnknownSentence
                );
                return compare with { AssistantTask = AssistantTask.Retrieve };
            }

            var grounded = AssistantLiveAnswerCopy.GroundedFromEvidence(
                input.UserMessage,
                input.OwnedLocationName,
                input.PeriodPhrase,
                input.Evidence,
                input.SuppressMixedRefusal
            );
            return AssistantLiveAnswerCopy.WithSentences(
                grounded,
                input.Caveat,
                input.DroppedUnknownSentence
            ) with { AssistantTask = AssistantTask.Retrieve };
        }
    }
}
