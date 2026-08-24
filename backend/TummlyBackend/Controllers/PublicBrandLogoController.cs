using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/public/brand-logos")]
    [AllowAnonymous]
    public class PublicBrandLogoController : ControllerBase
    {
        private readonly IAccountWorkspaceService _accountWorkspace;

        public PublicBrandLogoController(
            IAccountWorkspaceService accountWorkspace
        )
        {
            _accountWorkspace = accountWorkspace;
        }

        [HttpGet("{fileName}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetBrandLogo(string fileName)
        {
            var objectKey =
                BrandLogoRules.TryParseObjectKeyFromPublicSegment(fileName);

            if (objectKey == null)
            {
                return NotFound();
            }

            var result =
                await _accountWorkspace.OpenPublicBrandLogoAsync(objectKey);

            if (result == null)
            {
                return NotFound();
            }

            return File(result.Value.Stream, result.Value.ContentType);
        }
    }
}
