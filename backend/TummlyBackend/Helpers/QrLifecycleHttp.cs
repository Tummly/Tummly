using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Maps QR lifecycle module results to today's HTTP status codes and
    /// anonymous payloads (field / reason / message shapes unchanged).
    /// </summary>
    public static class QrLifecycleHttp
    {
        public static IActionResult ToActionResult(
            ControllerBase controller,
            QrLifecycleResult result
        )
        {
            return result.Kind switch
            {
                QrLifecycleResultKind.Ok => controller.Ok(result.Payload),
                QrLifecycleResultKind.NotFound => controller.NotFound(new
                {
                    success = false,
                    message = result.Message,
                }),
                QrLifecycleResultKind.Validation => result.Field != null
                    ? controller.BadRequest(new
                    {
                        success = false,
                        field = result.Field,
                        message = result.Message,
                    })
                    : controller.BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                    }),
                QrLifecycleResultKind.Conflict => ToConflict(controller, result),
                QrLifecycleResultKind.InvalidTransition =>
                    controller.BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                    }),
                QrLifecycleResultKind.LocationLocked =>
                    controller.BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                    }),
                QrLifecycleResultKind.OperatorBillingLocked =>
                    OperatorBillingLockGate.Forbidden(
                        result.Code ?? result.Message ?? "soft_lock"
                    ),
                _ => controller.StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected lifecycle result.",
                }),
            };
        }

        private static IActionResult ToConflict(
            ControllerBase controller,
            QrLifecycleResult result
        )
        {
            if (result.Code != null)
            {
                return controller.Conflict(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Message,
                    cap = result.Cap,
                    current = result.Current,
                });
            }

            if (result.Field != null)
            {
                return controller.Conflict(new
                {
                    success = false,
                    field = result.Field,
                    message = result.Message,
                });
            }

            if (result.Reason != null)
            {
                return controller.Conflict(new
                {
                    success = false,
                    reason = result.Reason,
                    message = result.Message,
                });
            }

            return controller.Conflict(new
            {
                success = false,
                message = result.Message,
            });
        }
    }
}