using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Read-only Customers directory and account profile for internal Admin staff.
    /// Never returns guest data.
    /// </summary>
    public sealed class AdminAccountDirectoryService : IAdminAccountDirectoryService
    {
        public const int DefaultTake = 50;
        public const int MaxTake = 100;

        private readonly ApplicationDbContext _context;

        public AdminAccountDirectoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminAccountListResultDto> ListAsync(
            AdminAccountListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            if (
                query.CreatedFrom is DateTime from
                && query.CreatedBefore is DateTime before
                && from >= before
            )
            {
                throw new ArgumentException(
                    "createdFrom must be earlier than createdTo."
                );
            }

            var take = Math.Clamp(query.Take, 1, MaxTake);
            var skip = Math.Max(0, query.Skip);

            var rows = _context.Restaurants
                .AsNoTracking()
                .Where(row => row.BillingAccount != null);

            var term = query.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                // A non-numeric term never equals a Workspace ID.
                var workspaceId = int.TryParse(term, out var parsed) ? parsed : -1;
                rows = rows.Where(row =>
                    row.Id == workspaceId
                    || row.Name.Contains(term)
                    || (
                        row.BusinessDetails != null
                        && (
                            (row.BusinessDetails.TradingName ?? string.Empty).Contains(term)
                            || (row.BusinessDetails.LegalBusinessName ?? string.Empty).Contains(term)
                        )
                    )
                );
            }

            var plan = query.SubscriptionPlan?.Trim();
            if (!string.IsNullOrEmpty(plan))
            {
                rows = rows.Where(row => row.BillingAccount!.SubscriptionPlan == plan);
            }

            var status = query.BillingStatus?.Trim();
            if (!string.IsNullOrEmpty(status))
            {
                rows = rows.Where(row => row.BillingAccount!.BillingStatus == status);
            }

            if (query.CreatedFrom is DateTime createdFrom)
            {
                rows = rows.Where(row => row.CreatedAt >= createdFrom);
            }

            if (query.CreatedBefore is DateTime createdBefore)
            {
                rows = rows.Where(row => row.CreatedAt < createdBefore);
            }

            var totalCount = await rows.CountAsync(cancellationToken);

            var items = await rows
                .OrderByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Skip(skip)
                .Take(take)
                .Select(row => new AdminAccountListRowDto
                {
                    RestaurantId = row.Id,
                    DisplayName = row.Name,
                    SubscriptionPlan = row.BillingAccount!.SubscriptionPlan,
                    BillingStatus = row.BillingAccount!.BillingStatus,
                    CreatedAtUtc = row.CreatedAt,
                    PeriodEndUtc = row.BillingAccount!.SubscriptionPlan == BillingSubscriptionPlans.Pilot
                        ? row.BillingAccount!.PilotPeriodEnd
                        : row.BillingAccount!.RenewalDateUtc,
                    ActiveLocationCount = row.Locations.Count(
                        location => location.LifecycleStatus == LocationLifecycleStatus.Active
                    ),
                    OwnerDisplayName = row.OwnerUser.FullName,
                })
                .ToListAsync(cancellationToken);

            return new AdminAccountListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                Skip = skip,
                Take = take,
            };
        }
    }
}
