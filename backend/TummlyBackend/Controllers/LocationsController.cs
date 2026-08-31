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
        private readonly ILocationsLifecycleWriteService _lifecycleWrite;
        private readonly ILocationLifecycleService _lifecycle;

        public LocationsController(
            IRestaurantPermissionHelper permissions,
            IOwnedLocationInsertService insert,
            ILocationsListService list,
            ILocationsLifecycleWriteService lifecycleWrite,
            ILocationLifecycleService lifecycle
        )
        {
            _permissions = permissions;
            _insert = insert;
            _list = list;
            _lifecycleWrite = lifecycleWrite;
            _lifecycle = lifecycle;
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
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
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
                userId,
                request
            );

            return result switch
            {
                AddOwnedLocationResult.Created created => Ok(new
                {
                    success = true,
                    locationId = created.LocationId,
                }),
                AddOwnedLocationResult.InvalidRequest invalid => BadRequest(new
                {
                    success = false,
                    message = invalid.Message,
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

        [HttpPost("{locationId:int}/activate")]
        public async Task<IActionResult> ActivateDraft(int locationId)
        {
            return await RunLifecycleWriteAsync(
                locationId,
                (restaurantId, userId) =>
                    _lifecycleWrite.ActivateDraftAsync(restaurantId, locationId, userId)
            );
        }

        [HttpDelete("{locationId:int}")]
        public async Task<IActionResult> DeleteDraft(int locationId)
        {
            return await RunLifecycleWriteAsync(
                locationId,
                (restaurantId, userId) =>
                    _lifecycleWrite.DeleteDraftAsync(restaurantId, locationId, userId)
            );
        }

        [HttpPut("{locationId:int}/manager")]
        public async Task<IActionResult> SetManager(
            int locationId,
            [FromBody] SetLocationManagerRequest request
        )
        {
            return await RunLifecycleWriteAsync(
                locationId,
                (restaurantId, userId) =>
                    _lifecycleWrite.SetManagerAsync(
                        restaurantId,
                        locationId,
                        userId,
                        request.ManagerUserId
                    )
            );
        }

        [HttpPost("{locationId:int}/pause")]
        public Task<IActionResult> PauseLocation(int locationId) =>
            MutateLifecycleAsync(locationId, _lifecycle.PauseAsync);

        [HttpPost("{locationId:int}/resume")]
        public Task<IActionResult> ResumeLocation(int locationId) =>
            MutateLifecycleAsync(locationId, _lifecycle.ResumeAsync);

        [HttpPost("{locationId:int}/archive")]
        public Task<IActionResult> ArchiveLocation(int locationId) =>
            MutateLifecycleAsync(locationId, _lifecycle.ArchiveAsync);

        [HttpPost("{locationId:int}/restore")]
        public Task<IActionResult> RestoreLocation(int locationId) =>
            MutateLifecycleAsync(locationId, _lifecycle.RestoreAsync);

        private async Task<IActionResult> RunLifecycleWriteAsync(
            int locationId,
            Func<int, int, Task<LocationLifecycleWriteResult>> action
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (!decision.LocationIds.Contains(locationId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message = "You do not have access to this location.",
                    }
                );
            }

            var result = await action(decision.RestaurantId, userId);
            return result switch
            {
                LocationLifecycleWriteResult.Ok => Ok(new { success = true }),
                LocationLifecycleWriteResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Location not found.",
                }),
                LocationLifecycleWriteResult.InvalidRequest invalid => BadRequest(
                    new
                    {
                        success = false,
                        message = invalid.Message,
                    }
                ),
                LocationLifecycleWriteResult.Conflict conflict => Conflict(new
                {
                    success = false,
                    message = conflict.Message,
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected location write result.",
                    }
                ),
            };
        }

        private async Task<IActionResult> MutateLifecycleAsync(
            int locationId,
            Func<LocationLifecycleCommand, Task<LocationLifecycleResult>> action
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(
                User,
                out var userId
            );
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Locations,
                PermissionLevel.Manage,
                locationId
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await action(
                new LocationLifecycleCommand
                {
                    UserId = userId,
                    RestaurantId = decision.RestaurantId,
                    LocationId = locationId,
                }
            );

            return result.Kind switch
            {
                LocationLifecycleResultKind.Ok => Ok(new
                {
                    success = true,
                    lifecycleStatus = result.LifecycleStatus,
                }),
                LocationLifecycleResultKind.NotFound => NotFound(new
                {
                    success = false,
                    message = result.Message,
                }),
                LocationLifecycleResultKind.InvalidTransition => Conflict(new
                {
                    success = false,
                    message = result.Message,
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected location lifecycle result.",
                    }
                ),
            };
        }
    }
}
