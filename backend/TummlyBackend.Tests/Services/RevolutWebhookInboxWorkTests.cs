using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class RevolutWebhookInboxWorkTests
    {
        private const string SigningSecret = "test-webhook-signing-secret";
        private static readonly DateTime Now = new(
            2026,
            9,
            10,
            12,
            0,
            0,
            DateTimeKind.Utc
        );

        [Fact]
        public async Task Receive_StoresBeforeProcessing_ThenWorkerCompletes()
        {
            await using var provider = BuildProvider(
                RevolutWebhookHandleStatus.Accepted
            );
            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_durable"}""";

            await using (var scope = provider.CreateAsyncScope())
            {
                var receiver = scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookReceiver>();
                using var requestLifetime = new CancellationTokenSource();
                var result = await receiver.ReceiveAsync(
                    body,
                    Sign(body),
                    "1710000000",
                    requestLifetime.Token
                );
                requestLifetime.Cancel();

                Assert.Equal(
                    RevolutWebhookHandleStatus.Accepted,
                    result.Status
                );
            }

            var processor = provider.GetRequiredService<RecordingProcessor>();
            Assert.Empty(processor.Calls);

            await provider
                .GetRequiredService<IRevolutWebhookInboxWork>()
                .DrainAsync(CancellationToken.None);

            Assert.Equal([body], processor.Calls);
            await using var assertScope = provider.CreateAsyncScope();
            var context = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var item = await context.RevolutWebhookInboxItems.SingleAsync();
            Assert.Equal(RevolutWebhookInboxStatuses.Completed, item.Status);
            Assert.Equal(1, item.AttemptCount);
            Assert.Equal(Now, item.CompletedAtUtc);
        }

        [Fact]
        public async Task Receive_DuplicatePayload_IsStoredOnce()
        {
            await using var provider = BuildProvider(
                RevolutWebhookHandleStatus.Accepted
            );
            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_duplicate"}""";

            RevolutWebhookHandleResult first;
            RevolutWebhookHandleResult second;
            await using (var scope = provider.CreateAsyncScope())
            {
                var receiver = scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookReceiver>();
                first = await receiver.ReceiveAsync(
                    body,
                    Sign(body),
                    "1710000000"
                );
                second = await receiver.ReceiveAsync(
                    body,
                    Sign(body),
                    "1710000000"
                );
            }

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, first.Status);
            Assert.Equal(RevolutWebhookHandleStatus.Replay, second.Status);
            await using var assertScope = provider.CreateAsyncScope();
            var context = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.RevolutWebhookInboxItems.CountAsync());
        }

        [Fact]
        public async Task Worker_RetryLater_LeavesItemPendingWithBackoff()
        {
            await using var provider = BuildProvider(
                RevolutWebhookHandleStatus.RetryLater
            );
            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_retry"}""";

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookReceiver>()
                    .ReceiveAsync(body, Sign(body), "1710000000");
            }

            await provider
                .GetRequiredService<IRevolutWebhookInboxWork>()
                .DrainAsync(CancellationToken.None);

            await using var assertScope = provider.CreateAsyncScope();
            var context = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var item = await context.RevolutWebhookInboxItems.SingleAsync();
            Assert.Equal(RevolutWebhookInboxStatuses.Pending, item.Status);
            Assert.Null(item.ClaimedAtUtc);
            Assert.Equal(Now.AddSeconds(5), item.RetryAfterUtc);
            Assert.Equal("processor_status:RetryLater", item.LastError);
        }

        private static ServiceProvider BuildProvider(
            RevolutWebhookHandleStatus processorStatus
        )
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
            );
            services.Configure<RevolutSettings>(settings =>
                settings.WebhookSigningSecret = SigningSecret
            );
            services.AddSingleton<TimeProvider>(
                new FixedTimeProvider(Now)
            );
            services.AddSingleton<IHostEnvironment>(
                new TestHostEnvironment("Production")
            );
            services.AddSingleton(
                new RecordingProcessor(processorStatus)
            );
            services.AddScoped<IRevolutWebhookService>(services =>
                services.GetRequiredService<RecordingProcessor>()
            );
            services.AddSingleton<
                IRevolutWebhookInboxWork,
                RevolutWebhookInboxWork
            >();
            services.AddScoped<
                IRevolutWebhookReceiver,
                RevolutWebhookReceiver
            >();
            return services.BuildServiceProvider();
        }

        private static string Sign(string body) =>
            RevolutWebhookSignature.SignForTests(
                SigningSecret,
                "1710000000",
                body
            );

        private sealed class RecordingProcessor : IRevolutWebhookService
        {
            private readonly RevolutWebhookHandleStatus _status;

            public RecordingProcessor(RevolutWebhookHandleStatus status)
            {
                _status = status;
            }

            public List<string> Calls { get; } = [];

            public Task<RevolutWebhookHandleResult> HandleAsync(
                string rawBody,
                string? signatureHeader,
                string? requestTimestamp,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<RevolutWebhookHandleResult> ProcessVerifiedAsync(
                string rawBody,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(rawBody);
                return Task.FromResult(
                    new RevolutWebhookHandleResult(_status)
                );
            }
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTime now)
            {
                _now = new DateTimeOffset(now, TimeSpan.Zero);
            }

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}
