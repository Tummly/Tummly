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

        /// <param name="scope">"active" (default) lists active rows only; "all" adds withdrawn rows.</param>
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? scope,
            CancellationToken cancellationToken
        )
        {
            var normalized = (scope ?? "active").Trim().ToLowerInvariant();
            if (normalized is not ("active" or "all"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "scope must be active or all.",
                });
            }

            var result = await _interest.ListForAdminAsync(
                includeWithdrawn: normalized == "all",
                cancellationToken: cancellationToken
            );
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
