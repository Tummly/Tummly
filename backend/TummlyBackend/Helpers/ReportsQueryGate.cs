using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared auth, location, and half-open date-window checks for Reports GETs.
    /// </summary>
    public static class ReportsQueryGate
    {
        public const int MaxInclusiveCalendarDays = 180;

        public static IActionResult? TryValidateLocationAndWindow(
            ControllerBase controller,
            int locationId,
            DateTime? from,
            DateTime? to,
            out DateTime fromUtc,
            out DateTime toUtc
        )
        {
            fromUtc = default;
            toUtc = default;

            if (locationId <= 0)
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            if (from == null || to == null)
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message = "from and to are required.",
                });
            }

            fromUtc = GuestsDateWindows.EnsureUtc(from.Value);
            toUtc = GuestsDateWindows.EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message = "from must be before to.",
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days.",
                });
            }

            return null;
        }

        public static Task<RestaurantPermissionDecision> AuthorizeReportsViewAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user,
            int locationId
        )
        {
            return permissions.AuthorizeLocationAsync(
                user,
                OperatorAreaIds.Reports,
                PermissionLevel.View,
                locationId
            );
        }

        public static object WireMetric(DTOs.Reports.ReportsMetricDto metric)
        {
            return new
            {
                value = metric.Value,
                valuePrevious = metric.ValuePrevious,
            };
        }
    }
}
