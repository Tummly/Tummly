using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.TrustIndicators;

namespace TummlyBackend.Tests.Services
{
    public sealed class TrustEvidenceLinkCheckerTests
    {
        private const string Start = "https://evidence.example/record/1";

        [Fact]
        public async Task Head200_Ok()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(Respond(HttpStatusCode.OK)));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.True(result.Ok);
            Assert.Equal("HTTP 200", result.Detail);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Head, request.Method);
            Assert.Equal("TummlyTrustEvidenceCheck/1.0", request.UserAgent);
        }

        [Fact]
        public async Task Head405ThenGet200_Ok()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(
                req.Method == HttpMethod.Head
                    ? Respond(HttpStatusCode.MethodNotAllowed)
                    : Respond(HttpStatusCode.OK)));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.True(result.Ok);
            Assert.Equal("HTTP 200", result.Detail);
            Assert.Equal(
                new[] { HttpMethod.Head, HttpMethod.Get },
                handler.Requests.Select(r => r.Method).ToArray()
            );
        }

        [Fact]
        public async Task Get404_Fails_DetailHttp404()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(
                req.Method == HttpMethod.Head
                    ? Respond(HttpStatusCode.Forbidden)
                    : Respond(HttpStatusCode.NotFound)));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.False(result.Ok);
            Assert.Equal("HTTP 404", result.Detail);
        }

        [Fact]
        public async Task Timeout_Fails_DetailTimeout()
        {
            // Waits for the checker's own deadline, the way a stalled host would.
            var handler = new FakeHandler(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return Respond(HttpStatusCode.OK);
            });
            var checker = CheckerFor(handler, timeout: TimeSpan.FromMilliseconds(50));

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.False(result.Ok);
            Assert.Equal("timeout", result.Detail);
        }

        [Fact]
        public async Task CallerCancellation_IsNotReportedAsTimeout()
        {
            using var cts = new CancellationTokenSource();
            var handler = new FakeHandler(async (_, ct) =>
            {
                cts.Cancel();
                await Task.Delay(Timeout.Infinite, ct);
                return Respond(HttpStatusCode.OK);
            });
            var checker = CheckerFor(handler);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => checker.CheckAsync(new Uri(Start), cts.Token));
        }

        [Fact]
        public async Task NetworkError_Fails_DetailNetworkError_WithoutExceptionText()
        {
            var handler = new FakeHandler((_, _) =>
                throw new HttpRequestException("secret host detail"));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.False(result.Ok);
            Assert.Equal("network error", result.Detail);
        }

        [Fact]
        public async Task RedirectToHttp_Fails()
        {
            var handler = new FakeHandler((_, _) => Task.FromResult(
                Respond(HttpStatusCode.MovedPermanently, "http://insecure.example/record/1")));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.False(result.Ok);
            Assert.Equal("redirect to non-https", result.Detail);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task MoreThan5Redirects_Fails()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(
                Respond(HttpStatusCode.Found, NextHop(req))));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.False(result.Ok);
            Assert.Equal("too many redirects", result.Detail);
            // The original request plus five followed hops; the sixth redirect is refused.
            Assert.Equal(6, handler.Requests.Count);
        }

        [Fact]
        public async Task FiveRedirects_ThenOk_Passes()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(
                HopIndex(req) < 5
                    ? Respond(HttpStatusCode.Found, NextHop(req))
                    : Respond(HttpStatusCode.OK)));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.True(result.Ok);
            Assert.Equal("HTTP 200", result.Detail);
        }

        [Fact]
        public async Task RelativeRedirect_ResolvesAgainstCurrentUri()
        {
            var handler = new FakeHandler((req, _) => Task.FromResult(
                req.RequestUri!.AbsolutePath == "/record/1"
                    ? Respond(HttpStatusCode.Found, "/record/final")
                    : Respond(HttpStatusCode.OK)));
            var checker = CheckerFor(handler);

            var result = await checker.CheckAsync(new Uri(Start), CancellationToken.None);

            Assert.True(result.Ok);
            Assert.Equal(
                "https://evidence.example/record/final",
                handler.Requests[1].Url
            );
        }

        [Fact]
        public void Di_ResolvesChecker_WithTheTrustEvidenceClient()
        {
            // Same registration as Program.cs. The web factory swaps the checker for a fake, so this
            // is the only test that proves the container can build the real type.
            var services = new ServiceCollection();
            services.AddLogging();
            services
                .AddHttpClient(TrustEvidenceLinkChecker.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
            services.AddScoped<ITrustEvidenceLinkChecker, TrustEvidenceLinkChecker>();
            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();

            var checker = scope.ServiceProvider.GetRequiredService<ITrustEvidenceLinkChecker>();

            Assert.IsType<TrustEvidenceLinkChecker>(checker);
        }

        /// <summary>Redirect chain position: 0 for the start URL, k for https://evidence.example/hop/k.</summary>
        private static int HopIndex(HttpRequestMessage req)
        {
            var path = req.RequestUri!.AbsolutePath;
            return path.StartsWith("/hop/", StringComparison.Ordinal)
                ? int.Parse(path["/hop/".Length..])
                : 0;
        }

        private static string NextHop(HttpRequestMessage req) =>
            $"https://evidence.example/hop/{HopIndex(req) + 1}";

        private static HttpResponseMessage Respond(HttpStatusCode status, string? location = null)
        {
            var response = new HttpResponseMessage(status);
            if (location != null)
            {
                response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
            }

            return response;
        }

        private static TrustEvidenceLinkChecker CheckerFor(
            FakeHandler handler,
            TimeSpan? timeout = null
        ) => new(
            new FakeClientFactory(new HttpClient(handler)),
            NullLogger<TrustEvidenceLinkChecker>.Instance,
            timeout
        );

        private sealed record RecordedRequest(HttpMethod Method, string Url, string? UserAgent);

        private sealed class FakeHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond
        ) : HttpMessageHandler
        {
            public List<RecordedRequest> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri!.AbsoluteUri,
                    request.Headers.UserAgent.ToString()
                ));
                return respond(request, cancellationToken);
            }
        }

        private sealed class FakeClientFactory(HttpClient client) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                Assert.Equal(TrustEvidenceLinkChecker.HttpClientName, name);
                return client;
            }
        }
    }
}
