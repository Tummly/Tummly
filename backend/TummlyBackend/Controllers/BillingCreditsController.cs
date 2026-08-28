using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/billing-credits")]
    [Authorize]
    public class BillingCreditsController : ControllerBase
    {
        private readonly IBillingCreditsService _billingCredits;
        private readonly IRestaurantPermissionHelper _permissions;

        public BillingCreditsController(
            IBillingCreditsService billingCredits,
            IRestaurantPermissionHelper permissions
        )
        {
            _billingCredits = billingCredits;
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
                var result = await _billingCredits.SubmitPlanChangeAsync(
                    userId,
                    manage.RestaurantId,
                    request
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
            catch (InvalidOperationException ex) when (ex.Message == "invalid-target")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Pilot is not a valid plan-change target.",
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "forbidden")
            {
                return Forbid();
            }
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
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
                page,
                pageSize
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
                return StatusCode(statusCode, new
                {
                    success = false,
                    message = errorMessage,
                });
            }

            return Ok(response);
        }

        [HttpPost("extra-location/add")]
        public async Task<IActionResult> AddExtraGroupLocation()
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
                var result = await _billingCredits.AddExtraGroupLocationAsync(
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
            catch (InvalidOperationException ex) when (ex.Message == "forbidden")
            {
                return Forbid();
            }
            catch (InvalidOperationException ex) when (ex.Message == "not-group-plan")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Additional Group Location is only available on Group.",
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "location-cap-reached")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The Group location cap has been reached.",
                });
            }
        }

        [HttpPost("extra-location/remove")]
        public async Task<IActionResult> RemoveExtraGroupLocation()
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
                var result = await _billingCredits.RemoveExtraGroupLocationAsync(
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
            catch (InvalidOperationException ex) when (ex.Message == "forbidden")
            {
                return Forbid();
            }
            catch (InvalidOperationException ex) when (ex.Message == "not-group-plan")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Additional Group Location is only available on Group.",
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "remove-below-floor")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot remove an extra Location below the included allowance.",
                });
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
            catch (InvalidOperationException ex) when (ex.Message == "forbidden")
            {
                return Forbid();
            }
            catch (InvalidOperationException ex) when (ex.Message == "pilot-cancel-not-allowed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cancel plan is not available on Pilot.",
                });
            }
        }

    }
}
