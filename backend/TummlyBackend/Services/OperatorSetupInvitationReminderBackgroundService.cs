using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class OperatorSetupInvitationReminderBackgroundService
        : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;

        private readonly IHostEnvironment _environment;

        private readonly ILogger<OperatorSetupInvitationReminderBackgroundService> _logger;

        public OperatorSetupInvitationReminderBackgroundService(
            IServiceProvider serviceProvider,
            IHostEnvironment environment,
            ILogger<OperatorSetupInvitationReminderBackgroundService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken
        )
        {
            if (_environment.IsEnvironment("Testing"))
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested
                )
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Operator Setup invitation reminder job failed"
                    );
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessRemindersAsync(
            CancellationToken stoppingToken
        )
        {
            using var scope = _serviceProvider.CreateScope();
            var adminService =
                scope.ServiceProvider.GetRequiredService<IAdminService>();

            var sentCount =
                await adminService
                    .ProcessOperatorSetupInvitationRemindersAsync();

            if (sentCount > 0)
            {
                _logger.LogInformation(
                    "Sent {Count} Operator Setup invitation reminder(s)",
                    sentCount
                );
            }

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
