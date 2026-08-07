using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and local demos — no live Azure/OpenAI.
    /// </summary>
    public sealed class FakeCampaignMessageDraftProvider
        : ICampaignMessageDraftProvider
    {
        private CampaignMessageDraftProviderResult _nextResult =
            new CampaignMessageDraftProviderResult.Succeeded(
                Body: "Thank you for joining us recently. We look forward to seeing you again.",
                Subject: "Thanks for visiting",
                Channel: "email"
            );

        private Exception? _throwOnDraft;

        private CampaignMessageDraftInput? _lastInput;

        private int _callCount;

        public CampaignMessageDraftInput? LastInput => _lastInput;

        public int CallCount => _callCount;

        public void ResetCallCount()
        {
            _callCount = 0;
            _lastInput = null;
        }

        public void SucceedWith(
            string body,
            string? subject,
            string channel
        )
        {
            _throwOnDraft = null;
            _nextResult = new CampaignMessageDraftProviderResult.Succeeded(
                body,
                subject,
                channel
            );
        }

        public void Fail(bool retryable = true)
        {
            _throwOnDraft = null;
            _nextResult = new CampaignMessageDraftProviderResult.Failed(
                retryable
            );
        }

        public void ThrowOnDraft(Exception? exception = null)
        {
            _throwOnDraft =
                exception ?? new InvalidOperationException("Fake draft boom");
        }

        public Task<CampaignMessageDraftProviderResult> DraftAsync(
            CampaignMessageDraftInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;
            _lastInput = input;

            if (_throwOnDraft is not null)
            {
                throw _throwOnDraft;
            }

            if (_nextResult is CampaignMessageDraftProviderResult.Succeeded succeeded)
            {
                var subject =
                    string.Equals(
                        input.Channel,
                        "sms",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? null
                        : succeeded.Subject;

                return Task.FromResult<CampaignMessageDraftProviderResult>(
                    new CampaignMessageDraftProviderResult.Succeeded(
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
