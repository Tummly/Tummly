using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Durable Guest response email delivery: Pending rows are the queue;
    /// Channel is wake-only (ADR-0026).
    /// </summary>
    public sealed class GuestResponseEmailDeliveryWork
        : IGuestResponseEmailDeliveryWork
    {
        private sealed record DeliveryScope(
            ApplicationDbContext Context,
            IEmailService EmailService,
            IConfiguration Configuration
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
        private readonly IOptions<GuestResponseEmailDeliverySettings> _settings;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<GuestResponseEmailDeliveryWork> _logger;

        public GuestResponseEmailDeliveryWork(
            IServiceScopeFactory scopeFactory,
            IOptions<GuestResponseEmailDeliverySettings> settings,
            IHostEnvironment environment,
            ILogger<GuestResponseEmailDeliveryWork> logger
        )
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _environment = environment;
            _logger = logger;
        }

        private TimeSpan SweepInterval =>
            TimeSpan.FromSeconds(
                Math.Max(5, _settings.Value.SweepIntervalSeconds)
            );

        public ValueTask NotifyAsync(
            int guestResponseId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                if (!_wake.Writer.TryWrite(guestResponseId))
                {
                    _logger.LogWarning(
                        "Guest response email delivery wake dropped for {GuestResponseId}",
                        guestResponseId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Guest response email delivery wake failed for {GuestResponseId}",
                    guestResponseId
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
            foreach (var exactId in DrainWakeHints())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ClaimAndDeliverGuardedAsync(exactId, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var worked = await ClaimAndDeliverGuardedAsync(
                    exactGuestResponseId: null,
                    cancellationToken
                );
                if (!worked)
                {
                    break;
                }
            }
        }

        private IEnumerable<int> DrainWakeHints()
        {
            while (_wake.Reader.TryRead(out var id))
            {
                yield return id;
            }
        }

        private async Task<bool> ClaimAndDeliverGuardedAsync(
            int? exactGuestResponseId,
            CancellationToken cancellationToken
        )
        {
            try
            {
                return await ClaimAndDeliverInScopeAsync(
                    exactGuestResponseId,
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
                _logger.LogError(
                    ex,
                    "Guest response email delivery pass aborted for {GuestResponseId} — leaving Pending for reclaim",
                    exactGuestResponseId ?? 0
                );
                return false;
            }
        }

        private async Task<bool> ClaimAndDeliverInScopeAsync(
            int? exactGuestResponseId,
            CancellationToken cancellationToken
        )
        {
            using var scope = _scopeFactory.CreateScope();
            var deps = ResolveScope(scope.ServiceProvider);
            var settings = _settings.Value;

            var row = await TryClaimAtomicAsync(
                deps.Context,
                settings,
                exactGuestResponseId,
                cancellationToken
            );

            if (row is null)
            {
                return false;
            }

            var claimStamp = row.EmailDeliveryClaimedAt;
            await DeliverAndPersistAsync(
                deps,
                row,
                claimStamp!.Value,
                settings,
                cancellationToken
            );
            return true;
        }

        private static DeliveryScope ResolveScope(IServiceProvider services)
            => new(
                services.GetRequiredService<ApplicationDbContext>(),
                services.GetRequiredService<IEmailService>(),
                services.GetRequiredService<IConfiguration>()
            );

        private async Task<FeedbackGuestResponse?> TryClaimAtomicAsync(
            ApplicationDbContext context,
            GuestResponseEmailDeliverySettings settings,
            int? exactGuestResponseId,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var leaseCutoff = now - TimeSpan.FromMinutes(
                Math.Max(1, settings.ClaimLeaseMinutes)
            );

            for (var attempts = 0; attempts < 32; attempts++)
            {
                IQueryable<FeedbackGuestResponse> eligible = context
                    .FeedbackGuestResponses
                    .AsNoTracking()
                    .Where(r =>
                        r.EmailDeliveryStatus
                            == GuestResponseEmailDeliveryStatus.Pending
                    )
                    .Where(r =>
                        r.EmailDeliveryClaimedAt == null
                        || r.EmailDeliveryClaimedAt < leaseCutoff
                    )
                    .Where(r =>
                        r.EmailDeliveryRetryAfter == null
                        || r.EmailDeliveryRetryAfter <= now
                    );

                if (exactGuestResponseId is int exactId)
                {
                    eligible = eligible.Where(r => r.Id == exactId);
                }
                else
                {
                    eligible = eligible
                        .OrderBy(r => r.CreatedAt)
                        .ThenBy(r => r.Id);
                }

                var candidateId = await eligible
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (candidateId is null)
                {
                    return null;
                }

                var claimed = await TryTakeClaimAsync(
                    context,
                    candidateId.Value,
                    now,
                    leaseCutoff,
                    cancellationToken
                );

                if (claimed is null)
                {
                    if (exactGuestResponseId is not null)
                    {
                        return null;
                    }

                    continue;
                }

                return claimed;
            }

            return null;
        }

        private static async Task<FeedbackGuestResponse?> TryTakeClaimAsync(
            ApplicationDbContext context,
            int guestResponseId,
            DateTime now,
            DateTime leaseCutoff,
            CancellationToken cancellationToken
        )
        {
            var usedExecuteUpdate = false;
            try
            {
                var updated = await context.FeedbackGuestResponses
                    .Where(r => r.Id == guestResponseId)
                    .Where(r =>
                        r.EmailDeliveryStatus
                            == GuestResponseEmailDeliveryStatus.Pending
                    )
                    .Where(r =>
                        r.EmailDeliveryClaimedAt == null
                        || r.EmailDeliveryClaimedAt < leaseCutoff
                    )
                    .Where(r =>
                        r.EmailDeliveryRetryAfter == null
                        || r.EmailDeliveryRetryAfter <= now
                    )
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                r => r.EmailDeliveryClaimedAt,
                                now
                            )
                            .SetProperty(
                                r => r.EmailDeliveryAttemptCount,
                                r => r.EmailDeliveryAttemptCount + 1
                            ),
                        cancellationToken
                    );

                usedExecuteUpdate = true;

                if (updated > 0)
                {
                    context.ChangeTracker.Clear();
                    var claimed = await context.FeedbackGuestResponses
                        .FirstOrDefaultAsync(
                            r => r.Id == guestResponseId,
                            cancellationToken
                        );

                    if (
                        claimed is not null
                        && claimed.EmailDeliveryStatus
                            == GuestResponseEmailDeliveryStatus.Pending
                        && claimed.EmailDeliveryClaimedAt is not null
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

            if (usedExecuteUpdate)
            {
                context.ChangeTracker.Clear();
            }

            var tracked = await context.FeedbackGuestResponses
                .FirstOrDefaultAsync(
                    r => r.Id == guestResponseId,
                    cancellationToken
                );

            if (
                tracked is null
                || tracked.EmailDeliveryStatus
                    != GuestResponseEmailDeliveryStatus.Pending
                || (
                    tracked.EmailDeliveryClaimedAt is DateTime held
                    && held >= leaseCutoff
                )
                || (
                    tracked.EmailDeliveryRetryAfter is DateTime retryAfter
                    && retryAfter > now
                )
            )
            {
                return null;
            }

            tracked.EmailDeliveryClaimedAt = now;
            tracked.EmailDeliveryAttemptCount += 1;
            await context.SaveChangesAsync(cancellationToken);
            return tracked;
        }

        private async Task DeliverAndPersistAsync(
            DeliveryScope deps,
            FeedbackGuestResponse row,
            DateTime claimStamp,
            GuestResponseEmailDeliverySettings settings,
            CancellationToken cancellationToken
        )
        {
            if (
                row.EmailDeliveryStatus
                    != GuestResponseEmailDeliveryStatus.Pending
                || row.EmailDeliveryClaimedAt != claimStamp
            )
            {
                return;
            }

            try
            {
                await SendGuestResponseEmailForRowAsync(
                    deps,
                    row,
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Resend failed for guest response {GuestResponseId} — staying Pending",
                    row.Id
                );

                var failed = await deps.Context.FeedbackGuestResponses
                    .FirstOrDefaultAsync(
                        r => r.Id == row.Id,
                        cancellationToken
                    );

                if (
                    failed is null
                    || failed.EmailDeliveryStatus
                        != GuestResponseEmailDeliveryStatus.Pending
                    || failed.EmailDeliveryClaimedAt != claimStamp
                )
                {
                    return;
                }

                failed.EmailDeliveryClaimedAt = null;
                // Always schedule a future retry so a single DrainAsync cannot
                // spin forever on a hard Resend failure (RetryBackoffSeconds may
                // be 0 in tests).
                var backoffSeconds = Math.Max(
                    1,
                    settings.RetryBackoffSeconds
                );
                failed.EmailDeliveryRetryAfter =
                    DateTime.UtcNow.AddSeconds(backoffSeconds);
                await deps.Context.SaveChangesAsync(cancellationToken);
                return;
            }

            var accepted = await deps.Context.FeedbackGuestResponses
                .FirstOrDefaultAsync(
                    r => r.Id == row.Id,
                    cancellationToken
                );

            if (
                accepted is null
                || accepted.EmailDeliveryStatus
                    != GuestResponseEmailDeliveryStatus.Pending
                || accepted.EmailDeliveryClaimedAt != claimStamp
            )
            {
                return;
            }

            accepted.EmailDeliveryStatus =
                GuestResponseEmailDeliveryStatus.Accepted;
            accepted.EmailDeliveredAt = DateTime.UtcNow;
            accepted.EmailDeliveryClaimedAt = null;
            accepted.EmailDeliveryRetryAfter = null;
            await deps.Context.SaveChangesAsync(cancellationToken);
        }

        private static async Task SendGuestResponseEmailForRowAsync(
            DeliveryScope deps,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        )
        {
            var feedback = await deps.Context.Feedbacks
                .AsNoTracking()
                .Include(f => f.RestaurantLocation!)
                .ThenInclude(l => l.Restaurant)
                .FirstOrDefaultAsync(
                    f => f.Id == row.FeedbackId,
                    cancellationToken
                );

            if (feedback is null)
            {
                throw new InvalidOperationException(
                    $"Feedback {row.FeedbackId} not found for guest response {row.Id}."
                );
            }

            if (
                feedback.ContactType != ContactType.Email
                || string.IsNullOrWhiteSpace(feedback.GuestContact)
            )
            {
                throw new InvalidOperationException(
                    $"Guest response {row.Id} has no email contact for delivery."
                );
            }

            if (string.IsNullOrWhiteSpace(row.Subject))
            {
                throw new InvalidOperationException(
                    $"Guest response {row.Id} is missing subject for email delivery."
                );
            }

            var location = feedback.RestaurantLocation
                ?? throw new InvalidOperationException(
                    "Feedback location not found."
                );
            var restaurant = location.Restaurant
                ?? throw new InvalidOperationException("Restaurant not found.");

            var brandTitle = string.IsNullOrWhiteSpace(restaurant.Name)
                ? location.LocationName
                : restaurant.Name.Trim();
            var brandSubtitle =
                string.Equals(
                    brandTitle,
                    location.LocationName,
                    StringComparison.OrdinalIgnoreCase
                )
                    ? null
                    : location.LocationName;

            var offer = await ResolveOfferBlockAsync(
                deps.Context,
                row.Id,
                cancellationToken
            );

            await deps.EmailService.SendGuestResponseEmailAsync(
                feedback.GuestContact.Trim(),
                row.Subject!,
                brandTitle,
                brandSubtitle,
                location.Address,
                row.Body,
                brandLogoUrl: BrandLogoRules.BuildAbsolutePublicUrl(
                    restaurant.BrandLogoObjectKey,
                    deps.Configuration["PublicApi:BaseUrl"]
                ),
                offer: offer
            );
        }

        private static async Task<GuestResponseEmailOfferBlock?> ResolveOfferBlockAsync(
            ApplicationDbContext context,
            int guestResponseId,
            CancellationToken cancellationToken
        )
        {
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(row => row.Id == guestResponseId)
                .Select(row => new { row.FeedbackId, row.Intent })
                .FirstOrDefaultAsync(cancellationToken);

            if (guestResponse is null)
            {
                return null;
            }

            // Catalog cutover (ticket 05): prefer Offer issue linked to this
            // Feedback. Fall back to historical one-off FeedbackRecoveryOffer.
            if (guestResponse.Intent
                == FeedbackRecoveryIntent.RespondWithRecoveryOffer)
            {
                var catalogIssue = await context.OfferIssues
                    .AsNoTracking()
                    .Where(issue =>
                        issue.FeedbackId == guestResponse.FeedbackId
                        && issue.Source == OfferIssueSources.Recovery
                    )
                    .OrderByDescending(issue => issue.IssuedAtUtc)
                    .ThenByDescending(issue => issue.Id)
                    .Select(issue => new
                    {
                        issue.Title,
                        issue.Description,
                        issue.ClaimCode,
                        issue.ExpiryAtUtc,
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (catalogIssue is not null)
                {
                    return new GuestResponseEmailOfferBlock(
                        Title: catalogIssue.Title,
                        Description: catalogIssue.Description,
                        RedemptionCode: catalogIssue.ClaimCode,
                        ExpiryLabel: FeedbackRecoveryOfferMapping.FormatOfferExpiryLabel(
                            catalogIssue.ExpiryAtUtc
                        )
                    );
                }
            }

            var offer = await context.FeedbackRecoveryOffers
                .AsNoTracking()
                .Where(o => o.GuestResponseId == guestResponseId)
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Select(o => new
                {
                    o.Title,
                    o.Description,
                    o.RedemptionCode,
                    o.ExpiryAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (offer is null)
            {
                return null;
            }

            return new GuestResponseEmailOfferBlock(
                Title: offer.Title,
                Description: offer.Description,
                RedemptionCode: offer.RedemptionCode,
                ExpiryLabel: FeedbackRecoveryOfferMapping.FormatOfferExpiryLabel(
                    offer.ExpiryAt
                )
            );
        }
    }
}
