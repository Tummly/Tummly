using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/locations")]
    [Authorize]
    public class CaptureLocationsController : ControllerBase
    {
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly ICaptureMultiLocationReadsService _reads;
        private readonly ICaptureLocationSnapshotService _snapshot;
        private readonly ICapturePreviewOptionsService _previewOptions;
        private readonly ICaptureQrLifecycleService _lifecycle;
        private readonly ICaptureThankYouOfferService _thankYouOffer;

        public CaptureLocationsController(
            IRestaurantPermissionHelper permissions,
            ICaptureMultiLocationReadsService reads,
            ICaptureLocationSnapshotService snapshot,
            ICapturePreviewOptionsService previewOptions,
            ICaptureQrLifecycleService lifecycle,
            ICaptureThankYouOfferService thankYouOffer
        )
        {
            _permissions = permissions;
            _reads = reads;
            _snapshot = snapshot;
            _previewOptions = previewOptions;
            _lifecycle = lifecycle;
            _thankYouOffer = thankYouOffer;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? q,
            [FromQuery] string[]? status,
            [FromQuery] int[]? locationIds,
            [FromQuery] string sort = "highest-qr-scans",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var set = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.Capture,
                PermissionLevel.View
            );
            var denied = set.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (locationIds is { Length: > 0 })
            {
                var named = await _permissions.AuthorizeNamedLocationIdsAsync(
                    set.LocationIds,
                    locationIds
                );
                var namedDenied = named.ToHttpResult();
                if (namedDenied != null)
                {
                    return namedDenied;
                }
            }

            try
            {
                var result = await _reads.GetLocationsAsync(
                    new CaptureLocationsQuery
                    {
                        RestaurantId = set.RestaurantId,
                        ScopedLocationIds = set.LocationIds,
                        From = from,
                        To = to,
                        Q = q,
                        Status = status,
                        LocationIds = locationIds,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                    }
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "location-scope-denied"
            )
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
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("{locationId:int}/snapshot")]
        public async Task<IActionResult> GetSnapshot(
            int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _snapshot.GetSnapshotAsync(
                    new CaptureLocationSnapshotQuery
                    {
                        LocationId = locationId,
                        From = from,
                        To = to,
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

        [HttpGet("{locationId:int}/preview-options")]
        public async Task<IActionResult> GetPreviewOptions(int locationId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _previewOptions.GetPreviewOptionsAsync(
                new CapturePreviewOptionsQuery
                {
                    LocationId = locationId,
                }
            );

            return Ok(result);
        }

        [HttpGet("{locationId:int}/thank-you-offer")]
        public async Task<IActionResult> GetThankYouOffer(int locationId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var dto = await _thankYouOffer.GetAsync(locationId);
            return Ok(new
            {
                success = true,
                thankYouOfferId = dto.ThankYouOfferId,
                thankYouOfferTitle = dto.ThankYouOfferTitle,
                thankYouOfferLive = dto.ThankYouOfferLive,
            });
        }

        [HttpPut("{locationId:int}/thank-you-offer")]
        public async Task<IActionResult> PutThankYouOffer(
            int locationId,
            [FromBody] SetCaptureThankYouOfferRequest body
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _thankYouOffer.SetAsync(
                locationId,
                body.OfferId
            );

            return result switch
            {
                CaptureThankYouOfferSetResult.Ok ok => Ok(new
                {
                    success = true,
                    thankYouOfferId = ok.Value.ThankYouOfferId,
                    thankYouOfferTitle = ok.Value.ThankYouOfferTitle,
                    thankYouOfferLive = ok.Value.ThankYouOfferLive,
                }),
                CaptureThankYouOfferSetResult.LocationNotFound => NotFound(new
                {
                    success = false,
                    message = "Location not found.",
                }),
                CaptureThankYouOfferSetResult.InvalidOffer invalid =>
                    BadRequest(new
                    {
                        success = false,
                        message = invalid.Message,
                    }),
                CaptureThankYouOfferSetResult.CapReached cap => Conflict(new
                {
                    success = false,
                    code = ActiveOfferCapGate.CapReachedCode,
                    cap = cap.Cap,
                    current = cap.Current,
                }),
                CaptureThankYouOfferSetResult.FailClosed => Conflict(new
                {
                    success = false,
                }),
                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected thank-you attach result.",
                }),
            };
        }

        [HttpPost("{locationId:int}/pause")]
        public Task<IActionResult> PauseLocationCapture(int locationId) =>
            MutateLocationCaptureAsync(
                locationId,
                _lifecycle.PauseLocationCaptureAsync
            );

        [HttpPost("{locationId:int}/activate")]
        public Task<IActionResult> ActivateLocationCapture(int locationId) =>
            MutateLocationCaptureAsync(
                locationId,
                _lifecycle.ActivateLocationCaptureAsync
            );

        private async Task<IActionResult> MutateLocationCaptureAsync(
            int locationId,
            Func<LocationCaptureLifecycleCommand, Task<QrLifecycleResult>> action
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await action(
                new LocationCaptureLifecycleCommand
                {
                    UserId = userId,
                    LocationId = locationId,
                }
            );

            return QrLifecycleHttp.ToActionResult(this, result);
        }

        private async Task<RestaurantPermissionDecision> GateLocationAsync(
            int locationId
        )
        {
            var minimum = HttpMethods.IsGet(Request.Method)
                ? PermissionLevel.View
                : PermissionLevel.Manage;
            return await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Capture,
                minimum,
                locationId
            );
        }
    }
}
