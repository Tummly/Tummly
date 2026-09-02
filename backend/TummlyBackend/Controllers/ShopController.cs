using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop/catalog")]
    [Authorize]
    public class ShopController : ControllerBase
    {
        private readonly IMaterialsCatalog _catalog;
        private readonly IRestaurantPermissionHelper _permissions;

        public ShopController(
            IMaterialsCatalog catalog,
            IRestaurantPermissionHelper permissions
        )
        {
            _catalog = catalog;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetCatalog([FromQuery] int? locationId)
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId is not > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var gate = await GateLocationAsync(locationId.Value);
            var denied = gate.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                catalogVersion = _catalog.CurrentCatalogId,
                items = _catalog.BuildList(),
            });
        }

        [HttpGet("{skuId}")]
        public async Task<IActionResult> GetCatalogItem(
            string skuId,
            [FromQuery] int? locationId
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId is not > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var gate = await GateLocationAsync(locationId.Value);
            var denied = gate.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var item = _catalog.TryBuildDetail(skuId);
            if (item == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Catalog item was not found.",
                });
            }

            return Ok(new
            {
                success = true,
                item,
            });
        }

        private Task<RestaurantPermissionDecision> GateLocationAsync(int locationId)
        {
            return _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                PermissionLevel.View,
                locationId
            );
        }
    }
}
