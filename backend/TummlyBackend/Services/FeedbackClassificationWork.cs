using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Durable AI classification work: Pending rows are the queue; Channel is wake-only (ADR-0010).
    /// </summary>
    public sealed class FeedbackClassificationWork : IFeedbackClassificationWork
    {
        private sealed record ClassificationScope(
            ApplicationDbContext Context,
            IFeedbackClassificationProvider Provider,
            IFeedbackHomeRealtimePublisher Realtime
        );

        private readonly Channel<int> _wake =
            Channel.CreateUnbounded<int>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                }
            );

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<FeedbackClassificationSettings> _settings;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<FeedbackClassificationWork> _logger;

        public FeedbackClassificationWork(
            IServiceScopeFactory scopeFactory,
            IOptions<FeedbackClassificationSettings> settings,
            IHostEnvironment environment,
            ILogger<FeedbackClassificationWork> logger
        )
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _environment = environment;
            _logger = logger;
        }

        public ValueTask NotifyAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                if (!_wake.Writer.TryWrite(feedbackId))
                {
                    _logger.LogWarning(
                        "Classification wake dropped for Feedback {FeedbackId}",
                        feedbackId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Classification wake failed for Feedback {FeedbackId}",
                    feedbackId
                );
            }

            return ValueTask.CompletedTask;
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            if (_environment.IsEnvironment("Testing"))
            {
                return;
            }

            await DrainAsync(stoppingToken);

            var sweep = SweepInterval;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delayTask = Task.Delay(sweep, stoppingToken);
                    var wakeTask = _wake.Reader
                        .WaitToReadAsync(stoppingToken)
                        .AsTask();

                    await Task.WhenAny(delayTask, wakeTask);
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested
                )
                {
                    break;
                }

                await DrainAsync(stoppingToken);
            }
        }

        public async Task DrainAsync(
            CancellationToken cancellationToken = default
        )
        {
            await ReopenDueRetryableFailedAsync(cancellationToken);

            var parallelism = Math.Max(
                1,
                _settings.Value.MaxDegreeOfParallelism
            );

            await RunParallelUnitsAsync(
                DrainWakeHints().Select(id => (int?)id),
                parallelism,
                cancellationToken
            );

            // Sweep until a full parallel wave finds no claimable work.
            while (!cancellationToken.IsCancellationRequested)
            {
                var wave = Enumerable
                    .Range(0, parallelism)
                    .Select(_ =>
                        ClaimAndClassifyGuardedAsync(
                            exactFeedbackId: null,
                            cancellationToken
                        )
                    )
                    .ToArray();

                var results = await Task.WhenAll(wave);
                if (results.All(worked => !worked))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// ADR-0012: flip due retryable Failed → Pending, reset claims, publish.
        /// </summary>
        private async Task ReopenDueRetryableFailedAsync(
            CancellationToken cancellationToken
        )
        {
            using var scope = _scopeFactory.CreateScope();
            var deps = ResolveScope(scope.ServiceProvider);
            var settings = _settings.Value;
            var now = DateTime.UtcNow;
            var maxReopens = Math.Max(1, settings.MaxDelayedReopens);

            var dueIds = await deps.Context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Failed
                )
                .Where(f => f.ClassificationRetryable)
                .Where(f => f.ClassificationRetryAfter != null
                    && f.ClassificationRetryAfter <= now)
                .Where(f => f.ClassificationDelayedReopenCount < maxReopens)
                .OrderBy(f => f.ClassificationRetryAfter)
                .ThenBy(f => f.Id)
                .Select(f => f.Id)
                .Take(32)
                .ToListAsync(cancellationToken);

            foreach (var feedbackId in dueIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = await deps.Context.Feedbacks
                    .FirstOrDefaultAsync(
                        f => f.Id == feedbackId,
                        cancellationToken
                    );

                if (
                    row is null
                    || row.ClassificationStatus != ClassificationStatus.Failed
                    || !FeedbackClassificationDelayedRequeue.CanReopen(
                        row.ClassificationRetryable,
                        row.ClassificationRetryAfter,
                        row.ClassificationDelayedReopenCount,
                        maxReopens,
                        now
                    )
                )
                {
                    continue;
                }

                row.ClassificationStatus = ClassificationStatus.Pending;
                row.Sentiment = null;
                row.DetectedTagsJson = null;
                row.ClassificationClaimedAt = null;
                row.ClassificationClaimAttempts = 0;
                row.ClassificationDelayedReopenCount += 1;
                row.ClassificationRetryable = false;
                row.ClassificationRetryAfter = null;
                await deps.Context.SaveChangesAsync(cancellationToken);

                await PublishTerminalBestEffortAsync(
                    deps.Context,
                    deps.Realtime,
                    row,
                    cancellationToken
                );
            }
        }

        private async Task RunParallelUnitsAsync(
            IEnumerable<int?> exactFeedbackIds,
            int parallelism,
            CancellationToken cancellationToken
        )
        {
            using var gate = new SemaphoreSlim(parallelism, parallelism);
            var tasks = exactFeedbackIds.Select(async exactId =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    await ClaimAndClassifyGuardedAsync(
                        exactId,
                        cancellationToken
                    );
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<bool> ClaimAndClassifyGuardedAsync(
            int? exactFeedbackId,
            CancellationToken cancellationToken
        )
        {
            try
            {
                return await ClaimAndClassifyInScopeAsync(
                    exactFeedbackId,
                    cancellationToken
                );
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested
            )
            {
                return false;
            }
            catch (Exception ex)
            {
                // Persist could not complete — Pending + claim remain for lease
                // reclaim / MaxClaimAttempts (ADR-0010). Do not invent Failed here.
                _logger.LogError(
                    ex,
                    "Classification pass aborted for Feedback {FeedbackId} — leaving Pending for reclaim",
                    exactFeedbackId ?? 0
                );
                return false;
            }
        }

        /// <summary>
        /// Soft-claim (atomic) then classify in one scope.
        /// Returns false when there was no claimable work for this call.
        /// </summary>
        private async Task<bool> ClaimAndClassifyInScopeAsync(
            int? exactFeedbackId,
            CancellationToken cancellationToken
        )
        {
            using var scope = _scopeFactory.CreateScope();
            var deps = ResolveScope(scope.ServiceProvider);
            var settings = _settings.Value;

            var feedback = await TryClaimAtomicAsync(
                deps.Context,
                deps.Realtime,
                settings,
                exactFeedbackId,
                cancellationToken
            );

            if (feedback is null)
            {
                return false;
            }

            var claimStamp = feedback.ClassificationClaimedAt;
            await ClassifyAndPersistAsync(
                deps,
                feedback,
                claimStamp!.Value,
                cancellationToken
            );
            return true;
        }

        private static ClassificationScope ResolveScope(
            IServiceProvider services
        )
            => new(
                services.GetRequiredService<ApplicationDbContext>(),
                services.GetRequiredService<IFeedbackClassificationProvider>(),
                services.GetRequiredService<IFeedbackHomeRealtimePublisher>()
            );

        /// <summary>
        /// Conditional soft-claim via ExecuteUpdate so parallel workers cannot
        /// both own the same Pending row.
        /// </summary>
        private async Task<Feedback?> TryClaimAtomicAsync(
            ApplicationDbContext context,
            IFeedbackHomeRealtimePublisher realtime,
            FeedbackClassificationSettings settings,
            int? exactFeedbackId,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var leaseCutoff = now - TimeSpan.FromMinutes(
                Math.Max(1, settings.ClaimLeaseMinutes)
            );
            var maxClaims = Math.Max(1, settings.MaxClaimAttempts);
            var attempts = 0;

            while (attempts < 32)
            {
                attempts++;
                IQueryable<Feedback> eligible = context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        f.ClassificationStatus == ClassificationStatus.Pending
                    )
                    .Where(f =>
                        f.ClassificationClaimedAt == null
                        || f.ClassificationClaimedAt < leaseCutoff
                    );

                if (exactFeedbackId is int exactId)
                {
                    eligible = eligible.Where(f => f.Id == exactId);
                }
                else
                {
                    eligible = eligible
                        .OrderBy(f => f.CreatedAt)
                        .ThenBy(f => f.Id);
                }

                var candidate = await eligible
                    .Select(f => new
                    {
                        f.Id,
                        f.ClassificationClaimAttempts,
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (candidate is null)
                {
                    return null;
                }

                if (candidate.ClassificationClaimAttempts >= maxClaims)
                {
                    var exhausted = await context.Feedbacks
                        .FirstOrDefaultAsync(
                            f => f.Id == candidate.Id,
                            cancellationToken
                        );

                    if (
                        exhausted is not null
                        && exhausted.ClassificationStatus
                            == ClassificationStatus.Pending
                    )
                    {
                        ApplyFailedMetadata(
                            exhausted,
                            retryable: true,
                            settings
                        );
                        exhausted.ClassificationClaimedAt = null;
                        await context.SaveChangesAsync(cancellationToken);
                        await PublishTerminalBestEffortAsync(
                            context,
                            realtime,
                            exhausted,
                            cancellationToken
                        );
                    }

                    if (exactFeedbackId is not null)
                    {
                        return null;
                    }

                    continue;
                }

                var claimed = await TryTakeClaimAsync(
                    context,
                    candidate.Id,
                    now,
                    leaseCutoff,
                    maxClaims,
                    cancellationToken
                );

                if (claimed is null)
                {
                    if (exactFeedbackId is not null)
                    {
                        return null;
                    }

                    // Lost a race — retry another eligible row, not forever.
                    continue;
                }

                return claimed;
            }

            return null;
        }

        /// <summary>
        /// Prefer ExecuteUpdate (atomic across workers). If it updates 0 rows or
        /// the provider rejects it, fall back to a tracked SaveChanges claim
        /// (sufficient for single-reader / InMemory tests).
        /// </summary>
        private static async Task<Feedback?> TryTakeClaimAsync(
            ApplicationDbContext context,
            int feedbackId,
            DateTime now,
            DateTime leaseCutoff,
            int maxClaims,
            CancellationToken cancellationToken
        )
        {
            var usedExecuteUpdate = false;
            try
            {
                var updated = await context.Feedbacks
                    .Where(f => f.Id == feedbackId)
                    .Where(f =>
                        f.ClassificationStatus == ClassificationStatus.Pending
                    )
                    .Where(f =>
                        f.ClassificationClaimedAt == null
                        || f.ClassificationClaimedAt < leaseCutoff
                    )
                    .Where(f => f.ClassificationClaimAttempts < maxClaims)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                f => f.ClassificationClaimedAt,
                                now
                            )
                            .SetProperty(
                                f => f.ClassificationClaimAttempts,
                                f => f.ClassificationClaimAttempts + 1
                            ),
                        cancellationToken
                    );

                usedExecuteUpdate = true;

                if (updated > 0)
                {
                    context.ChangeTracker.Clear();
                    var claimed = await context.Feedbacks
                        .FirstOrDefaultAsync(
                            f => f.Id == feedbackId,
                            cancellationToken
                        );

                    if (
                        claimed is not null
                        && claimed.ClassificationStatus
                            == ClassificationStatus.Pending
                        && claimed.ClassificationClaimedAt is not null
                    )
                    {
                        return claimed;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Provider rejected ExecuteUpdate — use tracked claim below.
            }

            // Fallback: tracked claim (InMemory often reports 0 from ExecuteUpdate).
            if (usedExecuteUpdate)
            {
                context.ChangeTracker.Clear();
            }

            var tracked = await context.Feedbacks
                .FirstOrDefaultAsync(
                    f => f.Id == feedbackId,
                    cancellationToken
                );

            if (
                tracked is null
                || tracked.ClassificationStatus != ClassificationStatus.Pending
                || (
                    tracked.ClassificationClaimedAt is DateTime held
                    && held >= leaseCutoff
                )
                || tracked.ClassificationClaimAttempts >= maxClaims
            )
            {
                return null;
            }

            tracked.ClassificationClaimedAt = now;
            tracked.ClassificationClaimAttempts += 1;
            await context.SaveChangesAsync(cancellationToken);
            return tracked;
        }

        private async Task ClassifyAndPersistAsync(
            ClassificationScope deps,
            Feedback feedback,
            DateTime claimStamp,
            CancellationToken cancellationToken
        )
        {
            if (
                feedback.ClassificationStatus != ClassificationStatus.Pending
                || feedback.ClassificationClaimedAt != claimStamp
            )
            {
                return;
            }

            FeedbackClassificationResult result;
            try
            {
                result = await deps.Provider.ClassifyAsync(
                    feedback.Comment,
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Classification provider failed for Feedback {FeedbackId}",
                    feedback.Id
                );
                await TryPersistFailedAndPublishAsync(
                    deps,
                    feedback,
                    claimStamp,
                    retryable: true,
                    cancellationToken
                );
                return;
            }

            ClassificationStatus terminalStatus;
            FeedbackSentiment? terminalSentiment;
            string? terminalIssuesJson;
            bool failedRetryable;
            try
            {
                (terminalStatus, terminalSentiment, terminalIssuesJson, failedRetryable) =
                    MapProviderResult(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Post-provider logic/mapping bug — Failed now if we can write.
                _logger.LogError(
                    ex,
                    "Classification mapping failed for Feedback {FeedbackId}",
                    feedback.Id
                );
                await TryPersistFailedAndPublishAsync(
                    deps,
                    feedback,
                    claimStamp,
                    retryable: false,
                    cancellationToken
                );
                return;
            }

            await PersistTerminalAndPublishBestEffortAsync(
                deps,
                feedback,
                claimStamp,
                terminalStatus,
                terminalSentiment,
                terminalIssuesJson,
                failedRetryable,
                cancellationToken
            );
        }

        private static (
            ClassificationStatus Status,
            FeedbackSentiment? Sentiment,
            string? DetectedTagsJson,
            bool FailedRetryable
        ) MapProviderResult(FeedbackClassificationResult result)
            => result switch
            {
                FeedbackClassificationResult.Succeeded succeeded => (
                    ClassificationStatus.Succeeded,
                    succeeded.Sentiment,
                    FeedbackClassificationMapping.SerializeDetectedTags(
                        succeeded.DetectedTags
                    ),
                    false
                ),
                FeedbackClassificationResult.Failed failed => (
                    ClassificationStatus.Failed,
                    null,
                    null,
                    failed.Retryable
                ),
                _ => throw new InvalidOperationException(
                    $"Unexpected classification result {result.GetType().Name}"
                ),
            };

        /// <summary>
        /// Mark Failed and persist when possible. If SaveChanges cannot complete,
        /// leave Pending for lease / MaxClaimAttempts (ADR-0010).
        /// </summary>
        private async Task TryPersistFailedAndPublishAsync(
            ClassificationScope deps,
            Feedback feedback,
            DateTime claimStamp,
            bool retryable,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await PersistTerminalAndPublishBestEffortAsync(
                    deps,
                    feedback,
                    claimStamp,
                    ClassificationStatus.Failed,
                    sentiment: null,
                    detectedTagsJson: null,
                    failedRetryable: retryable,
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Could not persist Failed for Feedback {FeedbackId} — leaving Pending for reclaim",
                    feedback.Id
                );
                throw;
            }
        }

        /// <summary>
        /// Terminal DB write is required and gated on still owning the claim stamp.
        /// SignalR publish is best-effort and never reopens classification (ADR-0010).
        /// </summary>
        private async Task PersistTerminalAndPublishBestEffortAsync(
            ClassificationScope deps,
            Feedback feedback,
            DateTime claimStamp,
            ClassificationStatus terminalStatus,
            FeedbackSentiment? sentiment,
            string? detectedTagsJson,
            bool failedRetryable,
            CancellationToken cancellationToken
        )
        {
            // Reload so lease reclaim by another pass cannot be overwritten from
            // stale in-memory claim fields.
            await deps.Context.Entry(feedback).ReloadAsync(cancellationToken);

            if (
                feedback.ClassificationStatus != ClassificationStatus.Pending
                || feedback.ClassificationClaimedAt != claimStamp
            )
            {
                _logger.LogWarning(
                    "Classification terminal skipped for Feedback {FeedbackId} — claim no longer owned",
                    feedback.Id
                );
                return;
            }

            feedback.ClassificationStatus = terminalStatus;
            feedback.Sentiment = sentiment;
            feedback.DetectedTagsJson = detectedTagsJson;
            feedback.ClassificationClaimedAt = null;

            if (terminalStatus == ClassificationStatus.Failed)
            {
                ApplyFailedMetadata(
                    feedback,
                    failedRetryable,
                    _settings.Value
                );
            }
            else
            {
                ClearRetryMetadata(feedback);
            }

            await deps.Context.SaveChangesAsync(cancellationToken);

            await PublishTerminalBestEffortAsync(
                deps.Context,
                deps.Realtime,
                feedback,
                cancellationToken
            );
        }

        private async Task PublishTerminalBestEffortAsync(
            ApplicationDbContext context,
            IFeedbackHomeRealtimePublisher realtime,
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var ownerUserId = await context.RestaurantLocations
                    .Where(location =>
                        location.Id == feedback.RestaurantLocationId
                    )
                    .Select(location => location.Restaurant!.OwnerUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ownerUserId == 0)
                {
                    _logger.LogWarning(
                        "Classification terminal for Feedback {FeedbackId} — owner not found for location {LocationId}",
                        feedback.Id,
                        feedback.RestaurantLocationId
                    );
                    return;
                }

                await realtime.PublishClassificationTerminalAsync(
                    ownerUserId,
                    feedback.Id,
                    feedback.RestaurantLocationId
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Classification terminal publish failed for Feedback {FeedbackId} — DB status kept",
                    feedback.Id
                );
            }
        }

        private static void ApplyFailedMetadata(
            Feedback feedback,
            bool retryable,
            FeedbackClassificationSettings settings
        )
        {
            feedback.ClassificationStatus = ClassificationStatus.Failed;
            feedback.Sentiment = null;
            feedback.DetectedTagsJson = null;

            var maxReopens = Math.Max(1, settings.MaxDelayedReopens);
            if (
                !retryable
                || feedback.ClassificationDelayedReopenCount >= maxReopens
            )
            {
                feedback.ClassificationRetryable = false;
                feedback.ClassificationRetryAfter = null;
                return;
            }

            var initial = TimeSpan.FromMinutes(
                Math.Max(1, settings.DelayedRequeueInitialDelayMinutes)
            );
            var maxDelay = TimeSpan.FromMinutes(
                Math.Max(1, settings.DelayedRequeueMaxDelayMinutes)
            );
            var delay = FeedbackClassificationDelayedRequeue.DelayBeforeReopen(
                feedback.ClassificationDelayedReopenCount,
                initial,
                maxDelay
            );

            feedback.ClassificationRetryable = true;
            feedback.ClassificationRetryAfter = DateTime.UtcNow.Add(delay);
        }

        private static void ClearRetryMetadata(Feedback feedback)
        {
            feedback.ClassificationRetryable = false;
            feedback.ClassificationRetryAfter = null;
        }

        private List<int> DrainWakeHints()
        {
            var ids = new List<int>();
            while (_wake.Reader.TryRead(out var feedbackId))
            {
                ids.Add(feedbackId);
            }

            return ids;
        }

        private TimeSpan SweepInterval
        {
            get
            {
                var seconds = Math.Max(1, _settings.Value.SweepIntervalSeconds);
                return TimeSpan.FromSeconds(seconds);
            }
        }
    }
}
