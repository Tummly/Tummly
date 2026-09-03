using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Produces <c>weekly-brief-ready</c> after a successful first-write generate.
    /// Preference gate and dedupe live in <see cref="IOperatorNotificationsService.ProduceAsync"/>.
    /// CTA opens the Reports Weekly Brief page for the location.
    /// </summary>
    public sealed class WeeklyBriefReadyNotifier : IWeeklyBriefReadyNotifier
    {
        public const string NotificationType = "weekly-brief-ready";

        public const string CtaLabel = "View weekly brief";

        /// <summary>
        /// Single-location: <c>/single-dashboard/reports/weekly-brief?location={id}</c>.
        /// Multi-location: <c>/multi-dashboard/reports/weekly-brief?location={id}</c>.
        /// Matches frontend <c>operatorDashboardWeeklyBriefPath</c>.
        /// </summary>
        public static string ReportsWeeklyBriefCtaHref(string accountType, int locationId)
            => string.Equals(accountType, "Multi", StringComparison.Ordinal)
                ? $"/multi-dashboard/reports/weekly-brief?location={locationId}"
                : $"/single-dashboard/reports/weekly-brief?location={locationId}";

        public static string DedupeKeyFor(int locationId, string weekKey)
            => $"{locationId}:{weekKey}";

        private readonly ApplicationDbContext _context;
        private readonly IOperatorNotificationsService _notifications;
        private readonly ILogger<WeeklyBriefReadyNotifier> _logger;

        public WeeklyBriefReadyNotifier(
            ApplicationDbContext context,
            IOperatorNotificationsService notifications,
            ILogger<WeeklyBriefReadyNotifier>? logger = null
        )
        {
            _context = context;
            _notifications = notifications;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    WeeklyBriefReadyNotifier
                >.Instance;
        }

        public async Task NotifyGeneratedAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            CancellationToken cancellationToken = default
        )
        {
            var owner = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => new
                {
                    location.Restaurant!.OwnerUserId,
                    location.Restaurant.AccountType,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (owner is null)
            {
                return;
            }

            try
            {
                await _notifications.ProduceAsync(
                    new ProduceNotificationRequest
                    {
                        UserId = owner.OwnerUserId,
                        Type = NotificationType,
                        Title = "Weekly brief ready",
                        Body = "Your weekly summary is ready.",
                        CtaLabel = CtaLabel,
                        CtaHref = ReportsWeeklyBriefCtaHref(
                            owner.AccountType,
                            locationId
                        ),
                        DedupeKey = DedupeKeyFor(
                            locationId,
                            closedWeek.WeekKey
                        ),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to produce weekly-brief-ready for location {LocationId} week {WeekKey}",
                    locationId,
                    closedWeek.WeekKey
                );
            }
        }
    }
}
