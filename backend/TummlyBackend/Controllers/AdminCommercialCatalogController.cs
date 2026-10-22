using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/commercial-catalog")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminCommercialCatalogController : ControllerBase
    {
        private readonly IAdminCommercialCatalogService _catalog;

        public AdminCommercialCatalogController(IAdminCommercialCatalogService catalog)
        {
            _catalog = catalog;
        }

        [HttpGet("published")]
        public async Task<IActionResult> GetPublished(CancellationToken cancellationToken)
        {
            var item = await _catalog.GetPublishedAsync(cancellationToken);
            return Ok(new { success = true, item });
        }

        [HttpGet("draft")]
        public async Task<IActionResult> GetDraft(CancellationToken cancellationToken)
        {
            var item = await _catalog.GetDraftAsync(cancellationToken);
            return Ok(new { success = true, item });
        }

        [HttpPost("draft/ensure")]
        public async Task<IActionResult> EnsureDraft(CancellationToken cancellationToken)
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var (item, error) = await _catalog.EnsureDraftAsync(
                adminId.Value,
                cancellationToken
            );
            if (error != null)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item });
        }

        [HttpPut("draft")]
        public async Task<IActionResult> SaveDraft(
            [FromBody] AdminCommercialCatalogSaveDraftRequest request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var (item, error) = await _catalog.SaveDraftAsync(
                adminId.Value,
                request,
                cancellationToken
            );
            if (error != null)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item });
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish(
            [FromBody] AdminCommercialCatalogPublishRequest request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var (item, error) = await _catalog.PublishAsync(
                adminId.Value,
                request,
                cancellationToken
            );
            if (error != null)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item });
        }

        private int? ResolveAdminId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    [ApiController]
    [Route("api/public/commercial-catalog")]
    public sealed class PublicCommercialCatalogController : ControllerBase
    {
        private readonly IAdminCommercialCatalogService _catalog;
        private readonly ICustomPlanQuoteService _quote;

        public PublicCommercialCatalogController(
            IAdminCommercialCatalogService catalog,
            ICustomPlanQuoteService quote
        )
        {
            _catalog = catalog;
            _quote = quote;
        }

        [HttpGet("custom-unit-rates")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomUnitRates(
            CancellationToken cancellationToken
        )
        {
            var item = await _catalog.GetPublicCustomUnitRatesAsync(cancellationToken);
            if (item == null)
            {
                return NotFound(new { success = false, message = "Catalog not ready." });
            }

            return Ok(new { success = true, item });
        }

        [HttpPost("custom-plan/quote")]
        [AllowAnonymous]
        public async Task<IActionResult> QuoteCustomPlan(
            [FromBody] CustomPlanQuoteRequest request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var item = await _quote.QuoteAsync(request, cancellationToken);
                return Ok(new { success = true, item });
            }
            catch (CustomPlanQuoteException ex)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        code = ex.Code,
                        message = ex.Message,
                    }
                );
            }
        }
    }
}
