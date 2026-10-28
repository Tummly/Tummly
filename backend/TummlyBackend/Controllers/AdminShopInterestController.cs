using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Shop;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/shop-interest")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminShopInterestController : ControllerBase
    {
        private readonly IShopInterestService _interest;
        private readonly IAdminShopInterestProductService _products;

        public AdminShopInterestController(
            IShopInterestService interest,
            IAdminShopInterestProductService products
        )
        {
            _interest = interest;
            _products = products;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var result = await _interest.ListForAdminAsync(cancellationToken);
            return Ok(
                new
                {
                    success = true,
                    items = result.Items,
                    counts = result.CountsByProduct,
                    // Same list under the name the first admin screen reads.
                    countsByProduct = result.CountsByProduct,
                }
            );
        }

        [HttpGet("counts/export")]
        public async Task<IActionResult> ExportCounts(CancellationToken cancellationToken)
        {
            var (fileName, content) = await _interest.ExportCountsCsvAsync(
                cancellationToken
            );
            return File(content, "text/csv", fileName);
        }

        [HttpDelete("registrations/{id:int}")]
        public async Task<IActionResult> RemoveRegistration(
            int id,
            CancellationToken cancellationToken
        )
        {
            var staffId = GetStaffId();
            if (staffId == null)
            {
                return Unauthorized(new { success = false, message = "Invalid token." });
            }

            var adminIdentity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(adminIdentity))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = "Admin identity could not be resolved from the current session.",
                    }
                );
            }

            var (ok, error) = await _interest.RemoveRegistrationAsync(
                id,
                staffId.Value,
                adminIdentity,
                cancellationToken
            );
            if (!ok)
            {
                return NotFound(new { success = false, message = error });
            }

            return Ok(new { success = true });
        }

        [HttpGet("products")]
        public async Task<IActionResult> ListProducts(
            CancellationToken cancellationToken
        )
        {
            var items = await _products.ListProductsAsync(cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpPatch("products/{productKey}")]
        public async Task<IActionResult> UpdateProduct(
            string productKey,
            [FromBody] AdminShopInterestProductWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            var (product, error) = await _products.UpdateProductAsync(
                productKey,
                request,
                cancellationToken
            );
            if (error != null)
            {
                return MapError(error);
            }

            return Ok(new { success = true, item = product });
        }

        [HttpPost("products/{productKey}/images")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            string productKey,
            IFormFile file,
            CancellationToken cancellationToken
        )
        {
            var (image, error) = await _products.UploadImageAsync(
                productKey,
                file,
                cancellationToken
            );
            if (error != null)
            {
                return MapError(error);
            }

            return Ok(new { success = true, item = image });
        }

        [HttpDelete("products/{productKey}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(
            string productKey,
            int imageId,
            CancellationToken cancellationToken
        )
        {
            var (ok, error) = await _products.DeleteImageAsync(
                productKey,
                imageId,
                cancellationToken
            );
            if (!ok)
            {
                return MapError(error ?? "Could not remove image.");
            }

            return Ok(new { success = true });
        }

        [HttpPost("products/{productKey}/promote")]
        public async Task<IActionResult> Promote(
            string productKey,
            [FromBody] AdminShopInterestPromoteRequest request,
            CancellationToken cancellationToken
        )
        {
            var (product, error) = await _products.PromoteAsync(
                productKey,
                request,
                cancellationToken
            );
            if (error != null)
            {
                return MapError(error);
            }

            return Ok(new { success = true, item = product });
        }

        [HttpPost("products/{productKey}/demote")]
        public async Task<IActionResult> DemoteByProductKey(
            string productKey,
            CancellationToken cancellationToken
        )
        {
            var (product, error) = await _products.DemoteByProductKeyAsync(
                productKey,
                cancellationToken
            );
            if (error != null)
            {
                return MapError(error);
            }

            return Ok(new { success = true, item = product });
        }

        [HttpPost("demote-by-sku/{skuId}")]
        public async Task<IActionResult> DemoteBySkuId(
            string skuId,
            CancellationToken cancellationToken
        )
        {
            var (product, error) = await _products.DemoteBySkuIdAsync(
                skuId,
                cancellationToken
            );
            if (error != null)
            {
                return MapError(error);
            }

            return Ok(new { success = true, item = product });
        }

        private int? GetStaffId()
        {
            var staffIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (
                string.IsNullOrEmpty(staffIdClaim)
                || !int.TryParse(staffIdClaim, out var staffId)
            )
            {
                return null;
            }

            return staffId;
        }

        private IActionResult MapError(string error)
        {
            if (error == ShopQrPathRules.SkuHasNoQrPathCode)
            {
                return Conflict(
                    new
                    {
                        success = false,
                        code = error,
                        message = ShopQrPathRules.SkuHasNoQrPathMessage,
                    }
                );
            }

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { success = false, message = error });
            }

            return BadRequest(new { success = false, message = error });
        }
    }
}
