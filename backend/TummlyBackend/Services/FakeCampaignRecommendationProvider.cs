using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and local demos — no live Azure/OpenAI.
    /// </summary>
    public sealed class FakeCampaignRecommendationProvider
        : ICampaignRecommendationProvider
    {
        private CampaignRecommendationProviderResult _nextResult =
            new CampaignRecommendationProviderResult.Succeeded(
                new CampaignRecommendationModelOutput(
                    Type: "none",
                    Title: null,
                    Opportunity: null,
                    EligibleAudience: null,
                    WhyBullets: null,
                    SuggestedChannel: null,
                    EstimatedUsage: null,
                    DraftPrefill: null
                )
            );

        private Exception? _throwOnRecommend;

        private CampaignRecommendationProviderInput? _lastInput;

        private int _callCount;

        public CampaignRecommendationProviderInput? LastInput => _lastInput;

        public int CallCount => _callCount;

        public void ResetCallCount()
        {
            _callCount = 0;
            _lastInput = null;
        }

        public void SucceedWith(CampaignRecommendationModelOutput output)
        {
            _throwOnRecommend = null;
            _nextResult = new CampaignRecommendationProviderResult.Succeeded(
                output
            );
        }

        public void Fail(bool retryable = true)
        {
            _throwOnRecommend = null;
            _nextResult = new CampaignRecommendationProviderResult.Failed(
                retryable
            );
        }

        public void ThrowOnRecommend(Exception? exception = null)
        {
            _throwOnRecommend =
                exception
                ?? new InvalidOperationException("Fake recommendation boom");
        }

        public Task<CampaignRecommendationProviderResult> RecommendAsync(
            CampaignRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;
            _lastInput = input;

            if (_throwOnRecommend is not null)
            {
                throw _throwOnRecommend;
            }

            return Task.FromResult(_nextResult);
        }
    }
}
