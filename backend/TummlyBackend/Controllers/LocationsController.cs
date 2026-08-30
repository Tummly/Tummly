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

        public LocationsController(
            IRestaurantPermissionHelper permissions,
            IOwnedLocationInsertService insert
        )
        {
            _permissions = permissions;
            _insert = insert;
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
