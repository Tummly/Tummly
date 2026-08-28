using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    }
}
