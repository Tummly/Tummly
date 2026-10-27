using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Interfaces;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class SignupRateLimitEndpointsTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string ForwardedFor = "X-Forwarded-For";
        private const string RateLimitedCode = "rate_limited";
        private const string RateLimitedMessage =
            "Too many attempts. Wait a moment and try again.";

        private const string ClientA = "203.0.113.10";
        private const string ClientB = "203.0.113.11";

        private readonly TummlyWebApplicationFactory _factory;

        public SignupRateLimitEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Each call builds its own host, so every test starts with empty limiter state.
        /// The real production limits (10 per endpoint, 30 combined) are used here.
        /// </summary>
        private WebApplicationFactory<Program> CreateLimitedFactory(int perEndpoint = 10) =>
            _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["SignupRateLimit:PerEndpointPerMinute"] = perEndpoint.ToString(),
                            ["SignupRateLimit:CombinedPerMinute"] = "30",
                            ["SignupRateLimit:TokenEndpointPerMinute"] = "120",
                            ["SignupRateLimit:TrustedProxyHops"] = "1",
                        }
                    )
                );

                builder.ConfigureServices(ConfigureTestServices);
            });

        [Fact]
        public async Task RateLimit_EleventhStartInAMinute_429WithRetryAfter()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 10; i++)
            {
                var allowed = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
            }

            var limited = await PostAsync(client, "/api/signup/start", ClientA, StartBody());

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.TryGetValues("Retry-After", out var retryAfter));
            var seconds = int.Parse(Assert.Single(retryAfter));
            Assert.InRange(seconds, 1, 60);

            using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(RateLimitedCode, body.RootElement.GetProperty("code").GetString());
            Assert.Equal(RateLimitedMessage, body.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task RateLimit_CombinedAcrossEndpoints_31st_429()
        {
            // Per-endpoint budget is raised so only the combined limit can refuse the 31st.
            using var factory = CreateLimitedFactory(perEndpoint: 20);
            using var client = factory.CreateClient();

            for (var i = 0; i < 10; i++)
            {
                await PostAsync(client, "/api/signup/start", ClientA, StartBody());
                await PostAsync(client, "/api/signup/verify-otp", ClientA, new { });
                await PostAsync(client, "/api/signup/resend-otp", ClientA, new { });
            }

            var limited = await PostAsync(client, "/api/signup/start", ClientA, StartBody());

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
            Assert.Equal(RateLimitedCode, body.RootElement.GetProperty("code").GetString());
        }

        [Fact]
        public async Task RateLimit_PartitionsByForwardedClientIp()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            // Every request arrives from the same test host address, so only the
            // forwarded client IP can tell these two users apart.
            for (var i = 0; i < 10; i++)
            {
                var a = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, a.StatusCode);
            }

            for (var i = 0; i < 10; i++)
            {
                var b = await PostAsync(client, "/api/signup/start", ClientB, StartBody());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, b.StatusCode);
            }

            var limitedA = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
            Assert.Equal(HttpStatusCode.TooManyRequests, limitedA.StatusCode);
        }

        [Fact]
        public async Task RateLimit_NonSignupEndpoint_NotLimited()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 40; i++)
            {
                var response = await client.GetAsync("/health");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task RateLimit_PollingProvisioningStatus_30TimesInAMinute_NotLimited()
        {
            // Default limits come from SignupRateLimitSettings, not from test configuration.
            using var factory = CreateDefaultFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 30; i++)
            {
                var poll = await GetAsync(
                    client,
                    "/api/signup/provisioning-status",
                    ClientA,
                    Guid.NewGuid()
                );
                // An unknown token is 401, never 429.
                Assert.Equal(HttpStatusCode.Unauthorized, poll.StatusCode);
            }

            // Token endpoints are not counted in the combined limit: the 30 polls above
            // leave the whole strict budget of this client free.
            for (var i = 0; i < 10; i++)
            {
                var allowed = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
            }

            var limited = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Fact]
        public async Task RateLimit_TokenEndpoint_121stInAMinute_429()
        {
            using var factory = CreateDefaultFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 120; i++)
            {
                var poll = await GetAsync(client, "/api/signup/session", ClientA, Guid.NewGuid());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, poll.StatusCode);
            }

            var limited = await GetAsync(client, "/api/signup/session", ClientA, Guid.NewGuid());

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.Contains("Retry-After"));
            using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
            Assert.Equal(RateLimitedCode, body.RootElement.GetProperty("code").GetString());

            // Another token endpoint has its own bucket.
            var other = await GetAsync(
                client,
                "/api/signup/provisioning-status",
                ClientA,
                Guid.NewGuid()
            );
            Assert.NotEqual(HttpStatusCode.TooManyRequests, other.StatusCode);
        }

        [Fact]
        public async Task RateLimit_SpoofedLeftMostForwardedEntries_ShareOnePartition()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 10; i++)
            {
                var allowed = await PostAsync(
                    client,
                    "/api/signup/start",
                    $"6.6.6.{i}, {ClientA}",
                    StartBody()
                );
                Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
            }

            var limited = await PostAsync(
                client,
                "/api/signup/start",
                "7.7.7.7, " + ClientA,
                StartBody()
            );
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Fact]
        public async Task RateLimit_ForwardedEntryWithPort_ShareOnePartition()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 10; i++)
            {
                var allowed = await PostAsync(
                    client,
                    "/api/signup/start",
                    $"{ClientA}:{40000 + i}",
                    StartBody()
                );
                Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
            }

            var limited = await PostAsync(client, "/api/signup/start", ClientA, StartBody());
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Fact]
        public async Task RateLimit_TrailingSlashPath_ShareOneBucket()
        {
            using var factory = CreateLimitedFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 5; i++)
            {
                await PostAsync(client, "/api/signup/start", ClientA, StartBody());
                await PostAsync(client, "/api/signup/start/", ClientA, StartBody());
            }

            var limited = await PostAsync(client, "/api/signup/start/", ClientA, StartBody());
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        private WebApplicationFactory<Program> CreateDefaultFactory() =>
            _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(ConfigureTestServices)
            );

        private static void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, EmailServiceStubBase>();

            // Behind the platform ingress the app does not trust the forwarded headers
            // (only loopback is trusted), so the middleware leaves X-Forwarded-For intact.
            // The test host has no remote address and would trust it, which hides spoofing.
            services.PostConfigure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.None
            );
        }

        private static Task<HttpResponseMessage> GetAsync(
            HttpClient client,
            string path,
            string forwardedFor,
            Guid sessionToken
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add(ForwardedFor, forwardedFor);
            request.Headers.Add("X-Signup-Session", sessionToken.ToString());
            return client.SendAsync(request);
        }

        private static object StartBody() =>
            new { email = $"rl-{Guid.NewGuid():N}@example.com", termsAccepted = true };

        private static Task<HttpResponseMessage> PostAsync(
            HttpClient client,
            string path,
            string forwardedFor,
            object body
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Add(ForwardedFor, forwardedFor);
            return client.SendAsync(request);
        }
    }
}
