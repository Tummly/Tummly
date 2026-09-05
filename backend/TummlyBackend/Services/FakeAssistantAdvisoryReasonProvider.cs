using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Testing Fake for advisory Clear → Reason. No live Azure.
    /// </summary>
    public sealed class FakeAssistantAdvisoryReasonProvider
        : IAssistantAdvisoryReasonProvider
    {
        private AssistantAdvisoryReasonResult? _forcedResult;
        private readonly Queue<AssistantAdvisoryReasonResult> _resultQueue = new();
        private Exception? _throwOnComplete;

        public AssistantAdvisoryReasonInput? LastInput { get; private set; }

        public int CompleteCount { get; private set; }

        public void SucceedWith(AssistantAdvisoryReasonOutput output)
        {
            _throwOnComplete = null;
            _forcedResult = new AssistantAdvisoryReasonResult.Succeeded(output);
        }

        public void EnqueueSucceedWith(AssistantAdvisoryReasonOutput output)
        {
            _throwOnComplete = null;
            _resultQueue.Enqueue(
                new AssistantAdvisoryReasonResult.Succeeded(output)
            );
        }

        public void Fail(bool retryable = true)
        {
            _throwOnComplete = null;
            _forcedResult = new AssistantAdvisoryReasonResult.Failed(retryable);
        }

        public void ThrowOnComplete(Exception? exception = null)
        {
            _throwOnComplete =
                exception
                ?? new InvalidOperationException("Fake advisory Reason boom");
        }

        public void ResetToCannedStub()
        {
            _throwOnComplete = null;
            _forcedResult = null;
            _resultQueue.Clear();
            CompleteCount = 0;
            LastInput = null;
        }

        public Task<AssistantAdvisoryReasonResult> CompleteAsync(
            AssistantAdvisoryReasonInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastInput = input;
            CompleteCount++;

            if (_throwOnComplete is not null)
            {
                throw _throwOnComplete;
            }

            if (_resultQueue.Count > 0)
            {
                return Task.FromResult(_resultQueue.Dequeue());
            }

            if (_forcedResult is not null)
            {
                return Task.FromResult(_forcedResult);
            }

            return Task.FromResult(DefaultFor(input.Snapshot));
        }

        private static AssistantAdvisoryReasonResult DefaultFor(
            RestaurantContextSnapshot snapshot
        )
        {
            var covers = snapshot.Account.Covers.Current;
            if (covers > 0m
                || snapshot.Account.Covers.Prior is not null
                || snapshot.Account.Covers.PctDelta is not null)
            {
                return new AssistantAdvisoryReasonResult.Succeeded(
                    new AssistantAdvisoryReasonOutput(
                        AnswerType: "advisory",
                        Summary:
                            $"Covers look stable at {covers:0} in the current window.",
                        ClarifyingQuestion: null,
                        Recommendations:
                        [
                            new AssistantAdvisoryReasonRecommendation(
                                Action: AssistantAdvisoryReasonStructuredOutput
                                    .AdviceOnlyAction,
                                Headline: "Watch covers",
                                Reason: "Account.Covers shows the current load.",
                                EvidenceRef: ["Account.Covers"],
                                Confidence: "medium"
                            ),
                        ],
                        EvidenceUsed: ["Account.Covers"]
                    )
                );
            }

            return new AssistantAdvisoryReasonResult.Succeeded(
                new AssistantAdvisoryReasonOutput(
                    AnswerType: "direct",
                    Summary: "Here is a short read of the current account window.",
                    ClarifyingQuestion: null,
                    Recommendations: [],
                    EvidenceUsed: ["Account"]
                )
            );
        }
    }
}
