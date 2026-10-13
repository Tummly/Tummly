using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/shop-catalog")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminShopCatalogController : ControllerBase
    {
        private readonly IAdminShopCatalogService _catalog;

        public AdminShopCatalogController(IAdminShopCatalogService catalog)
        {
            _catalog = catalog;
        }

        [HttpGet("skus")]
        public async Task<IActionResult> ListSkus(
            [FromQuery] string? status,
            CancellationToken cancellationToken
        )
        {
            var items = await _catalog.ListSkusAsync(status, cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpGet("skus/{skuId}")]
        public async Task<IActionResult> GetSku(
            string skuId,
            CancellationToken cancellationToken
        )
        {
            var item = await _catalog.GetSkuAsync(skuId, cancellationToken);
            if (item == null)
            {
                return NotFound(new { success = false, message = "Product was not found." });
            }

            return Ok(new { success = true, item });
        }

        [HttpPost("skus")]
        public async Task<IActionResult> CreateSku(
            [FromBody] AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.CreateSkuAsync(
                request,
                cancellationToken
            );
            if (error != null)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpPatch("skus/{skuId}")]
        public async Task<IActionResult> UpdateSku(
            string skuId,
            [FromBody] AdminShopCatalogSkuWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.UpdateSkuAsync(
                skuId,
                request,
                cancellationToken
            );
            if (error != null)
            {
                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpPost("skus/{skuId}/publish")]
        public async Task<IActionResult> Publish(
            string skuId,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.PublishAsync(skuId, cancellationToken);
            if (error != null)
            {
                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpPost("skus/{skuId}/unpublish")]
        public async Task<IActionResult> Unpublish(
            string skuId,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.UnpublishAsync(
                skuId,
                cancellationToken
            );
            if (error != null)
            {
                return NotFound(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpPost("skus/{skuId}/delete")]
        public async Task<IActionResult> Delete(
            string skuId,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.DeleteAsync(skuId, cancellationToken);
            if (error != null)
            {
                return NotFound(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpGet("tags")]
        public async Task<IActionResult> ListTags(CancellationToken cancellationToken)
        {
            var items = await _catalog.ListTagsAsync(cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpPost("tags")]
        public async Task<IActionResult> CreateTag(
            [FromBody] AdminShopCatalogCreateTagRequest request,
            CancellationToken cancellationToken
        )
        {
            var (tag, error) = await _catalog.CreateTagAsync(
                request.Label,
                cancellationToken
            );
            if (error != null)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = tag });
        }

        [HttpDelete("tags/{tagId:int}")]
        public async Task<IActionResult> DeleteTag(
            int tagId,
            CancellationToken cancellationToken
        )
        {
            var (ok, error) = await _catalog.DeleteTagAsync(
                tagId,
                cancellationToken
            );
            if (!ok)
            {
                if (error != null
                    && error.Contains("attached", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new { success = false, message = error });
                }

                return NotFound(new { success = false, message = error });
            }

            return Ok(new { success = true });
        }

        [HttpPost("skus/{skuId}/images")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            string skuId,
            IFormFile file,
            CancellationToken cancellationToken
        )
        {
            var (image, error) = await _catalog.UploadImageAsync(
                skuId,
                file,
                cancellationToken
            );
            if (error != null)
            {
                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = image });
        }

        [HttpPut("skus/{skuId}/images/order")]
        public async Task<IActionResult> ReorderImages(
            string skuId,
            [FromBody] AdminShopCatalogReorderImagesRequest request,
            CancellationToken cancellationToken
        )
        {
            var (sku, error) = await _catalog.ReorderImagesAsync(
                skuId,
                request.ImageIds,
                cancellationToken
            );
            if (error != null)
            {
                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, item = sku });
        }

        [HttpDelete("skus/{skuId}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(
            string skuId,
            int imageId,
            CancellationToken cancellationToken
        )
        {
            var (ok, error) = await _catalog.DeleteImageAsync(
                skuId,
                imageId,
                cancellationToken
            );
            if (!ok)
            {
                if (error != null
                    && error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true });
        }
    }
}
