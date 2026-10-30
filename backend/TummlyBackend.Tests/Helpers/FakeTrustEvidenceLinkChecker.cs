using TummlyBackend.TrustIndicators;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>Answers Ok unless a test sets a result for the URL. Records every URL checked. No network.</summary>
    public sealed class FakeTrustEvidenceLinkChecker : ITrustEvidenceLinkChecker
    {
        public Dictionary<string, TrustEvidenceLinkResult> Results { get; } = new();

        public List<string> CheckedUrls { get; } = [];

        public Task<TrustEvidenceLinkResult> CheckAsync(Uri uri, CancellationToken cancellationToken)
        {
            CheckedUrls.Add(uri.AbsoluteUri);
            return Task.FromResult(
                Results.TryGetValue(uri.AbsoluteUri, out var result)
                    ? result
                    : new TrustEvidenceLinkResult(true, "HTTP 200")
            );
        }
    }
}
