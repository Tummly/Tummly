using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;

namespace TummlyBackend.Helpers
{
    public static class GuestScopedListValidation
    {
        public static Task<bool> EnsureLocationGuestExistsAsync(
            ApplicationDbContext context,
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    guest =>
                        guest.Id == locationGuestId
                        && guest.RestaurantLocationId == locationId,
                    cancellationToken
                );
        }

        public static string ValidatePagingAndSort(
            int page,
            int pageSize,
            string? sort,
            int defaultPageSize
        )
        {
            if (page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (pageSize != defaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {defaultPageSize}."
                );
            }

            var sortKey = (sort ?? string.Empty).Trim().ToLowerInvariant();
            if (sortKey is not ("recent-activity" or "oldest-first"))
            {
                throw new ArgumentException(
                    "sort must be recent-activity or oldest-first."
                );
            }

            return sortKey;
        }

        public static (DateTime? FromUtc, DateTime? ToUtc)
            ResolveOptionalDateWindow(
                string? datePreset,
                DateTime? dateFrom,
                DateTime? dateTo,
                int utcOffsetMinutes,
                DateTime? utcNow = null
            )
        {
            var hasPreset = !string.IsNullOrWhiteSpace(datePreset);
            var hasCustom = dateFrom.HasValue || dateTo.HasValue;

            if (hasPreset && hasCustom)
            {
                throw new ArgumentException(
                    "datePreset and dateFrom/dateTo are mutually exclusive."
                );
            }

            if (hasPreset)
            {
                if (!GuestsDateWindows.IsValidTablePreset(datePreset!))
                {
                    throw new ArgumentException("Invalid datePreset.");
                }

                var (from, to) = GuestsDateWindows.ResolvePreset(
                    datePreset!,
                    utcNow ?? DateTime.UtcNow,
                    utcOffsetMinutes
                );
                return (from, to);
            }

            if (!hasCustom)
            {
                return (null, null);
            }

            if (!dateFrom.HasValue || !dateTo.HasValue)
            {
                throw new ArgumentException(
                    "dateFrom and dateTo are both required for a custom range."
                );
            }

            var (fromUtc, toUtc) = GuestsDateWindows.ResolveCustom(
                dateFrom.Value,
                dateTo.Value
            );
            return (fromUtc, toUtc);
        }
    }
}
