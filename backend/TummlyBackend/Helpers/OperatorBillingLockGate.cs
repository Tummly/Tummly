using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed class OperatorBillingLockedException : Exception
    {
        public OperatorBillingLockedException(string code)
            : base(code)
        {
            Code = code;
        }

        public string Code { get; }
    }

    /// <summary>
    /// Soft lock / Dormant / chargeback paid-write gate (ticket 33).
    /// Does not apply past_due_sends_blocked (create/edit/schedule stay open on day 7–9).
    /// </summary>
    public static class OperatorBillingLockGate
    {
        public static async Task EnsurePaidWriteAllowedAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var deny = await EvaluatePaidWriteDenyAsync(
                context,
                restaurantId,
                cancellationToken
            );
            if (deny != null)
            {
                throw new OperatorBillingLockedException(deny);
            }
        }

        public static async Task EnsurePaidWriteAllowedForLocationAsync(
            ApplicationDbContext context,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId == 0)
            {
                return;
            }

            await EnsurePaidWriteAllowedAsync(
                context,
                restaurantId,
                cancellationToken
            );
        }

        public static async Task<string?> EvaluatePaidWriteDenyAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var account = await context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null)
            {
                return null;
            }

            return OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account)
            );
        }

        public static ObjectResult Forbidden(string code)
        {
            return new ObjectResult(new
            {
                success = false,
                code,
                message = code,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
