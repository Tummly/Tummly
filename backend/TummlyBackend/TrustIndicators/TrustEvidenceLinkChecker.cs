using System.Net;
using Microsoft.Extensions.Logging;

namespace TummlyBackend.TrustIndicators
{
    /// <summary>Outcome of one evidence link check. Detail never carries exception text.</summary>
    public sealed record TrustEvidenceLinkResult(bool Ok, string Detail);

    /// <summary>CONTRACT §5 item 6: confirms a public evidence link answers before a publish.</summary>
    public interface ITrustEvidenceLinkChecker
    {
        Task<TrustEvidenceLinkResult> CheckAsync(Uri uri, CancellationToken cancellationToken);
    }

    /// <summary>
    /// HEAD first, GET when HEAD returns 405 or 403. Redirects are followed by hand so every hop is
    /// checked for https. One 10 second budget covers the whole chain. The body is never read.
    /// </summary>
    public sealed class TrustEvidenceLinkChecker : ITrustEvidenceLinkChecker
    {
        public const string HttpClientName = "trust-evidence";

        public const string UserAgent = "TummlyTrustEvidenceCheck/1.0";

        private const int MaxRedirects = 5;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TrustEvidenceLinkChecker> _logger;
        private readonly TimeSpan _timeout;

        public TrustEvidenceLinkChecker(
            IHttpClientFactory httpClientFactory,
            ILogger<TrustEvidenceLinkChecker> logger,
            TimeSpan? timeout = null
        )
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _timeout = timeout ?? DefaultTimeout;
        }

        public async Task<TrustEvidenceLinkResult> CheckAsync(
            Uri uri,
            CancellationToken cancellationToken
        )
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budget.CancelAfter(_timeout);
            var client = _httpClientFactory.CreateClient(HttpClientName);

            try
            {
                return await CheckChainAsync(client, uri, budget.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Trust evidence link timed out: {Url}", uri);
                return new TrustEvidenceLinkResult(false, "timeout");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Trust evidence link check failed: {Url}", uri);
                return new TrustEvidenceLinkResult(false, "network error");
            }
        }

        private static async Task<TrustEvidenceLinkResult> CheckChainAsync(
            HttpClient client,
            Uri start,
            CancellationToken cancellationToken
        )
        {
            var current = start;
            for (var redirects = 0; ; redirects++)
            {
                if (!IsHttps(current))
                {
                    return new TrustEvidenceLinkResult(false, "redirect to non-https");
                }

                using var response = await SendWithFallbackAsync(client, current, cancellationToken);
                var status = response.StatusCode;
                if (IsRedirect(status) && response.Headers.Location is { } location)
                {
                    if (redirects == MaxRedirects)
                    {
                        return new TrustEvidenceLinkResult(false, "too many redirects");
                    }

                    // A relative Location resolves against the URI that produced it.
                    current = new Uri(current, location);
                    continue;
                }

                var detail = $"HTTP {(int)status}";
                return new TrustEvidenceLinkResult(response.IsSuccessStatusCode, detail);
            }
        }

        private static async Task<HttpResponseMessage> SendWithFallbackAsync(
            HttpClient client,
            Uri uri,
            CancellationToken cancellationToken
        )
        {
            var head = await SendAsync(client, HttpMethod.Head, uri, cancellationToken);
            if (head.StatusCode is not (HttpStatusCode.MethodNotAllowed or HttpStatusCode.Forbidden))
            {
                return head;
            }

            head.Dispose();
            return await SendAsync(client, HttpMethod.Get, uri, cancellationToken);
        }

        private static async Task<HttpResponseMessage> SendAsync(
            HttpClient client,
            HttpMethod method,
            Uri uri,
            CancellationToken cancellationToken
        )
        {
            using var request = new HttpRequestMessage(method, uri);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            return await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
        }

        private static bool IsRedirect(HttpStatusCode status) =>
            status is HttpStatusCode.MovedPermanently
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect;

        private static bool IsHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttps;
    }
}
