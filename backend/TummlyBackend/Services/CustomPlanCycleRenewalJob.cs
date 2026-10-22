using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Opens unpaid Custom plan cycle orders at renewal and starts dunning.
    /// Soft-lock / dormant advance via <see cref="IBillingAccountLifecycle.TickAsync"/>.
    /// </summary>
    public sealed class CustomPlanCycleRenewalJob : ICustomPlanCycleRenewalJob
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomPlanCycleBillingService _cycleBilling;
        private readonly IBillingAccountLifecycle _lifecycle;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomPlanCycleRenewalJob> _logger;

        public CustomPlanCycleRenewalJob(
            ApplicationDbContext context,
            CustomPlanCycleBillingService cycleBilling,
            IBillingAccountLifecycle lifecycle,
            IConfiguration configuration,
            ILogger<CustomPlanCycleRenewalJob> logger
        )
        {
            _context = context;
            _cycleBilling = cycleBilling;
            _lifecycle = lifecycle;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CustomPlanCycleRenewalJobResult> ProcessAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            var restaurantIds = await _context.BillingAccounts
                .AsNoTracking()
                .Where(row =>
                    row.SubscriptionPlan == BillingSubscriptionPlans.Custom
                    && row.RenewalDateUtc != null
                    && row.RenewalDateUtc <= nowUtc
                    && !row.ScheduledCancelPlan
                    && (
                        row.BillingStatus == BillingStatuses.Active
                        || row.BillingStatus == BillingStatuses.PastDue
                        || row.BillingStatus == BillingStatuses.SoftLock
                    )
                )
                .Select(row => row.RestaurantId)
                .ToListAsync(cancellationToken);

            var ordersOpened = 0;
            var failed = 0;

            foreach (var restaurantId in restaurantIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var opened = await ProcessRestaurantAsync(
                        restaurantId,
                        nowUtc,
                        cancellationToken
                    );
                    if (opened)
                    {
                        ordersOpened++;
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
                    failed++;
                    _logger.LogError(
                        ex,
                        "Custom plan cycle renewal failed for restaurant {RestaurantId}",
                        restaurantId
                    );
                }
            }

            return new CustomPlanCycleRenewalJobResult
            {
                Processed = restaurantIds.Count,
                OrdersOpened = ordersOpened,
                Failed = failed,
            };
        }

        private async Task<bool> ProcessRestaurantAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken
        )
        {
            var account = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null || account.ScheduledCancelPlan)
            {
                return false;
            }

            if (
                !string.Equals(
                    account.SubscriptionPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            var config = CustomPlanConfig.TryParse(account.CustomPlanConfigJson);
            if (config == null || config.PeriodNetPence <= 0)
            {
                return false;
            }

            if (account.RenewalDateUtc == null || account.RenewalDateUtc > nowUtc)
            {
                return false;
            }

            var openIntent = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.CustomPlanCycle
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var openedNew = false;
            string? orderId = openIntent?.OrderId;

            if (openIntent == null)
            {
                var redirectUrl = await BuildRedirectUrlAsync(
                    restaurantId,
                    cancellationToken
                );
                if (redirectUrl == null)
                {
                    return false;
                }

                var renewalKeyDate = account.RenewalDateUtc.Value;
                var idempotencyKey =
                    $"custom-cycle-{restaurantId}-{renewalKeyDate:yyyyMMdd}";

                var (checkoutUrl, error) = await _cycleBilling.StartCyclePayAsync(
                    restaurantId,
                    redirectUrl,
                    idempotencyKey,
                    cancellationToken
                );
                if (error != null || string.IsNullOrWhiteSpace(checkoutUrl))
                {
                    throw new InvalidOperationException(
                        error ?? "custom_cycle_start_failed"
                    );
                }

                openedNew = true;
                orderId = await _context.RevolutOrderIntents
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.Purpose == RevolutOrderIntentPurposes.CustomPlanCycle
                        && row.IdempotencyKey == idempotencyKey
                    )
                    .OrderByDescending(row => row.CreatedAtUtc)
                    .Select(row => row.OrderId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                if (
                    !string.Equals(
                        account.DunningOutstandingOrderId,
                        orderId,
                        StringComparison.Ordinal
                    )
                )
                {
                    account.DunningOutstandingOrderId = orderId;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (account.DunningEpisodeStartedAt == null)
                {
                    await _lifecycle.StartDunningEpisodeAsync(
                        restaurantId,
                        nowUtc,
                        orderId,
                        cancellationToken
                    );
                }
            }

            await _lifecycle.TickAsync(restaurantId, nowUtc, cancellationToken);
            return openedNew;
        }

        private async Task<string?> BuildRedirectUrlAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == restaurantId)
                .Select(row => new { row.AccountType })
                .FirstOrDefaultAsync(cancellationToken);
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
                locationId = restaurantId;
            }

            try
            {
                return RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                    _configuration,
                    restaurant.AccountType,
                    locationId,
                    "plan-subscription",
                    new Dictionary<string, string>
                    {
                        ["customCycleOutcome"] = "success",
                    }
                );
            }
            catch (InvalidOperationException ex)
                when (string.Equals(
                    ex.Message,
                    RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode,
                    StringComparison.Ordinal
                ))
            {
                _logger.LogWarning(
                    "Custom cycle redirect host invalid for restaurant {RestaurantId}",
                    restaurantId
                );
                return null;
            }
        }
    }
}
