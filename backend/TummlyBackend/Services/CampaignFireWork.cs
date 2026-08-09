using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaign fire work loop — wakes on notify; sweeps due Scheduled and
    /// in-progress Sending / Partially sent (ticket 31).
    /// </summary>
    public sealed class CampaignFireWork : ICampaignFireWork
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<CampaignFireWork> _logger;
        private readonly Channel<int> _wake = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            }
        );

        public CampaignFireWork(
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment,
            ILogger<CampaignFireWork> logger
        )
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        public ValueTask NotifyAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        )
        {
            _wake.Writer.TryWrite(campaignId);
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
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken
                    );
                    linked.CancelAfter(SweepInterval);

                    try
                    {
                        _ = await _wake.Reader.ReadAsync(linked.Token);
                    }
                    catch (OperationCanceledException) when (
                        !stoppingToken.IsCancellationRequested
                    )
                    {
                        // Sweep interval elapsed.
                    }

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
                    _logger.LogError(ex, "Campaign fire work loop failed");
                }
            }
        }

        public async Task DrainAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fire =
                scope.ServiceProvider.GetRequiredService<ICampaignFireService>();
            var now = DateTime.UtcNow;

            var dueIds = await context.Campaigns
                .AsNoTracking()
                .Where(
                    campaign =>
                        campaign.Status == CampaignFireService.SendingStatus
                        || (
                            campaign.Status == CampaignFireService.ScheduledStatus
                            && campaign.ScheduledAtUtc != null
                            && campaign.ScheduledAtUtc <= now
                        )
                )
                .OrderBy(campaign => campaign.Id)
                .Select(campaign => campaign.Id)
                .ToListAsync(cancellationToken);

            foreach (var campaignId in dueIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await fire.FireAsync(campaignId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Campaign fire failed for {CampaignId}",
                        campaignId
                    );
                }
            }
        }
    }
}
