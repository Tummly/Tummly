using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/locations")]
    [Authorize]
    public class LocationsController : ControllerBase
    {
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IOwnedLocationInsertService _insert;
        private readonly ILocationsListService _list;

        public LocationsController(
            IRestaurantPermissionHelper permissions,
            IOwnedLocationInsertService insert,
            ILocationsListService list
        )
        {
            _permissions = permissions;
            _insert = insert;
            _list = list;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(
            [FromQuery] string? q = null,
            [FromQuery] string[]? lifecycle = null,
            [FromQuery] string[]? setup = null,
            [FromQuery] string[]? city = null,
            [FromQuery] string sort = "name-asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.View
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _list.GetListAsync(
                    new LocationsListQuery
                    {
                        RestaurantId = decision.RestaurantId,
                        LocationIds = decision.LocationIds,
                        Q = q,
                        Lifecycle = lifecycle ?? [],
                        Setup = setup ?? [],
                        City = city ?? [],
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                    }
                );
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddOwnedLocation(
            [FromBody] AddOwnedLocationRequest request
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _insert.AddAsync(
                decision.RestaurantId,
                request
            );

            return result switch
            {
                AddOwnedLocationResult.Created created => Ok(new
                {
                    success = true,
                    locationId = created.LocationId,
                }),
                AddOwnedLocationResult.CapReached cap => Conflict(new
                {
                    success = false,
                    code = LocationCap.CapReachedCode,
                    cap = cap.Cap,
                    current = cap.Current,
                }),
                AddOwnedLocationResult.FailClosed => Conflict(new
                {
                    success = false,
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected add-location result.",
                    }
                ),
            };
        }
    }
}
