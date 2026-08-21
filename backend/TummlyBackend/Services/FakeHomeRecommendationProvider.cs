using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and CI — no live Azure/OpenAI.
    /// Default mode returns deterministic fixtures per SelectedType.
    /// </summary>
    public sealed class FakeHomeRecommendationProvider
        : IHomeRecommendationProvider
    {
        private enum Mode
        {
            DefaultFixtures,
            SucceedWith,
            Fail,
            Throw,
        }

        private Mode _mode = Mode.DefaultFixtures;
        private HomeRecommendationModelOutput? _nextOutput;
        private bool _failRetryable = true;
        private Exception? _throwOnRecommend;
        private HomeRecommendationProviderInput? _lastInput;
        private int _callCount;

        public HomeRecommendationProviderInput? LastInput => _lastInput;

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

        public void SucceedWith(HomeRecommendationModelOutput output)
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
                ?? new InvalidOperationException("Fake home recommendation boom");
        }

        public Task<HomeRecommendationProviderResult> RecommendAsync(
            HomeRecommendationProviderInput input,
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
                return Task.FromResult<HomeRecommendationProviderResult>(
                    new HomeRecommendationProviderResult.Failed(_failRetryable)
                );
            }

            var output =
                _mode == Mode.SucceedWith && _nextOutput is not null
                    ? _nextOutput
                    : FixtureFor(input.SelectedType);

            return Task.FromResult<HomeRecommendationProviderResult>(
                new HomeRecommendationProviderResult.Succeeded(output)
            );
        }

        /// <summary>
        /// Deterministic CI fixtures keyed by Home-native allow-list type.
        /// </summary>
        public static HomeRecommendationModelOutput FixtureFor(string type)
            => type switch
            {
                "review-open-feedback" => new HomeRecommendationModelOutput(
                    Type: "review-open-feedback",
                    Title: "Review open feedback",
                    Opportunity:
                        "Guests left feedback that still needs a response.",
                    WhyBullets:
                    [
                        "Open feedback is waiting in the inbox",
                        "Responding quickly keeps guests engaged",
                    ],
                    Action: new HomeRecommendationDomainActionOutput(
                        Kind: "open-feedback",
                        FeedbackId: null,
                        LocationGuestId: null,
                        OfferId: null
                    )
                ),
                "thank-or-follow-guest" => new HomeRecommendationModelOutput(
                    Type: "thank-or-follow-guest",
                    Title: "Thank or follow a guest",
                    Opportunity:
                        "New guests joined recently and may appreciate a quick thank-you.",
                    WhyBullets:
                    [
                        "Guests joined in this window",
                        "A personal follow-up builds loyalty",
                    ],
                    Action: new HomeRecommendationDomainActionOutput(
                        Kind: "open-guest",
                        FeedbackId: null,
                        LocationGuestId: null,
                        OfferId: null
                    )
                ),
                "promote-or-fix-offer" => new HomeRecommendationModelOutput(
                    Type: "promote-or-fix-offer",
                    Title: "Promote or fix an offer",
                    Opportunity:
                        "Offer health needs attention or no Active offer is live.",
                    WhyBullets:
                    [
                        "Offers drive return visits",
                        "Fixing health issues keeps redemptions flowing",
                    ],
                    Action: new HomeRecommendationDomainActionOutput(
                        Kind: "open-offer",
                        FeedbackId: null,
                        LocationGuestId: null,
                        OfferId: null
                    )
                ),
                _ => new HomeRecommendationModelOutput(
                    Type: "none",
                    Title: null,
                    Opportunity: null,
                    WhyBullets: null,
                    Action: null
                ),
            };
    }
}
