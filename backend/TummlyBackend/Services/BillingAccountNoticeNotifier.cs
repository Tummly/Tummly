using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class BillingAccountNoticeNotifier : IBillingAccountNoticeNotifier
    {
        private readonly ApplicationDbContext _context;
        private readonly IOperatorNotificationsService _notifications;
        private readonly IEmailService _emailService;
        private readonly ILogger<BillingAccountNoticeNotifier> _logger;

        public BillingAccountNoticeNotifier(
            ApplicationDbContext context,
            IOperatorNotificationsService notifications,
            IEmailService emailService,
            ILogger<BillingAccountNoticeNotifier>? logger = null
        )
        {
            _context = context;
            _notifications = notifications;
            _emailService = emailService;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    BillingAccountNoticeNotifier
                >.Instance;
        }

        public async Task NotifyCreditThresholdCrossedAsync(
            int restaurantId,
            string channel,
            int thresholdBand,
            string periodKey,
            string billingStatus,
            bool isPilot,
            CancellationToken cancellationToken = default
        )
        {
            if (IsCreditAlertSuppressed(billingStatus))
            {
                return;
            }

            var context = await LoadRestaurantContextAsync(
                restaurantId,
                cancellationToken
            );
            if (context == null)
            {
                return;
            }

            var recipients = await ResolveLowCreditRecipientsAsync(
                context,
                cancellationToken
            );
            if (recipients.Count == 0)
            {
                return;
            }

            var copy = BillingAlertCopyBuilder.CreditThreshold(
                context.WorkspaceName,
                channel,
                thresholdBand
            );
            var notificationType = BillingAlertCopyBuilder.NotificationTypeForCreditThreshold(
                thresholdBand
            );
            var eventKind = BillingAlertCtaResolver.CreditThresholdEventKind(
                thresholdBand,
                isPilot
            );
            var dedupeKey = $"{restaurantId}:{channel}:{thresholdBand}:{periodKey}";

            await DeliverToRecipientsAsync(
                recipients,
                context,
                notificationType,
                copy,
                eventKind,
                dedupeKey,
                channel,
                cancellationToken
            );
        }

        public async Task NotifyPaymentFailureDayStepAsync(
            int restaurantId,
            int dayStep,
            string episodeId,
            CancellationToken cancellationToken = default
        )
        {
            var context = await LoadRestaurantContextAsync(
                restaurantId,
                cancellationToken
            );
            if (context == null)
            {
                return;
            }

            var recipients = await ResolvePaymentFailureRecipientsAsync(
                context,
                cancellationToken
            );
            if (recipients.Count == 0)
            {
                return;
            }

            var copy = BillingAlertCopyBuilder.PaymentFailureDay(
                context.WorkspaceName,
                dayStep
            );
            var notificationType = BillingAlertCopyBuilder.NotificationTypeForPaymentFailureDay(
                dayStep
            );
            var dedupeKey = $"{episodeId}:{dayStep}";

            await DeliverToRecipientsAsync(
                recipients,
                context,
                notificationType,
                copy,
                BillingAlertEventKind.PaymentFailureDunning,
                dedupeKey,
                channel: null,
                cancellationToken
            );
        }

        public async Task NotifyUnpaidPilotLockEnterAsync(
            int restaurantId,
            string episodeKey,
            CancellationToken cancellationToken = default
        )
        {
            var context = await LoadRestaurantContextAsync(
                restaurantId,
                cancellationToken
            );
            if (context == null)
            {
                return;
            }

            var recipients = await ResolvePilotLockRecipientsAsync(
                context,
                cancellationToken
            );
            if (recipients.Count == 0)
            {
                return;
            }

            var copy = BillingAlertCopyBuilder.UnpaidPilotLock(context.WorkspaceName);
            var dedupeKey = $"{restaurantId}:pilot-lock-enter:{episodeKey}";

            await DeliverToRecipientsAsync(
                recipients,
                context,
                BillingAlertCopyBuilder.UnpaidPilotLockNotificationType,
                copy,
                BillingAlertEventKind.UnpaidPilotLock,
                dedupeKey,
                channel: null,
                cancellationToken
            );
        }

        internal static bool IsCreditAlertSuppressed(string billingStatus)
        {
            return string.Equals(billingStatus, "Soft lock", StringComparison.Ordinal)
                || string.Equals(billingStatus, "Dormant", StringComparison.Ordinal);
        }

        private async Task DeliverToRecipientsAsync(
            IReadOnlyList<BillingAlertRecipient> recipients,
            RestaurantAlertContext context,
            string notificationType,
            BillingAlertCopy copy,
            BillingAlertEventKind eventKind,
            string dedupeKey,
            string? channel,
            CancellationToken cancellationToken
        )
        {
            foreach (var recipient in recipients)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cta = BillingAlertCtaResolver.Resolve(
                    eventKind,
                    recipient.BillingCreditsLevel,
                    recipient.PermissionRole,
                    context.AccountType,
                    context.LocationId,
                    channel
                );

                try
                {
                    await _emailService.SendBillingAccountNoticeEmailAsync(
                        recipient.Email,
                        recipient.FirstName,
                        copy.Title,
                        copy.Body,
                        cta.Label,
                        cta.Href
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send billing alert email to user {UserId}",
                        recipient.UserId
                    );
                }

                try
                {
                    await _notifications.ProduceAsync(
                        new ProduceNotificationRequest
                        {
                            UserId = recipient.UserId,
                            Type = notificationType,
                            Title = copy.Title,
                            Body = copy.Body,
                            CtaLabel = cta.Label,
                            CtaHref = cta.Href,
                            DedupeKey = dedupeKey,
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to produce billing alert notification for user {UserId}",
                        recipient.UserId
                    );
                }
            }
        }

        private async Task<RestaurantAlertContext?> LoadRestaurantContextAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == restaurantId,
                    cancellationToken
                );
            if (restaurant == null)
            {
                return null;
            }

            var locationId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (locationId == 0)
            {
                return null;
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );

            return new RestaurantAlertContext(
                restaurantId,
                restaurant.Name,
                restaurant.AccountType,
                restaurant.OwnerUserId,
                restaurant.BillingContactUserId,
                locationId,
                billingAccount
            );
        }

        private async Task<List<BillingAlertRecipient>> ResolveLowCreditRecipientsAsync(
            RestaurantAlertContext context,
            CancellationToken cancellationToken
        )
        {
            var billingAccount = context.BillingAccount
                ?? BillingCreditsService.CreateDefaultBillingAccount(context.RestaurantId);

            var userIds = new HashSet<int>();
            if (billingAccount.LowCreditAlertOwner)
            {
                userIds.Add(context.OwnerUserId);
            }

            if (billingAccount.LowCreditAlertBillingContact)
            {
                userIds.Add(context.BillingContactUserId);
            }

            if (billingAccount.LowCreditAlertAdmin)
            {
                var adminIds = await _context.RestaurantMemberships
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantId == context.RestaurantId
                        && row.Status == MembershipStatus.Active
                        && row.PermissionRole == PermissionRoles.Admin
                    )
                    .Select(row => row.UserId)
                    .ToListAsync(cancellationToken);
                foreach (var id in adminIds)
                {
                    userIds.Add(id);
                }
            }

            return await LoadRecipientsAsync(
                context.RestaurantId,
                userIds,
                cancellationToken
            );
        }

        private async Task<List<BillingAlertRecipient>> ResolvePaymentFailureRecipientsAsync(
            RestaurantAlertContext context,
            CancellationToken cancellationToken
        )
        {
            var billingAccount = context.BillingAccount
                ?? BillingCreditsService.CreateDefaultBillingAccount(context.RestaurantId);

            var userIds = new HashSet<int>();
            if (billingAccount.PaymentFailureAlertOwner)
            {
                userIds.Add(context.OwnerUserId);
            }

            if (billingAccount.PaymentFailureAlertBillingContact)
            {
                userIds.Add(context.BillingContactUserId);
            }

            return await LoadRecipientsAsync(
                context.RestaurantId,
                userIds,
                cancellationToken
            );
        }

        private async Task<List<BillingAlertRecipient>> ResolvePilotLockRecipientsAsync(
            RestaurantAlertContext context,
            CancellationToken cancellationToken
        )
        {
            var userIds = new HashSet<int>
            {
                context.OwnerUserId,
                context.BillingContactUserId,
            };

            return await LoadRecipientsAsync(
                context.RestaurantId,
                userIds,
                cancellationToken
            );
        }

        private async Task<List<BillingAlertRecipient>> LoadRecipientsAsync(
            int restaurantId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken
        )
        {
            var distinctIds = userIds.Distinct().ToList();
            if (distinctIds.Count == 0)
            {
                return [];
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(user => distinctIds.Contains(user.Id))
                .ToListAsync(cancellationToken);

            var memberships = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && distinctIds.Contains(row.UserId)
                    && row.Status == MembershipStatus.Active
                )
                .ToListAsync(cancellationToken);

            var adminOverrides = await _context.RestaurantAdminPermissionCells
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToDictionaryAsync(
                    row => row.AreaId,
                    row => row.Level,
                    cancellationToken
                );

            var recipients = new List<BillingAlertRecipient>();
            foreach (var user in users)
            {
                var membership = memberships.FirstOrDefault(
                    row => row.UserId == user.Id
                );
                var permissionRole =
                    membership?.PermissionRole ?? PermissionRoles.Owner;
                var billingCreditsLevel = DefaultPermissionMatrix.LevelFor(
                    permissionRole,
                    OperatorAreaIds.BillingCredits,
                    adminOverrides
                );

                recipients.Add(
                    new BillingAlertRecipient(
                        user.Id,
                        user.Email,
                        SignInMetadataResolver.ExtractFirstName(user.FullName),
                        permissionRole,
                        billingCreditsLevel
                    )
                );
            }

            return recipients;
        }

        internal sealed record RestaurantAlertContext(
            int RestaurantId,
            string WorkspaceName,
            string AccountType,
            int OwnerUserId,
            int BillingContactUserId,
            int LocationId,
            BillingAccount? BillingAccount
        );

        internal sealed record BillingAlertRecipient(
            int UserId,
            string Email,
            string FirstName,
            string PermissionRole,
            PermissionLevel BillingCreditsLevel
        );
    }
}
