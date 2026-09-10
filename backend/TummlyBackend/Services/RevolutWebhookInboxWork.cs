using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RevolutWebhookInboxWork : IRevolutWebhookInboxWork
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _environment;
        private readonly TimeProvider _clock;
        private readonly ILogger<RevolutWebhookInboxWork> _logger;
        private readonly Channel<Guid> _wake = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            }
        );

        public RevolutWebhookInboxWork(
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment,
            TimeProvider clock,
            ILogger<RevolutWebhookInboxWork> logger
        )
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _clock = clock;
            _logger = logger;
        }

        public ValueTask NotifyAsync(
            Guid inboxItemId,
            CancellationToken cancellationToken = default
        )
        {
            _wake.Writer.TryWrite(inboxItemId);
            return ValueTask.CompletedTask;
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            if (_environment.IsEnvironment("Testing"))
            {
                return;
            }

            await DrainAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delayTask = Task.Delay(SweepInterval, stoppingToken);
                    var wakeTask = _wake.Reader
                        .WaitToReadAsync(stoppingToken)
                        .AsTask();
                    await Task.WhenAny(delayTask, wakeTask);
                    await DrainAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested
                )
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Revolut webhook inbox loop failed");
                }
            }
        }

        public async Task DrainAsync(
            CancellationToken cancellationToken = default
        )
        {
            while (_wake.Reader.TryRead(out var inboxItemId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ClaimAndProcessGuardedAsync(
                    inboxItemId,
                    cancellationToken
                );
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var worked = await ClaimAndProcessGuardedAsync(
                    exactInboxItemId: null,
                    cancellationToken
                );
                if (!worked)
                {
                    break;
                }
            }
        }

        private async Task<bool> ClaimAndProcessGuardedAsync(
            Guid? exactInboxItemId,
            CancellationToken cancellationToken
        )
        {
            var item = await TryClaimAsync(
                exactInboxItemId,
                cancellationToken
            );
            if (item == null)
            {
                return false;
            }

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookService>();
                var result = await processor.ProcessVerifiedAsync(
                    item.RawBody,
                    cancellationToken
                );

                if (
                    result.Status == RevolutWebhookHandleStatus.Accepted
                    || result.Status == RevolutWebhookHandleStatus.Replay
                )
                {
                    await MarkCompletedAsync(item, cancellationToken);
                }
                else
                {
                    await ScheduleRetryAsync(
                        item,
                        $"processor_status:{result.Status}",
                        cancellationToken
                    );
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested
            )
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Revolut webhook inbox item {InboxItemId} failed on attempt {AttemptCount}",
                    item.Id,
                    item.AttemptCount
                );
                await ScheduleRetryAsync(
                    item,
                    ex.ToString(),
                    cancellationToken
                );
            }

            return true;
        }

        private async Task<ClaimedInboxItem?> TryClaimAsync(
            Guid? exactInboxItemId,
            CancellationToken cancellationToken
        )
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = _clock.GetUtcNow().UtcDateTime;
            var leaseCutoff = now.Subtract(ClaimLease);

            var candidates = context.RevolutWebhookInboxItems
                .AsNoTracking()
                .Where(row =>
                    row.Status == RevolutWebhookInboxStatuses.Pending
                    && (
                        row.ClaimedAtUtc == null
                        || row.ClaimedAtUtc < leaseCutoff
                    )
                    && (
                        row.RetryAfterUtc == null
                        || row.RetryAfterUtc <= now
                    )
                );
            if (exactInboxItemId != null)
            {
                candidates = candidates.Where(row =>
                    row.Id == exactInboxItemId.Value
                );
            }

            var candidateId = await candidates
                .OrderBy(row => row.CreatedAtUtc)
                .Select(row => (Guid?)row.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (candidateId == null)
            {
                return null;
            }

            try
            {
                var updated = await context.RevolutWebhookInboxItems
                    .Where(row =>
                        row.Id == candidateId.Value
                        && row.Status == RevolutWebhookInboxStatuses.Pending
                        && (
                            row.ClaimedAtUtc == null
                            || row.ClaimedAtUtc < leaseCutoff
                        )
                        && (
                            row.RetryAfterUtc == null
                            || row.RetryAfterUtc <= now
                        )
                    )
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.ClaimedAtUtc, now)
                            .SetProperty(
                                row => row.AttemptCount,
                                row => row.AttemptCount + 1
                            ),
                        cancellationToken
                    );
                if (updated == 0)
                {
                    return null;
                }

                context.ChangeTracker.Clear();
                var claimed = await context.RevolutWebhookInboxItems
                    .AsNoTracking()
                    .SingleAsync(
                        row => row.Id == candidateId.Value,
                        cancellationToken
                    );
                return ToClaimed(claimed, now);
            }
            catch (InvalidOperationException)
            {
                // The EF InMemory provider does not support ExecuteUpdate.
                context.ChangeTracker.Clear();
            }

            var tracked = await context.RevolutWebhookInboxItems
                .SingleOrDefaultAsync(
                    row => row.Id == candidateId.Value,
                    cancellationToken
                );
            if (
                tracked == null
                || tracked.Status != RevolutWebhookInboxStatuses.Pending
                || (
                    tracked.ClaimedAtUtc is DateTime held
                    && held >= leaseCutoff
                )
                || (
                    tracked.RetryAfterUtc is DateTime retryAfter
                    && retryAfter > now
                )
            )
            {
                return null;
            }

            tracked.ClaimedAtUtc = now;
            tracked.AttemptCount += 1;
            await context.SaveChangesAsync(cancellationToken);
            return ToClaimed(tracked, now);
        }

        private async Task MarkCompletedAsync(
            ClaimedInboxItem item,
            CancellationToken cancellationToken
        )
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = await context.RevolutWebhookInboxItems
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == item.Id
                        && candidate.Status
                            == RevolutWebhookInboxStatuses.Pending
                        && candidate.ClaimedAtUtc == item.ClaimedAtUtc,
                    cancellationToken
                );
            if (row == null)
            {
                return;
            }

            row.Status = RevolutWebhookInboxStatuses.Completed;
            row.CompletedAtUtc = _clock.GetUtcNow().UtcDateTime;
            row.ClaimedAtUtc = null;
            row.RetryAfterUtc = null;
            row.LastError = null;
            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task ScheduleRetryAsync(
            ClaimedInboxItem item,
            string error,
            CancellationToken cancellationToken
        )
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = await context.RevolutWebhookInboxItems
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == item.Id
                        && candidate.Status
                            == RevolutWebhookInboxStatuses.Pending
                        && candidate.ClaimedAtUtc == item.ClaimedAtUtc,
                    cancellationToken
                );
            if (row == null)
            {
                return;
            }

            var exponent = Math.Clamp(item.AttemptCount - 1, 0, 6);
            var delaySeconds = Math.Min(300, 5 * (1 << exponent));
            row.ClaimedAtUtc = null;
            row.RetryAfterUtc = _clock
                .GetUtcNow()
                .UtcDateTime
                .AddSeconds(delaySeconds);
            row.LastError = error.Length <= 2000 ? error : error[..2000];
            await context.SaveChangesAsync(cancellationToken);
        }

        private static ClaimedInboxItem ToClaimed(
            RevolutWebhookInboxItem item,
            DateTime claimedAtUtc
        ) => new(
            item.Id,
            item.RawBody,
            item.AttemptCount,
            claimedAtUtc
        );

        private sealed record ClaimedInboxItem(
            Guid Id,
            string RawBody,
            int AttemptCount,
            DateTime ClaimedAtUtc
        );
    }
}
