using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: per-IP limit on the anonymous guest preference centre endpoints (spec §11).
    /// Only the proof endpoints (session/start and friends) count. Session-gated reads and
    /// writes do not. Each test builds its own host, so every test starts with an empty limiter.
    /// </summary>
    public class PublicGuestPreferencesRateLimitTests
        : IClassFixture<TummlyWebApplicationFactory>,
            IDisposable
    {
        private const string PreferencesPath = "/api/public/preferences";
        private const string SessionStartPath = PreferencesPath + "/session/start";
        private const string RejectedEvent = "guest_preferences.rate_limited";
        private const string SignupRejectedEvent = "signup.rate_limited";

        private readonly CapturingLoggerProvider _logs = new();
        private readonly WebApplicationFactory<Program> _host;
        private readonly HttpClient _client;

        public PublicGuestPreferencesRateLimitTests(TummlyWebApplicationFactory factory)
        {
            _host = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.AddProvider(_logs));
                builder.ConfigureTestServices(services =>
                    GuestPreferencesEndpointTestSupport.ReplaceSenders(
                        services,
                        new GuestPreferencesEndpointTestSupport.CapturingGuestPreferencesEmailService()
                    )
                );
            });
            _client = _host.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _host.Dispose();
        }

        [Fact]
        public async Task SessionReads_DoNotUseUpTheStartBudget_From_OneIp()
        {
            var token = await SeedSessionTokenAsync("ratelimit-reads@example.com");

            // Each of the 25 session reads is more than the 20 per-IP proof requests an hour allows.
            for (var i = 0; i < GuestPreferencesRateLimit.PermitLimitPerIpPerHour + 5; i++)
            {
                using var read = new HttpRequestMessage(HttpMethod.Get, PreferencesPath);
                read.Headers.Add(GuestPreferencesSessionToken.HeaderName, token);

                var response = await _client.SendAsync(read);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // None of those reads counted, so the full budget of 20 starts is still there.
            for (var i = 0; i < GuestPreferencesRateLimit.PermitLimitPerIpPerHour; i++)
            {
                var start = await PostStartAsync(RandomEmail());

                Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
            }
        }

        [Fact]
        public async Task SessionStart_TwentyFirstFromOneIp_429()
        {
            for (var i = 0; i < GuestPreferencesRateLimit.PermitLimitPerIpPerHour; i++)
            {
                var allowed = await PostStartAsync(RandomEmail());
                Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
            }

            var limited = await PostStartAsync(RandomEmail());

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.Contains("Retry-After"));
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(limited);
            Assert.Equal("rate_limited", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task SessionStart_Rejected_LogsGuestPreferencesEvent_NotSignup()
        {
            for (var i = 0; i < GuestPreferencesRateLimit.PermitLimitPerIpPerHour; i++)
            {
                await PostStartAsync(RandomEmail());
            }

            var limited = await PostStartAsync(RandomEmail());

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            var messages = _logs.Messages;
            Assert.Contains(messages, message => message.StartsWith(RejectedEvent, StringComparison.Ordinal));
            Assert.DoesNotContain(messages, message => message.StartsWith(SignupRejectedEvent, StringComparison.Ordinal));
        }

        private Task<HttpResponseMessage> PostStartAsync(string contact) =>
            _client.PostAsJsonAsync(SessionStartPath, new { contact });

        private static string RandomEmail() => $"ratelimit-{Guid.NewGuid():N}@example.com";

        /// <summary>A session token for a real Tummly Guest, built with the test host's secret.</summary>
        private async Task<string> SeedSessionTokenAsync(string email)
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<ITummlyGuestIdentityService>();

            var proven = await identity.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                email,
                CancellationToken.None
            );
            await GuestPreferencesEndpointTestSupport.SeedVenueAsync(
                context,
                email,
                $"RateLimit {email}",
                $"RateLimit {email} Main"
            );

            return GuestPreferencesSessionToken.Create(
                proven.TummlyGuestId,
                proven.ContactId,
                DateTime.UtcNow,
                GuestPreferencesEndpointTestSupport.SessionSecret(_host.Services)
            );
        }

        /// <summary>Keeps the formatted text of each log line, so tests can match an event name.</summary>
        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            private readonly object _lock = new();
            private readonly List<string> _messages = new();

            public IReadOnlyList<string> Messages
            {
                get
                {
                    lock (_lock)
                    {
                        return _messages.ToList();
                    }
                }
            }

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

            public void Dispose()
            {
            }

            private void Add(string message)
            {
                lock (_lock)
                {
                    _messages.Add(message);
                }
            }

            private sealed class CapturingLogger : ILogger
            {
                private readonly CapturingLoggerProvider _owner;

                public CapturingLogger(CapturingLoggerProvider owner) => _owner = owner;

                public IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter
                ) => _owner.Add(formatter(state, exception));
            }
        }
    }
}
