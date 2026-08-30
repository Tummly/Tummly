using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing;
using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Helpers
{
    public static class PlanEntitlementHttp
    {
        public static IActionResult ToConflict(
            ControllerBase controller,
            CatalogOfferInFlightSyncResult result
        )
        {
            return result switch
            {
                CatalogOfferInFlightSyncResult.CapReached cap =>
                    controller.Conflict(new
                    {
                        success = false,
                        code = ActiveOfferCapGate.CapReachedCode,
                        cap = cap.Cap,
                        current = cap.Current,
                    }),
                CatalogOfferInFlightSyncResult.FailClosed =>
                    controller.Conflict(new { success = false }),
                _ => controller.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { success = false }
                ),
            };
        }

        public static IActionResult ToConflict(
            ControllerBase controller,
            PlanEntitlementCapException ex
        )
        {
            if (ex.Unavailable)
            {
                return controller.Conflict(new { success = false });
            }

            return controller.Conflict(new
            {
                success = false,
                code = ex.Code,
                cap = ex.Cap,
                current = ex.Current,
            });
        }
    }

    public sealed class PlanEntitlementCapException : Exception
    {
        public PlanEntitlementCapException(
            string code,
            int cap,
            int current
        ) : base(code)
        {
            Code = code;
            Cap = cap;
            Current = current;
        }

        private PlanEntitlementCapException() : base("unavailable")
        {
            Unavailable = true;
            Code = string.Empty;
        }

        public string Code { get; }

        public int Cap { get; }

        public int Current { get; }

        public bool Unavailable { get; }

        public static PlanEntitlementCapException UnavailableNow()
            => new();

        public static PlanEntitlementCapException FromSync(
            CatalogOfferInFlightSyncResult result
        )
        {
            return result switch
            {
                CatalogOfferInFlightSyncResult.CapReached cap =>
                    new PlanEntitlementCapException(
                        ActiveOfferCapGate.CapReachedCode,
                        cap.Cap,
                        cap.Current
                    ),
                _ => UnavailableNow(),
            };
        }
    }
}
