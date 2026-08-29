using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/billing-credits")]
    [Authorize]
    public class BillingCreditsController : ControllerBase
    {
        private readonly IBillingCreditsService _billingCredits;
        private readonly IExtraGroupLocationService _extraGroupLocation;
        private readonly IRestaurantPermissionHelper _permissions;

        public BillingCreditsController(
            IBillingCreditsService billingCredits,
            IExtraGroupLocationService extraGroupLocation,
            IRestaurantPermissionHelper permissions
        )
        {
            _billingCredits = billingCredits;
            _extraGroupLocation = extraGroupLocation;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var actorCanManage =
                manage.Status == RestaurantPermissionStatus.Allowed;

            var page = await _billingCredits.GetPageAsync(
                userId,
                decision.RestaurantId,
                actorCanManage
            );
            if (page == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(page);
        }

        [HttpGet("invoices/{invoiceNo}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(string invoiceNo)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var pdf = await _billingCredits.GetInvoicePdfAsync(
                decision.RestaurantId,
                invoiceNo
            );
            if (pdf == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invoice not found.",
                });
            }

            return File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
        }

        [HttpPost("payment-method/update")]
        public async Task<IActionResult> CreatePaymentMethodUpdateSession()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            try
            {
                var session = await _billingCredits.CreatePaymentMethodUpdateSessionAsync(
                    manage.RestaurantId
                );
                if (session == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Restaurant not found.",
                    });
                }

                return Ok(session);
            }
            catch (InvalidOperationException ex) when (IsOperatorBillingLockCode(ex.Message))
            {
                return OperatorBillingLockForbidden(ex.Message);
            }
            catch (InvalidOperationException ex) when (
                IsRevolutMerchantNotReadyCode(ex.Message)
            )
            {
                return RevolutMerchantNotReady(ex.Message);
            }
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var usage = await _billingCredits.GetUsageAsync(decision.RestaurantId);
            if (usage == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(usage);
        }

        [HttpPost("plan-change")]
        public async Task<IActionResult> PostPlanChange(
            [FromBody] PlanChangeRequestDto request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var view = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = view.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader);
            var idempotencyKey = idempotencyHeader.ToString();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                idempotencyKey = null;
            }

            try
            {
                var result = await _billingCredits.SubmitPlanChangeAsync(
                    userId,
                    view.RestaurantId,
                    request,
                    idempotencyKey
                );
                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Restaurant not found.",
                    });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "billing_write_not_permitted"
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = "billing_write_not_permitted",
                        message = "Only the account owner may change the plan.",
                    }
                );
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "invalid_plan_target"
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = "invalid_plan_target",
                    message = "Pilot is not a valid plan-change target.",
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "idempotency_key_required"
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = "idempotency_key_required",
                    message = "Idempotency-Key header is required.",
                });
            }
            catch (InvalidOperationException ex) when (
                IsOperatorBillingLockCode(ex.Message) || ex.Message == "forbidden"
            )
            {
                return OperatorBillingLockForbidden(
                    ex.Message == "forbidden" ? "forbidden" : ex.Message
                );
            }
            catch (InvalidOperationException ex) when (
                ex.Message is "billing_status_not_active"
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = "billing_status_not_active",
                        message = "Plan change requires an Active billing status.",
                    }
                );
            }
            catch (InvalidOperationException ex) when (
                ex.Message is "soft_lock" or "dormant"
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = ex.Message,
                        message = ex.Message,
                    }
                );
            }
            catch (InvalidOperationException ex) when (
                ex.Message is "location_cap_reached" or "team_member_cap_reached"
            )
            {
                return Conflict(new
                {
                    success = false,
                    code = ex.Message,
                    message = "Resolve locations or team members before this plan change.",
                });
            }
            catch (InvalidOperationException ex) when (
                IsRevolutMerchantNotReadyCode(ex.Message)
            )
            {
                return RevolutMerchantNotReady(ex.Message);
            }
        }

        [HttpDelete("scheduled-change")]
        public async Task<IActionResult> DeleteScheduledChange()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var view = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = view.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var cleared = await _billingCredits.ClearScheduledChangeAsync(
                userId,
                view.RestaurantId
            );
            if (cleared == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            if (!cleared.Value.Success)
            {
                if (cleared.Value.ErrorCode == "billing_write_not_permitted")
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,
                            code = "billing_write_not_permitted",
                            message = "Only the account owner may clear a scheduled change.",
                        }
                    );
                }

                return BadRequest(new
                {
                    success = false,
                    code = "scheduled_change_empty",
                    message = "There is no scheduled change to clear.",
                });
            }

            return Ok(new { success = true });
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var list = await _billingCredits.GetActivityAsync(
                decision.RestaurantId,
                skip,
                take
            );
            if (list == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(list);
        }

        [HttpPut("billing-contacts")]
        public async Task<IActionResult> UpdateBillingContacts(
            [FromBody] UpdateBillingContactsRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var (response, error, statusCode) =
                await _billingCredits.UpdateBillingContactsAsync(
                    userId,
                    manage.RestaurantId,
                    request
                );

            if (response == null)
            {
                return StatusCode(statusCode, new
                {
                    success = false,
                    message = error,
                });
            }

            return Ok(response);
        }

        [HttpPost("top-up/confirm")]
        public async Task<IActionResult> ConfirmCreditTopUp(
            [FromBody] CreditTopUpRequestDto request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var (response, statusCode, errorMessage) =
                await _billingCredits.ConfirmCreditTopUpAsync(
                    userId,
                    manage.RestaurantId,
                    manage.Status == RestaurantPermissionStatus.Allowed,
                    request
                );

            if (response == null)
            {
                if (
                    errorMessage != null
                    && OperatorBillingLockEvaluator.IsLockCode(errorMessage)
                )
                {
                    return OperatorBillingLockForbidden(errorMessage);
                }

                return StatusCode(statusCode, new
                {
                    success = false,
                    message = errorMessage,
                });
            }

            return Ok(response);
        }

        [HttpPost("top-up/pay")]
        public async Task<IActionResult> PayCreditTopUp(
            [FromBody] CreditTopUpRequestDto request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var (response, statusCode, errorMessage) =
                await _billingCredits.PayCreditTopUpAsync(
                    userId,
                    manage.RestaurantId,
                    manage.Status == RestaurantPermissionStatus.Allowed,
                    request
                );

            if (response == null)
            {
                if (
                    errorMessage != null
                    && OperatorBillingLockEvaluator.IsLockCode(errorMessage)
                )
                {
                    return OperatorBillingLockForbidden(errorMessage);
                }

                if (
                    errorMessage != null
                    && IsRevolutMerchantNotReadyCode(errorMessage)
                )
                {
                    return RevolutMerchantNotReady(errorMessage);
                }

                return StatusCode(statusCode, new
                {
                    success = false,
                    message = errorMessage,
                });
            }

            return Ok(response);
        }

        [HttpPost("extra-location")]
        public async Task<IActionResult> PostExtraLocation(
            [FromBody] ExtraLocationRequestDto request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var action = (request?.Action ?? string.Empty).Trim().ToLowerInvariant();
            if (action == "add")
            {
                if (
                    !Request.Headers.TryGetValue("Idempotency-Key", out var key)
                    || string.IsNullOrWhiteSpace(key)
                )
                {
                    return BadRequest(new
                    {
                        success = false,
                        code = "idempotency_key_required",
                        message = "Idempotency-Key header is required.",
                    });
                }
            }

            try
            {
                var result = await _extraGroupLocation.SubmitAsync(
                    userId,
                    manage.RestaurantId,
                    action
                );
                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Restaurant not found.",
                    });
                }

                return Ok(result);
            }
            catch (ExtraGroupLocationException ex) when (
                ex.Code == ExtraGroupLocationService.BillingWriteNotPermittedCode
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = ex.Code,
                        message = "Only the account owner may change Additional Group Location.",
                    }
                );
            }
            catch (ExtraGroupLocationException ex) when (
                OperatorBillingLockEvaluator.IsLockCode(ex.Code)
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = ex.Code,
                        message = ex.Code,
                    }
                );
            }
            catch (ExtraGroupLocationException ex) when (
                ex.Code == ExtraGroupLocationService.BillingStatusNotActiveCode
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = ex.Code,
                        message = "Billing status does not allow this change.",
                    }
                );
            }
            catch (ExtraGroupLocationException ex) when (
                ex.Code == ExtraGroupLocationService.ExtraLocationNotGroupCode
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = ex.Code,
                    message = "Additional Group Location is only available on Group.",
                });
            }
            catch (ExtraGroupLocationException ex) when (
                ex.Code == ExtraGroupLocationService.GroupSelfServeMaxReachedCode
            )
            {
                return Conflict(new
                {
                    success = false,
                    code = ex.Code,
                    cap = ex.Cap ?? LocationCap.GroupSelfServeMax,
                    current = ex.Current ?? LocationCap.GroupSelfServeMax,
                    message = "Group self-serve location cap reached.",
                });
            }
            catch (ExtraGroupLocationException ex) when (
                ex.Code == ExtraGroupLocationService.RemoveBelowFloorCode
                || ex.Code == ExtraGroupLocationService.InvalidActionCode
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = ex.Code,
                    message = "Cannot apply this Additional Group Location change.",
                });
            }
            catch (ExtraGroupLocationException ex) when (
                IsRevolutMerchantNotReadyCode(ex.Code)
            )
            {
                return RevolutMerchantNotReady(ex.Code);
            }
        }

        [HttpPost("cancel-plan")]
        public async Task<IActionResult> CancelPlan()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            try
            {
                var result = await _billingCredits.CancelPlanAsync(
                    userId,
                    manage.RestaurantId
                );
                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Restaurant not found.",
                    });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (
                IsOperatorBillingLockCode(ex.Message) || ex.Message == "forbidden"
            )
            {
                return OperatorBillingLockForbidden(
                    ex.Message == "forbidden" ? "forbidden" : ex.Message
                );
            }
            catch (InvalidOperationException ex) when (ex.Message == "cancel_not_available")
            {
                return BadRequest(new
                {
                    success = false,
                    code = "cancel_not_available",
                    message = "Cancel plan is not available.",
                });
            }
        }

        private static bool IsOperatorBillingLockCode(string message)
        {
            return message
                is OperatorBillingLockEvaluator.SoftLock
                    or OperatorBillingLockEvaluator.Dormant
                    or OperatorBillingLockEvaluator.ChargebackRestricted
                    or OperatorBillingLockEvaluator.PastDueSendsBlocked;
        }

        private static bool IsRevolutMerchantNotReadyCode(string message)
        {
            return message
                is RevolutMerchantCreateGate.VatNotReady
                    or RevolutMerchantCreateGate.RevolutNotReady
                    or RevolutMerchantCreateGate.PlanVariationMissing;
        }

        private ObjectResult RevolutMerchantNotReady(string code)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    success = false,
                    code,
                    message = code,
                }
            );
        }

        private ObjectResult OperatorBillingLockForbidden(string code)
        {
            if (code == "forbidden")
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { success = false, message = "forbidden" }
                );
            }

            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    code,
                    message = code,
                }
            );
        }

    }
}
