using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
        /// Testing and local Fake twin — grounded from retrieved allow-list evidence.
    /// </summary>
    public sealed class FakeAssistantLiveAnswerProvider
        : IAssistantLiveAnswerProvider
    {
        private AssistantLiveAnswerResult? _forcedResult;
        private Exception? _throwOnComplete;

        public AssistantLiveAnswerInput? LastInput { get; private set; }

        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        public void SucceedWith(
            AssistantMessageClass answerClass,
            string? title,
            string body
        )
        {
            _throwOnComplete = null;
            _forcedResult = new AssistantLiveAnswerResult.Succeeded(
                answerClass,
                title,
                body,
                []
            );
        }

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

            if (_forcedResult is not null)
            {
                return _forcedResult;
            }

            var ask = AssistantAskIntent.Classify(input.UserMessage);
            if (AssistantAskIntent.IsFullRefusal(ask))
            {
                return AssistantLiveAnswerCopy.Refusal(ask);
            }

            if (input.CompareLocations is { Count: >= 2 })
            {
                return AssistantLiveAnswerCopy.CompareFromEvidence(
                    input.UserMessage,
                    input.PeriodPhrase,
                    input.CompareLocations,
                    input.Evidence.Feedback,
                    input.DroppedUnknownSentence
                );
            }

            var grounded = AssistantLiveAnswerCopy.GroundedFromEvidence(
                input.UserMessage,
                input.OwnedLocationName,
                input.PeriodPhrase,
                input.Evidence
            );
            return AssistantLiveAnswerCopy.WithSentences(
                grounded,
                input.Caveat,
                input.DroppedUnknownSentence
            );
        }
    }
}
