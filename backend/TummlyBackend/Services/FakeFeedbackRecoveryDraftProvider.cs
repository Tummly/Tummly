using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and local demos — no live Azure/OpenAI.
    /// </summary>
    public sealed class FakeFeedbackRecoveryDraftProvider
        : IFeedbackRecoveryDraftProvider
    {
        private FeedbackRecoveryDraftResult _nextResult =
            new FeedbackRecoveryDraftResult.Succeeded(
                Body: "Thank you for your feedback. We are looking into this.",
                Subject: "Regarding your recent visit",
                Channel: "email"
            );

        private Exception? _throwOnDraft;

        private FeedbackRecoveryDraftInput? _lastInput;

        public FeedbackRecoveryDraftInput? LastInput => _lastInput;

        public int CallCount { get; private set; }

        public void ResetCallCount()
        {
            CallCount = 0;
            _lastInput = null;
        }

        public void SucceedWith(
            string body,
            string? subject,
            string channel
        )
        {
            _throwOnDraft = null;
            _nextResult = new FeedbackRecoveryDraftResult.Succeeded(
                body,
                subject,
                channel
            );
        }

        public void Fail(bool retryable = true)
        {
            _throwOnDraft = null;
            _nextResult = new FeedbackRecoveryDraftResult.Failed(retryable);
        }

        public void ThrowOnDraft(Exception? exception = null)
        {
            _throwOnDraft =
                exception ?? new InvalidOperationException("Fake draft boom");
        }

        public Task<FeedbackRecoveryDraftResult> DraftAsync(
            FeedbackRecoveryDraftInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            _lastInput = input;

            if (_throwOnDraft is not null)
            {
                throw _throwOnDraft;
            }

            if (_nextResult is FeedbackRecoveryDraftResult.Succeeded succeeded)
            {
                var subject =
                    string.Equals(
                        input.Channel,
                        "sms",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? null
                        : succeeded.Subject;

                return Task.FromResult<FeedbackRecoveryDraftResult>(
                    new FeedbackRecoveryDraftResult.Succeeded(
                        succeeded.Body,
                        subject,
                        input.Channel
                    )
                );
            }

            return Task.FromResult(_nextResult);
        }
    }
}
