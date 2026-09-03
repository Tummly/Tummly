using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopOrdersListService : IShopOrdersListService
    {
        private const int MaxPageSize = 100;

        private static readonly HashSet<string> VisiblePaymentStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ShopPaymentStatuses.Paid,
                ShopPaymentStatuses.Refunded,
            };

        private static readonly HashSet<string> ValidFulfilmentFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ShopFulfilmentStatuses.Processing,
                ShopFulfilmentStatuses.InTransit,
                ShopFulfilmentStatuses.Delivered,
                ShopFulfilmentStatuses.Cancelled,
            };

        private static readonly HashSet<string> ValidPaymentFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ShopPaymentStatuses.Paid,
                ShopPaymentStatuses.Refunded,
            };

        private static readonly HashSet<string> ValidMaterialTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "table-tents",
                "counter-cards",
                "window-stickers",
                "packaging-stickers",
                "receipt-stickers",
                "delivery-inserts",
            };

        private static readonly HashSet<string> ValidSorts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "newest",
                "oldest",
                "highest-total",
                "lowest-total",
                "status",
            };

        private readonly ApplicationDbContext _context;

        public ShopOrdersListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShopOrderListResponseDto> GetListAsync(
            ShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidateQuery(query);

            var utcNow = DateTime.UtcNow;
            var normalizedSort = NormalizeSort(query.Sort);
            var normalizedQuery = query.Q?.Trim() ?? string.Empty;
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            var scoped = BaseVisibleQuery(query.RestaurantId, query.LocationIds);
            var aggregates = await ComputeAggregatesAsync(scoped, utcNow, cancellationToken);

            var filtered = ApplyFilters(
                scoped,
                normalizedQuery,
                query.FulfilmentStatus,
                query.PaymentStatus,
                query.MaterialType,
                query.OrderDatePreset,
                query.OrderDateFrom,
                query.OrderDateTo,
                query.UtcOffsetMinutes,
                utcNow
            );

            var totalCount = await filtered.CountAsync(cancellationToken);
            var sorted = ApplySort(filtered, normalizedSort);

            var pageRows = await sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(row => row.Lines)
                .ToListAsync(cancellationToken);

            return new ShopOrderListResponseDto
            {
                Items = pageRows.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Aggregates = aggregates,
            };
        }

        private static void ValidateQuery(ShopOrdersListQuery query)
        {
            if (query.LocationIds.Count == 0)
            {
                throw new ArgumentException("At least one location id is required.");
            }

            if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be between 1 and {MaxPageSize}."
                );
            }

            if (!ValidSorts.Contains(query.Sort))
            {
                throw new ArgumentException("Invalid sort.");
            }

            if (
                query.FulfilmentStatus.Any(status =>
                    !ValidFulfilmentFilters.Contains(status)
                )
            )
            {
                throw new ArgumentException("Invalid fulfilmentStatus filter.");
            }

            if (
                query.PaymentStatus.Any(status =>
                    !ValidPaymentFilters.Contains(status)
                )
            )
            {
                throw new ArgumentException("Invalid paymentStatus filter.");
            }

            if (
                query.MaterialType.Any(type => !ValidMaterialTypes.Contains(type))
            )
            {
                throw new ArgumentException("Invalid materialType filter.");
            }

            if (
                query.OrderDatePreset != null
                && query.OrderDatePreset is not ("last-30" or "last-90" or "this-year")
            )
            {
                throw new ArgumentException("Invalid orderDatePreset.");
            }

            if (
                query.OrderDatePreset != null
                && (query.OrderDateFrom != null || query.OrderDateTo != null)
            )
            {
                throw new ArgumentException(
                    "orderDatePreset and custom date bounds are mutually exclusive."
                );
            }
        }

        private static string NormalizeSort(string sort)
        {
            return ValidSorts.Contains(sort) ? sort : "newest";
        }

        private IQueryable<ShopOrder> BaseVisibleQuery(
            int restaurantId,
            IReadOnlyList<int> locationIds
        )
        {
            return _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && locationIds.Contains(row.LocationId)
                    && (
                        row.PaymentStatus == ShopPaymentStatuses.Paid
                        || row.PaymentStatus == ShopPaymentStatuses.Refunded
                    )
                );
        }

        private static IQueryable<ShopOrder> ApplyFilters(
            IQueryable<ShopOrder> query,
            string normalizedQuery,
            IReadOnlyList<string> fulfilmentStatus,
            IReadOnlyList<string> paymentStatus,
            IReadOnlyList<string> materialType,
            string? orderDatePreset,
            DateTime? orderDateFrom,
            DateTime? orderDateTo,
            int utcOffsetMinutes,
            DateTime utcNow
        )
        {
            if (fulfilmentStatus.Count > 0)
            {
                var allowed = fulfilmentStatus
                    .Select(status => status.ToLowerInvariant())
                    .ToList();
                query = query.Where(row =>
                    row.FulfilmentStatus != null
                    && allowed.Contains(row.FulfilmentStatus)
                );
            }

            if (paymentStatus.Count > 0)
            {
                var allowed = paymentStatus
                    .Select(status => status.ToLowerInvariant())
                    .ToList();
                query = query.Where(row => allowed.Contains(row.PaymentStatus));
            }

            if (materialType.Count > 0)
            {
                var allowed = materialType
                    .Select(type => type.ToLowerInvariant())
                    .ToList();
                query = query.Where(row =>
                    row.Lines.Any(line => allowed.Contains(line.CatalogSkuId))
                );
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var term = normalizedQuery.ToLowerInvariant();
                query = query.Where(row =>
                    row.OrderNumber.ToLower().Contains(term)
                    || row.LocationNameSnapshot.ToLower().Contains(term)
                    || row.PlacedByNameSnapshot.ToLower().Contains(term)
                    || row.Lines.Any(line =>
                        line.TitleSnapshot.ToLower().Contains(term)
                    )
                );
            }

            var dateWindow = ResolveOrderDateWindow(
                orderDatePreset,
                orderDateFrom,
                orderDateTo,
                utcOffsetMinutes,
                utcNow
            );
            if (dateWindow != null)
            {
                query = query.Where(row =>
                    row.CreatedAtUtc >= dateWindow.Value.FromUtc
                    && row.CreatedAtUtc < dateWindow.Value.ToUtc
                );
            }

            return query;
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveOrderDateWindow(
            string? preset,
            DateTime? dateFrom,
            DateTime? dateTo,
            int utcOffsetMinutes,
            DateTime utcNow
        )
        {
            if (preset != null)
            {
                var localNow = utcNow.AddMinutes(utcOffsetMinutes);
                var localToday = localNow.Date;
                var localTomorrow = localToday.AddDays(1);

                return preset switch
                {
                    "last-30" => (
                        utcNow.AddDays(-30),
                        utcNow.AddTicks(1)
                    ),
                    "last-90" => (
                        utcNow.AddDays(-90),
                        utcNow.AddTicks(1)
                    ),
                    "this-year" => (
                        new DateTime(localToday.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddMinutes(-utcOffsetMinutes),
                        localTomorrow.AddMinutes(-utcOffsetMinutes)
                    ),
                    _ => null,
                };
            }

            if (dateFrom == null && dateTo == null)
            {
                return null;
            }

            if (dateFrom == null || dateTo == null)
            {
                throw new ArgumentException(
                    "orderDateFrom and orderDateTo must both be set."
                );
            }

            return (dateFrom.Value, dateTo.Value);
        }

        private static IQueryable<ShopOrder> ApplySort(
            IQueryable<ShopOrder> query,
            string sort
        )
        {
            return sort switch
            {
                "oldest" => query.OrderBy(row => row.CreatedAtUtc),
                "highest-total" => query
                    .OrderByDescending(row => row.GrossPence)
                    .ThenByDescending(row => row.CreatedAtUtc),
                "lowest-total" => query
                    .OrderBy(row => row.GrossPence)
                    .ThenByDescending(row => row.CreatedAtUtc),
                "status" => query
                    .OrderBy(row => row.FulfilmentStatus)
                    .ThenByDescending(row => row.CreatedAtUtc),
                _ => query.OrderByDescending(row => row.CreatedAtUtc),
            };
        }

        private static async Task<ShopOrderListAggregatesDto> ComputeAggregatesAsync(
            IQueryable<ShopOrder> scoped,
            DateTime utcNow,
            CancellationToken cancellationToken
        )
        {
            var deliveredCutoff = utcNow.AddDays(-90);

            return new ShopOrderListAggregatesDto
            {
                InProgress = await scoped.CountAsync(
                    row =>
                        row.FulfilmentStatus == ShopFulfilmentStatuses.Processing,
                    cancellationToken
                ),
                Dispatched = await scoped.CountAsync(
                    row =>
                        row.FulfilmentStatus == ShopFulfilmentStatuses.InTransit,
                    cancellationToken
                ),
                DeliveredLast90Days = await scoped.CountAsync(
                    row =>
                        row.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        && row.DeliveredAtUtc != null
                        && row.DeliveredAtUtc >= deliveredCutoff,
                    cancellationToken
                ),
            };
        }

        private static ShopOrderListItemDto MapListItem(ShopOrder order)
        {
            return new ShopOrderListItemDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = ShopOrderPresentation.FormatDisplayDate(order.CreatedAtUtc),
                LocationId = order.LocationId,
                LocationName = order.LocationNameSnapshot,
                MaterialsSummary = ShopOrderPresentation.BuildMaterialsSummary(order.Lines),
                MaterialTypes = order.Lines
                    .Select(line => line.CatalogSkuId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(sku => sku, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                PlacedBy = order.PlacedByNameSnapshot,
                TotalFormatted = ShopOrderPresentation.FormatGbp(order.GrossPence),
                TotalGrossPence = order.GrossPence,
                PaymentStatus = ShopOrderFulfilmentLabels.ToPaymentDisplayLabel(
                    order.PaymentStatus
                ),
                FulfilmentStatus = ShopOrderFulfilmentLabels.ToDisplayLabel(
                    order.FulfilmentStatus
                ),
                UpdatedAtUtc = order.UpdatedAtUtc,
            };
        }
    }
}
