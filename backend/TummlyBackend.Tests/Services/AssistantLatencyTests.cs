using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Assistant latency: retry cap, timing trace, permission memo, and
    /// parallel retrieve tool reads.
    /// </summary>
    public class AssistantLatencyTests
    {
        private static readonly Uri RequestUri =
            new("https://tummly-test.openai.azure.com/openai/deployments/x/chat/completions");

        [Fact]
        public async Task SendAsync_LiveRetryOptions_CapRetriesAndRetryAfterWait()
        {
            var handler = new AlwaysStatusHandler(HttpStatusCode.TooManyRequests, retryAfterSeconds: 30);
            var stats = new AzureOpenAIChatSendStats();
            var stopwatch = Stopwatch.StartNew();

            var result = await AzureOpenAIChatCompletions.SendAsync(
                new HttpClient(handler),
                RequestUri,
                "key",
                "{}",
                CancellationToken.None,
                new AzureOpenAIRetryOptions(1, TimeSpan.FromMilliseconds(50)),
                stats
            );

            stopwatch.Stop();
            var transient = Assert.IsType<AzureOpenAIChatSendResult.Transient>(result);
            Assert.Equal(429, transient.StatusCode);
            Assert.Equal(2, handler.RequestCount);
            Assert.Equal(1, stats.Retries);
            Assert.Equal(429, stats.StatusCode);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task LiveProvider_Always429WithLongRetryAfter_FailsFastAndRecordsTimings()
        {
            var handler = new AlwaysStatusHandler(HttpStatusCode.TooManyRequests, retryAfterSeconds: 30);
            var provider = CreateProvider(handler, new FeedbackClassificationSettings
            {
                Endpoint = "https://tummly-test.openai.azure.com/",
                ApiKey = "test-key",
                AssistantLiveMaxTransientRetries = 1,
                AssistantLiveMaxRetryWaitSeconds = 0,
            });
            var timings = AssistantTurnTimings.Begin();
            var stopwatch = Stopwatch.StartNew();

            var result = await provider.CompleteAsync(LiveInput());

            stopwatch.Stop();
            Assert.IsType<AssistantLiveAnswerResult.Failed>(result);
            Assert.Equal(2, handler.RequestCount);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
            var json = timings.ToJson();
            var call = Assert.Single(json["modelCalls"]!.AsArray());
            Assert.Equal(AssistantTurnTimings.RoundOne, call!["round"]!.GetValue<string>());
            Assert.Equal(429, call["status"]!.GetValue<int>());
            Assert.Equal(1, call["retries"]!.GetValue<int>());
        }

        [Fact]
        public async Task LiveProvider_HttpTimeout_ReturnsRetryableFailureAndDoesNotThrow()
        {
            var provider = CreateProvider(
                new ThrowingHandler(new TaskCanceledException("HttpClient timeout")),
                new FeedbackClassificationSettings
                {
                    Endpoint = "https://tummly-test.openai.azure.com/",
                    ApiKey = "test-key",
                }
            );

            var result = await provider.CompleteAsync(LiveInput());

            var failed = Assert.IsType<AssistantLiveAnswerResult.Failed>(result);
            Assert.True(failed.Retryable);
        }

        [Fact]
        public async Task LiveProvider_CallerCancel_StillThrows()
        {
            var provider = CreateProvider(
                new ThrowingHandler(new TaskCanceledException("caller")),
                new FeedbackClassificationSettings
                {
                    Endpoint = "https://tummly-test.openai.azure.com/",
                    ApiKey = "test-key",
                }
            );
            using var cancel = new CancellationTokenSource();
            cancel.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.CompleteAsync(LiveInput(), cancel.Token)
            );
        }

        [Fact]
        public void Timings_ReadsUsageTokensFromResponse()
        {
            var timings = AssistantTurnTimings.Begin();
            var start = timings.MarkModelCallStart();
            timings.RecordModelCall(
                AssistantTurnTimings.RoundFinal,
                start,
                200,
                0,
                """
                {"usage":{"prompt_tokens":1200,"completion_tokens":300,
                "prompt_tokens_details":{"cached_tokens":1024},
                "completion_tokens_details":{"reasoning_tokens":128}}}
                """
            );
            timings.RecordToolCall("read_offers", 12, "success");

            var json = timings.ToJson();

            Assert.Equal(1200, json["promptTokens"]!.GetValue<int>());
            Assert.Equal(300, json["completionTokens"]!.GetValue<int>());
            Assert.Equal(1024, json["cachedTokens"]!.GetValue<int>());
            Assert.Equal(128, json["reasoningTokens"]!.GetValue<int>());
            Assert.Equal(1, json["modelCallCount"]!.GetValue<int>());
            Assert.Single(json["toolCalls"]!.AsArray());
        }

        [Fact]
        public void Timings_RelabelsTitleCalls()
        {
            var timings = AssistantTurnTimings.Begin();
            timings.RecordModelCall(AssistantTurnTimings.RoundOne, 0, 200, 0, null);
            var from = timings.ModelCallCount;
            timings.RecordModelCall(AssistantTurnTimings.RoundHistoryOnly, 0, 200, 0, null);

            timings.RelabelModelCallsFrom(from, AssistantTurnTimings.RoundTitle);

            var calls = timings.ToJson()["modelCalls"]!.AsArray();
            Assert.Equal("round1", calls[0]!["round"]!.GetValue<string>());
            Assert.Equal("title", calls[1]!["round"]!.GetValue<string>());
        }

        [Theory]
        [InlineData("""{"status":"success","evidence":{}}""", "success")]
        [InlineData("""{"status":"permission_blocked"}""", "permission_blocked")]
        [InlineData("""{"readOnly":true}""", "unknown")]
        public void Timings_ToolStatus_ReadsLeadingStatus(string json, string expected)
            => Assert.Equal(expected, AssistantTurnTimings.ToolStatus(json));

        [Fact]
        public void TurnTrace_WithTimings_AddsTimingsObject()
        {
            var timings = AssistantTurnTimings.Begin();
            timings.RecordToolCall("read_offers", 5, "success");

            var json = AssistantTurnTrace.Serialize(
                new FeedbackClassificationSettings { DeploymentName = "gpt-5-mini" },
                ["read_offers", "read_offers"],
                timings
            );

            using var document = JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("tools").GetArrayLength());
            Assert.Equal(
                1,
                document.RootElement
                    .GetProperty("timings")
                    .GetProperty("toolCalls")
                    .GetArrayLength()
            );
        }

        [Fact]
        public async Task CompareAll_ThreeLocations_ChecksEachPermissionOncePerTurn()
        {
            var fakes = new HostFakes();
            var host = fakes.SequentialHost();
            var context = CompareAllContext(1, 2, 3);

            await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest(
                        "c1",
                        AssistantRetrieveToolCatalog.CompareAllLocations,
                        "{}"
                    ),
                    new AssistantToolCallRequest(
                        "c2",
                        AssistantRetrieveToolCatalog.CompareAllLocations,
                        "{}"
                    ),
                ]
            );

            // Feedback, Offers, Campaigns, Capture, Guests per Location.
            Assert.Equal(3 * 5, fakes.Permissions.LocationChecks);
            Assert.Equal(15, fakes.Permissions.DistinctKeys);
        }

        [Fact]
        public async Task DeniedArea_StillReturnsPermissionBlocked()
        {
            var fakes = new HostFakes();
            fakes.Permissions.Denied.Add((OperatorAreaIds.Offers, 1));
            var host = fakes.SequentialHost();
            var context = SingleLocationContext(1);

            var results = await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest("a", AssistantRetrieveToolCatalog.ReadOffers, "{}"),
                    new AssistantToolCallRequest("b", AssistantRetrieveToolCatalog.ReadOffers, "{}"),
                ]
            );

            Assert.All(
                results,
                result => Assert.Equal(
                    """{"status":"permission_blocked"}""",
                    result.ContentJson
                )
            );
            Assert.Equal(1, fakes.Permissions.LocationChecks);
        }

        [Fact]
        public async Task ParallelHost_DomainBatch_SameResultsAndOrderAsSequential()
        {
            var calls = new List<AssistantToolCallRequest>
            {
                new("g", AssistantRetrieveToolCatalog.ReadGuests, "{}"),
                new("f", AssistantRetrieveToolCatalog.ReadFeedbackSummary, "{}"),
                new("o", AssistantRetrieveToolCatalog.ReadOffers, "{}"),
                new("c", AssistantRetrieveToolCatalog.ReadCampaigns, "{}"),
                new("p", AssistantRetrieveToolCatalog.ReadCapturePerformance, "{}"),
                new("h", AssistantRetrieveToolCatalog.ReadHomeKpis, "{}"),
            };
            var sequentialFakes = new HostFakes();
            var sequentialContext = SingleLocationContext(1);
            var sequential = await sequentialFakes
                .SequentialHost()
                .ExecuteBatchAsync(sequentialContext, calls);
            var parallelFakes = new HostFakes(readDelay: TimeSpan.FromMilliseconds(100));
            using var provider = parallelFakes.ServiceProvider();
            var parallelContext = SingleLocationContext(1);
            var stopwatch = Stopwatch.StartNew();

            var parallel = await parallelFakes
                .ParallelHost(provider)
                .ExecuteBatchAsync(parallelContext, calls);

            stopwatch.Stop();
            Assert.Equal(
                sequential.Select(result => (result.ToolCallId, result.Name, result.ContentJson)),
                parallel.Select(result => (result.ToolCallId, result.Name, result.ContentJson))
            );
            Assert.Equal(sequentialContext.AccumulatedEvidence, parallelContext.AccumulatedEvidence);
            Assert.True(parallelFakes.MaxConcurrentReads > 1);
            Assert.True(parallelFakes.ScopesCreated >= calls.Count);
            // Six 100 ms reads one at a time take at least 600 ms.
            Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(550));
        }

        [Fact]
        public async Task ParallelHost_CompareAll_SameResultsAndLocationOrderAsSequential()
        {
            var call = new AssistantToolCallRequest(
                "all",
                AssistantRetrieveToolCatalog.CompareAllLocations,
                "{}"
            );
            var sequentialContext = CompareAllContext(3, 1, 2, 4);
            var sequential = await new HostFakes()
                .SequentialHost()
                .ExecuteBatchAsync(sequentialContext, [call]);
            var parallelFakes = new HostFakes(readDelay: TimeSpan.FromMilliseconds(30));
            using var provider = parallelFakes.ServiceProvider();
            var parallelContext = CompareAllContext(3, 1, 2, 4);

            var parallel = await parallelFakes
                .ParallelHost(provider)
                .ExecuteBatchAsync(parallelContext, [call]);

            Assert.Equal(sequential[0].ContentJson, parallel[0].ContentJson);
            Assert.Equal(
                [3, 1, 2, 4],
                parallelContext.CompareLocations!.Select(row => row.OwnedLocationId)
            );
            Assert.Equal(sequentialContext.AccumulatedEvidence, parallelContext.AccumulatedEvidence);
            Assert.Empty(parallelContext.NotStartedLocationNames);
            Assert.True(parallelFakes.MaxConcurrentReads > 1);
            Assert.True(parallelFakes.MaxConcurrentReads <= 6);
        }

        [Fact]
        public async Task ParallelHost_CompareAllBudgetSpent_MarksLaterLocationsNotStarted()
        {
            var fakes = new HostFakes();
            using var provider = fakes.ServiceProvider();
            var clock = new SteppingTimeProvider(AssistantCompareAll.RetrieveBudget);
            var host = fakes.ParallelHost(provider, clock);
            var context = CompareAllContext(1, 2, 3, 4, 5);

            await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest(
                        "all",
                        AssistantRetrieveToolCatalog.CompareAllLocations,
                        "{}"
                    ),
                ]
            );

            // The clock moves past the budget after the first reading of it,
            // so no Location starts and every name is listed in input order.
            Assert.Equal(
                ["L1", "L2", "L3", "L4", "L5"],
                context.NotStartedLocationNames
            );
        }

        private static AzureOpenAIAssistantLiveAnswerProvider CreateProvider(
            HttpMessageHandler handler,
            FeedbackClassificationSettings settings
        )
            => new(
                new StubHttpClientFactory(new HttpClient(handler)),
                Options.Create(settings),
                NullLogger<AzureOpenAIAssistantLiveAnswerProvider>.Instance
            );

        private static AssistantLiveAnswerInput LiveInput()
            => new(
                "Summarise recent feedback",
                "Camden",
                "this week",
                AssistantRetrievedEvidence.Empty,
                ExecuteRetrieveTools: (_, _) =>
                    Task.FromResult<IReadOnlyList<AssistantToolCallResult>>([])
            );

        private static AssistantRetrieveToolContext SingleLocationContext(int locationId)
            => new()
            {
                OwnerUserId = 7,
                OwnedLocationId = locationId,
                OwnedLocationName = $"L{locationId}",
                FromUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                ToUtc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                PeriodPhrase = "this month",
                OwnedLocations = [Location(locationId)],
                CompareLocationIds = [locationId],
            };

        private static AssistantRetrieveToolContext CompareAllContext(params int[] locationIds)
            => new()
            {
                OwnerUserId = 7,
                OwnedLocationId = null,
                OwnedLocationName = "All locations",
                FromUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                ToUtc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                PeriodPhrase = "this month",
                OwnedLocations = locationIds.Select(Location).ToList(),
                CompareLocationIds = locationIds,
                CompareAllMode = true,
            };

        private static AssistantOwnedLocationRef Location(int id)
            => new(id, $"L{id}", $"{id} High Street", default);

        /// <summary>
        /// Shared fakes for the tool host. Evidence depends on the Location id,
        /// so a wrong merge order shows up as a different result.
        /// </summary>
        private sealed class HostFakes(TimeSpan readDelay = default)
        {
            private int _inFlight;
            private int _maxInFlight;
            private int _scopes;

            public CountingPermissions Permissions { get; } = new();

            public int MaxConcurrentReads => Volatile.Read(ref _maxInFlight);

            public int ScopesCreated => Volatile.Read(ref _scopes);

            public AssistantRetrieveToolHost SequentialHost()
                => new(
                    new Feedback(this),
                    new Offers(this),
                    new Campaigns(this),
                    new Capture(this),
                    new Home(this),
                    new Guests(this),
                    Permissions
                );

            public ServiceProvider ServiceProvider()
            {
                var services = new ServiceCollection();
                services.AddScoped<IAssistantFeedbackRetrieve>(_ => ScopedRead(new Feedback(this)));
                services.AddScoped<IAssistantOffersRetrieve>(_ => new Offers(this));
                services.AddScoped<IAssistantCampaignsRetrieve>(_ => new Campaigns(this));
                services.AddScoped<IAssistantCaptureRetrieve>(_ => new Capture(this));
                services.AddScoped<IAssistantHomeKpiRetrieve>(_ => new Home(this));
                services.AddScoped<IAssistantGuestsRetrieve>(_ => new Guests(this));
                services.AddScoped<IRestaurantPermissionHelper>(_ => Permissions);
                return services.BuildServiceProvider(validateScopes: true);
            }

            public AssistantRetrieveToolHost ParallelHost(
                ServiceProvider provider,
                TimeProvider? clock = null
            )
                => new(
                    new Feedback(this),
                    new Offers(this),
                    new Campaigns(this),
                    new Capture(this),
                    new Home(this),
                    new Guests(this),
                    Permissions,
                    timeProvider: clock,
                    scopeFactory: provider.GetRequiredService<IServiceScopeFactory>()
                );

            private T ScopedRead<T>(T read)
            {
                // Feedback is resolved once per scope (every leaf read
                // resolves every reader); count scopes from it.
                Interlocked.Increment(ref _scopes);
                return read;
            }

            public async Task ReadAsync(CancellationToken cancellationToken)
            {
                var now = Interlocked.Increment(ref _inFlight);
                int seen;
                while (now > (seen = Volatile.Read(ref _maxInFlight)))
                {
                    if (Interlocked.CompareExchange(ref _maxInFlight, now, seen) == seen)
                    {
                        break;
                    }
                }

                try
                {
                    if (readDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(readDelay, cancellationToken);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _inFlight);
                }
            }
        }

        private sealed class Feedback(HostFakes fakes) : IAssistantFeedbackRetrieve
        {
            public async Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantFeedbackRetrieveResult.Ok(
                    AssistantFeedbackEvidence.Empty with
                    {
                        TotalCount = ownedLocationId * 10,
                        SampleCount = ownedLocationId * 10,
                    }
                );
            }

            public Task<AssistantFeedbackRetrieveResult> RetrieveIdentityAsync(
                int ownedLocationId,
                string locationName,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();
        }

        private sealed class Offers(HostFakes fakes) : IAssistantOffersRetrieve
        {
            public async Task<AssistantOffersRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantOffersRetrieveResult.Ok(AssistantOffersEvidence.Empty);
            }
        }

        private sealed class Campaigns(HostFakes fakes) : IAssistantCampaignsRetrieve
        {
            public async Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                bool includeMessageCopy = false,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantCampaignsRetrieveResult.Ok(AssistantCampaignsEvidence.Empty);
            }
        }

        private sealed class Capture(HostFakes fakes) : IAssistantCaptureRetrieve
        {
            public async Task<AssistantCaptureRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantCaptureRetrieveResult.Ok(AssistantCaptureEvidence.Empty);
            }
        }

        private sealed class Home(HostFakes fakes) : IAssistantHomeKpiRetrieve
        {
            public async Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantHomeKpiRetrieveResult.Ok(AssistantHomeKpiEvidence.Empty);
            }
        }

        private sealed class Guests(HostFakes fakes) : IAssistantGuestsRetrieve
        {
            public async Task<AssistantGuestsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                CancellationToken cancellationToken = default
            )
            {
                await fakes.ReadAsync(cancellationToken);
                return new AssistantGuestsRetrieveResult.Ok(AssistantGuestsEvidence.Empty);
            }
        }

        private sealed class CountingPermissions : IRestaurantPermissionHelper
        {
            private readonly object _gate = new();
            private readonly HashSet<(string, int)> _keys = [];
            private int _locationChecks;

            public HashSet<(string AreaId, int LocationId)> Denied { get; } = [];

            public int LocationChecks => Volatile.Read(ref _locationChecks);

            public int DistinctKeys
            {
                get
                {
                    lock (_gate)
                    {
                        return _keys.Count;
                    }
                }
            }

            public async Task<RestaurantPermissionDecision> AuthorizeLocationForUserAsync(
                int userId,
                string areaId,
                PermissionLevel minimum,
                int locationId
            )
            {
                Interlocked.Increment(ref _locationChecks);
                lock (_gate)
                {
                    _keys.Add((areaId, locationId));
                }

                await Task.Yield();
                return Denied.Contains((areaId, locationId))
                    ? new RestaurantPermissionDecision
                    {
                        Status = RestaurantPermissionStatus.Forbidden,
                    }
                    : RestaurantPermissionDecision.Allow(1);
            }

            public Task<RestaurantPermissionDecision> AuthorizeAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum
            )
                => throw new NotSupportedException();

            public Task<RestaurantPermissionDecision> AuthorizeLocationAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum,
                int locationId
            )
                => throw new NotSupportedException();

            public Task<RestaurantPermissionDecision> AuthorizeLocationSetAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum
            )
                => throw new NotSupportedException();

            public Task<RestaurantPermissionDecision> AuthorizeNamedLocationIdsAsync(
                IReadOnlyList<int> allowedLocationIds,
                int[] namedLocationIds
            )
                => throw new NotSupportedException();

            public Task<RestaurantPermissionDecision> AuthorizeUserAsync(
                int userId,
                string areaId,
                PermissionLevel minimum
            )
                => throw new NotSupportedException();
        }

        /// <summary>Each read of the clock moves it forward by one step.</summary>
        private sealed class SteppingTimeProvider(TimeSpan step) : TimeProvider
        {
            private long _ticks = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero).UtcTicks;

            public override DateTimeOffset GetUtcNow()
                => new(Interlocked.Add(ref _ticks, step.Ticks) - step.Ticks, TimeSpan.Zero);
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        private sealed class AlwaysStatusHandler(HttpStatusCode status, int retryAfterSeconds)
            : HttpMessageHandler
        {
            private int _requestCount;

            public int RequestCount => Volatile.Read(ref _requestCount);

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                Interlocked.Increment(ref _requestCount);
                var response = new HttpResponseMessage(status)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                };
                response.Headers.RetryAfter = new RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(retryAfterSeconds)
                );
                return Task.FromResult(response);
            }
        }

        private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromException<HttpResponseMessage>(exception);
            }
        }
    }
}
