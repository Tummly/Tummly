using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Scans activated operators and produces Activation threshold Account notices.
    /// Days remaining uses ceil of remaining time (matches FE computeActivationDaysRemaining).
    /// Does not change Activation gate behavior.
    /// </summary>
    public class ActivationNotificationProducer : IActivationNotificationProducer
    {
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        private readonly ApplicationDbContext _context;
        private readonly IOperatorNotificationsService _notifications;
        private readonly ILogger<ActivationNotificationProducer> _logger;

        public ActivationNotificationProducer(
            ApplicationDbContext context,
            IOperatorNotificationsService notifications,
            ILogger<ActivationNotificationProducer>? logger = null
        )
        {
            _context = context;
            _notifications = notifications;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    ActivationNotificationProducer
                >.Instance;
        }

        public async Task<ActivationNotificationBatchResult> ProcessAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default
        )
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.ActivatedAt != null && u.ActivationExpiresAt != null
                )
                .Select(u => new { u.Id, ExpiresAt = u.ActivationExpiresAt!.Value })
                .ToListAsync(cancellationToken);

            var produced = 0;
            var failed = 0;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var notice = SelectNotice(user.ExpiresAt, utcNow);
                if (notice == null)
                {
                    continue;
                }

                try
                {
                    var result = await _notifications.ProduceAsync(
                        new ProduceNotificationRequest
                        {
                            UserId = user.Id,
                            Type = notice.Type,
                            Title = notice.Title,
                            Body = notice.Body,
                            DedupeKey = user.ExpiresAt.ToString("O"),
                        }
                    );

                    if (result.Status == ProduceNotificationStatus.Created)
                    {
                        produced++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(
                        ex,
                        "Failed to produce {NotificationType} for user {UserId}",
                        notice.Type,
                        user.Id
                    );
                }
            }

            return new ActivationNotificationBatchResult
            {
                Produced = produced,
                Failed = failed,
            };
        }

        /// <summary>
        /// Returns the notice to produce for this expiry, or null when not at a threshold.
        /// </summary>
        internal static ActivationThresholdNotice? SelectNotice(
            DateTime activationExpiresAt,
            DateTime utcNow
        )
        {
            if (activationExpiresAt <= utcNow)
            {
                return new ActivationThresholdNotice(
                    "activation-expired",
                    "Your Activation period has ended",
                    "Your Activation period has ended. Subscribe or contact support to continue using the Operator dashboard."
                );
            }

            var daysRemaining = ComputeActivationDaysRemaining(
                activationExpiresAt,
                utcNow
            );

            return daysRemaining switch
            {
                15 => new ActivationThresholdNotice(
                    "activation-ending-15-days",
                    "Your Activation period ends in 15 days",
                    "You have 15 days left in your Activation period."
                ),
                5 => new ActivationThresholdNotice(
                    "activation-ending-5-days",
                    "Your Activation period ends in 5 days",
                    "You have 5 days left in your Activation period."
                ),
                _ => null,
            };
        }

        /// <summary>
        /// Ceil of remaining time in days — mirrors FE computeActivationDaysRemaining.
        /// Returns null when expiry has passed (caller handles expired separately).
        /// </summary>
        internal static int? ComputeActivationDaysRemaining(
            DateTime activationExpiresAt,
            DateTime utcNow
        )
        {
            var remaining = activationExpiresAt - utcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return null;
            }

            return (int)Math.Ceiling(remaining / OneDay);
        }

        internal sealed record ActivationThresholdNotice(
            string Type,
            string Title,
            string Body
        );
    }
}
