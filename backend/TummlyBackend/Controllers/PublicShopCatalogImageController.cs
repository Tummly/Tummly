using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/public/shop-catalog-images")]
    [AllowAnonymous]
    public sealed class PublicShopCatalogImageController : ControllerBase
    {
        private readonly IShopCatalogImageStorage _storage;

        public PublicShopCatalogImageController(IShopCatalogImageStorage storage)
        {
            _storage = storage;
        }

        [HttpGet("{fileName}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetImage(
            string fileName,
            CancellationToken cancellationToken
        )
        {
            var storageKey =
                ShopCatalogImageRules.TryParseObjectKeyFromPublicSegment(fileName);
            if (storageKey == null)
            {
                return NotFound();
            }

            if (!_storage.IsConfigured)
            {
                return NotFound();
            }

            try
            {
                var stream = await _storage.OpenReadAsync(
                    storageKey,
                    cancellationToken
                );
                var contentType =
                    ShopCatalogImageRules.ResolveContentType(null, fileName)
                    ?? "application/octet-stream";
                return File(stream, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }
    }
}
