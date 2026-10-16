using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/shop-interest")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminShopInterestController : ControllerBase
    {
        private readonly IShopInterestService _interest;

        public AdminShopInterestController(IShopInterestService interest)
        {
            _interest = interest;
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
                    countsByProduct = result.CountsByProduct,
                }
            );
        }
    }
}
