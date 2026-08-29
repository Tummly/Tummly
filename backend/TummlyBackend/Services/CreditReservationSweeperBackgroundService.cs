using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Releases Recovery SMS holds after the 15-minute TTL (ticket 22).
    /// </summary>
    public sealed class CreditReservationSweeperBackgroundService
        : BackgroundService
    {
        public static readonly TimeSpan HoldTtl = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _clock;
        private readonly ILogger<CreditReservationSweeperBackgroundService> _logger;

        public CreditReservationSweeperBackgroundService(
            IServiceScopeFactory scopeFactory,
            TimeProvider clock,
            ILogger<CreditReservationSweeperBackgroundService> logger
        )
        {
            _scopeFactory = scopeFactory;
            _clock = clock;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SweepExpiredHoldsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Recovery SMS hold sweeper failed.");
                }

                await Task.Delay(SweepInterval, stoppingToken);
            }
        }

        internal async Task SweepExpiredHoldsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var billing = scope.ServiceProvider
                .GetRequiredService<IRecoverySmsBillingReserve>();
            var now = _clock.GetUtcNow().UtcDateTime;

            var expired = await context.RecoverySmsSendIdempotencies
                .Where(row =>
                    row.CompletedGuestResponseId == null
                    && row.HoldExpiresAtUtc <= now
                )
                .ToListAsync(cancellationToken);

            foreach (var row in expired)
            {
                await billing.ReleaseAsync(
                    new RecoverySmsBillingReleaseRequest
                    {
                        FeedbackId = row.FeedbackId,
                        ReservationRef = row.ReservationRef,
                    },
                    cancellationToken
                );
                context.RecoverySmsSendIdempotencies.Remove(row);
            }

            if (expired.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
