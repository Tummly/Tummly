using TummlyBackend.DTOs.Assistant;
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

        public int CompleteCount { get; private set; }

        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Extra delay after retrieve tools (simulates Azure final answer round).
        /// </summary>
        public TimeSpan SecondRoundDelay { get; set; } = TimeSpan.Zero;

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
            SecondRoundDelay = TimeSpan.Zero;
            _forcedResult = null;
            _resultQueue.Clear();
            CompleteCount = 0;
        }

        public async Task<AssistantLiveAnswerResult> CompleteAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken = default
        )
        {
            LastInput = input;
            CompleteCount++;

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
            if (task == AssistantTask.CreateCampaignDraft
                || task == AssistantTask.CreateCampaignWithOffer
                || task == AssistantTask.OfferPath
                || task == AssistantTask.RecoveryPath)
            {
                await RunRetrieveToolsIfEnabledAsync(input, cancellationToken);
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

                var recoveryEvidence = input.ReadToolEvidence?.Invoke() ?? input.Evidence;
                var recoveryGrounded = AssistantLiveAnswerCopy.GroundedFromEvidence(
                    input.UserMessage,
                    input.OwnedLocationName,
                    input.PeriodPhrase,
                    recoveryEvidence,
                    input.SuppressMixedRefusal
                );
                return new AssistantLiveAnswerResult.Succeeded(
                    AssistantMessageClass.Grounded,
                    recoveryGrounded.Title,
                    recoveryGrounded.Body,
                    recoveryGrounded.Actions,
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

            // Product-expert needles: Fake stands in for model knowledge (no domain
            // tools). Mixed retrieve+product asks fall through to tools.
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

            // Tools path: run retrieve tools then ground from tool evidence.
            if (input.ExecuteRetrieveTools is not null)
            {
                await RunRetrieveToolsIfEnabledAsync(input, cancellationToken);

                var toolEvidence = input.ReadToolEvidence?.Invoke() ?? input.Evidence;
                var toolCompare = input.ReadToolCompareLocations?.Invoke()
                    ?? input.CompareLocations;
                var (toolFailed, toolNotStarted) = input.ReadToolCompareAllMeta?.Invoke()
                    ?? (input.FailedLocationNames ?? [], input.NotStartedLocationNames ?? []);

                if (input.CompareAll)
                {
                    var compareAll = AssistantLiveAnswerCopy.CompareAllFromEvidence(
                        input.PeriodPhrase,
                        toolCompare ?? [],
                        toolFailed,
                        toolNotStarted
                    );
                    return compareAll with { AssistantTask = AssistantTask.Retrieve };
                }

                if (toolCompare is { Count: >= 2 } || input.NamedCompare)
                {
                    var compare = AssistantLiveAnswerCopy.CompareFromEvidence(
                        input.UserMessage,
                        input.PeriodPhrase,
                        toolCompare ?? [],
                        toolEvidence,
                        input.DroppedUnknownSentence
                    );
                    return compare with { AssistantTask = AssistantTask.Retrieve };
                }

                // Capability / greeting / other non-retrieve: no domain tools.
                if (!AssistantAskIntent.HasRetrieveAsk(input.UserMessage)
                    && !AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage)
                    && !AssistantTaskClassification.LooksLikeCreateTurn(input.UserMessage)
                    && !AssistantTaskClassification.LooksLikeRecoveryPath(input.UserMessage)
                    && !input.NamedCompare
                    && !input.CompareAll
                    && string.IsNullOrWhiteSpace(input.Caveat)
                    && string.IsNullOrWhiteSpace(input.DroppedUnknownSentence))
                {
                    return new AssistantLiveAnswerResult.Succeeded(
                        AssistantMessageClass.Grounded,
                        AssistantProductExpertCopy.CapabilitiesTitle,
                        AssistantProductExpertCopy.CapabilitiesBody,
                        [],
                        AssistantTask.Retrieve,
                        AssistantProductExpertCopy.CapabilitiesConversationTitle
                    );
                }

                var groundedFromTools = AssistantLiveAnswerCopy.GroundedFromEvidence(
                    input.UserMessage,
                    input.OwnedLocationName,
                    input.PeriodPhrase,
                    toolEvidence,
                    input.SuppressMixedRefusal
                );
                return AssistantLiveAnswerCopy.WithSentences(
                    groundedFromTools,
                    input.Caveat,
                    input.DroppedUnknownSentence
                ) with { AssistantTask = AssistantTask.Retrieve };
            }

            // Help-centre / early finishes may omit the tool executor.
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

        private async Task RunRetrieveToolsIfEnabledAsync(
            AssistantLiveAnswerInput input,
            CancellationToken cancellationToken
        )
        {
            if (input.ExecuteRetrieveTools is null)
            {
                return;
            }

            var toolCalls = ToolsForInput(input)
                .Select(
                    (name, index) => new AssistantToolCallRequest(
                        $"fake_{index}_{name}",
                        name,
                        name == AssistantRetrieveToolCatalog.ReadCampaigns
                        && AssistantAskIntent.NeedsCampaignCopy(input.UserMessage)
                            ? """{"includeCampaignCopy":true}"""
                            : "{}"
                    )
                )
                .ToList();
            if (toolCalls.Count == 0
                && !input.NamedCompare
                && !input.CompareAll)
            {
                return;
            }

            if (input.OnRetrieveProgress is not null)
            {
                await input.OnRetrieveProgress(
                    AssistantTurnProgressSteps.Retrieving,
                    cancellationToken
                );
            }

            if (toolCalls.Count > 0)
            {
                await input.ExecuteRetrieveTools(toolCalls, cancellationToken);
            }

            if (SecondRoundDelay > TimeSpan.Zero)
            {
                await Task.Delay(SecondRoundDelay, cancellationToken);
            }

            if (input.OnRetrieveProgress is not null)
            {
                await input.OnRetrieveProgress(
                    AssistantTurnProgressSteps.Preparing,
                    cancellationToken
                );
            }
        }

        private static IEnumerable<string> ToolsForInput(AssistantLiveAnswerInput input)
        {
            if (input.CompareAll)
            {
                return [AssistantRetrieveToolCatalog.CompareAllLocations];
            }

            if (input.NamedCompare)
            {
                return [AssistantRetrieveToolCatalog.CompareLocations];
            }

            var focus = AssistantAskFocus.Detect(input.UserMessage);
            if (AssistantAttentionAsk.IsAttentionRetrieve(input.UserMessage))
            {
                return AssistantRetrieveToolCatalog.DomainReads;
            }

            if (focus == AssistantAskFocusKind.Unknown
                && !AssistantAskIntent.HasRetrieveAsk(input.UserMessage)
                && !AssistantTaskClassification.LooksLikeCreateTurn(input.UserMessage)
                && !AssistantTaskClassification.LooksLikeRecoveryPath(input.UserMessage)
                && string.IsNullOrWhiteSpace(input.Caveat)
                && string.IsNullOrWhiteSpace(input.DroppedUnknownSentence))
            {
                // Capability / greeting / other non-domain asks: no forced reads.
                return [];
            }

            return ToolsForFocus(focus);
        }

        private static IEnumerable<string> ToolsForFocus(AssistantAskFocusKind focus)
            => AssistantRetrieveToolCatalog.DomainReadsForFocus(focus);
    }
}
