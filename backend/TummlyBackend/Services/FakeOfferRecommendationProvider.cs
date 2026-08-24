using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and CI — no live Azure/OpenAI.
    /// </summary>
    public sealed class FakeOfferRecommendationProvider
        : IOfferRecommendationProvider
    {
        private enum Mode
        {
            DefaultFixtures,
            SucceedWith,
            Fail,
            Throw,
        }

        private Mode _mode = Mode.DefaultFixtures;
        private OfferRecommendationModelOutput? _nextOutput;
        private bool _failRetryable = true;
        private Exception? _throwOnRecommend;
        private OfferRecommendationProviderInput? _lastInput;
        private int _callCount;

        public OfferRecommendationProviderInput? LastInput => _lastInput;

        public int CallCount => _callCount;

        public void ResetCallCount()
        {
            _callCount = 0;
            _lastInput = null;
        }

        public void UseDefaultFixtures()
        {
            _mode = Mode.DefaultFixtures;
            _throwOnRecommend = null;
            _nextOutput = null;
        }

        public void SucceedWith(OfferRecommendationModelOutput output)
        {
            _mode = Mode.SucceedWith;
            _throwOnRecommend = null;
            _nextOutput = output;
        }

        public void Fail(bool retryable = true)
        {
            _mode = Mode.Fail;
            _throwOnRecommend = null;
            _failRetryable = retryable;
            _nextOutput = null;
        }

        public void ThrowOnRecommend(Exception? exception = null)
        {
            _mode = Mode.Throw;
            _throwOnRecommend =
                exception
                ?? new InvalidOperationException("Fake offer recommendation boom");
        }

        public Task<OfferRecommendationProviderResult> RecommendAsync(
            OfferRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;
            _lastInput = input;

            if (_mode == Mode.Throw && _throwOnRecommend is not null)
            {
                throw _throwOnRecommend;
            }

            if (_mode == Mode.Fail)
            {
                return Task.FromResult<OfferRecommendationProviderResult>(
                    new OfferRecommendationProviderResult.Failed(_failRetryable)
                );
            }

            var output =
                _mode == Mode.SucceedWith && _nextOutput is not null
                    ? _nextOutput
                    : FixtureFor(input.SelectedType);

            return Task.FromResult<OfferRecommendationProviderResult>(
                new OfferRecommendationProviderResult.Succeeded(output)
            );
        }

        public static OfferRecommendationModelOutput FixtureFor(string type)
            => type switch
            {
                "promote-this-offer" => new OfferRecommendationModelOutput(
                    Type: "promote-this-offer",
                    Title: "Promote this offer",
                    Opportunity:
                        "Eligible guests have not claimed this offer yet.",
                    WhyBullets:
                    [
                        "Marketing-eligible guests can receive this offer",
                        "No claims in the Default reporting period",
                    ],
                    SuggestedChannel: "email",
                    CampaignName: "Promote this offer",
                    MessageSubject: "A thank-you offer from us",
                    MessageBody: "We would love you to use this offer."
                ),
                "fix-this-offer" => new OfferRecommendationModelOutput(
                    Type: "fix-this-offer",
                    Title: "Fix this offer",
                    Opportunity:
                        "This offer needs attention before you promote it.",
                    WhyBullets:
                    [
                        "Needs attention membership is open for this offer",
                        "Fix validity or Void before promoting",
                    ],
                    SuggestedChannel: null,
                    CampaignName: null,
                    MessageSubject: null,
                    MessageBody: null
                ),
                _ => new OfferRecommendationModelOutput(
                    Type: "none",
                    Title: null,
                    Opportunity: null,
                    WhyBullets: null,
                    SuggestedChannel: null,
                    CampaignName: null,
                    MessageSubject: null,
                    MessageBody: null
                ),
            };
    }
}
