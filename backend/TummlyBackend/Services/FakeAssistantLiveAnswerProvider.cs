using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Testing and local Fake twin — canned grounded body, no restaurant retrieve.
    /// </summary>
    public sealed class FakeAssistantLiveAnswerProvider
        : IAssistantLiveAnswerProvider
    {
        private AssistantLiveAnswerResult _nextResult =
            BuildDefaultStub("Camden", "the last 7 days");

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
            _nextResult = new AssistantLiveAnswerResult.Succeeded(
                answerClass,
                title,
                body
            );
        }

        public void Fail(bool retryable = true)
        {
            _throwOnComplete = null;
            _nextResult = new AssistantLiveAnswerResult.Failed(retryable);
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
            _nextResult = BuildDefaultStub("Camden", "the last 7 days");
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

            if (_nextResult is AssistantLiveAnswerResult.Succeeded)
            {
                return BuildDefaultStub(input.OwnedLocationName, input.PeriodPhrase);
            }

            return _nextResult;
        }

        public static AssistantLiveAnswerResult.Succeeded BuildDefaultStub(
            string ownedLocationName,
            string periodPhrase
        )
            => new(
                AssistantMessageClass.Grounded,
                $"A stub summary for {ownedLocationName}",
                $"This is a canned grounded live answer for {ownedLocationName} over {periodPhrase}. Restaurant retrieve is not wired yet."
            );
    }
}
