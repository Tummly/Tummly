using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/overview")]
    [Authorize]
    public class CaptureOverviewController : ControllerBase
    {
        private readonly ICaptureMultiLocationReadsService _reads;
        private readonly IRestaurantPermissionHelper _permissions;

        public CaptureOverviewController(
            ICaptureMultiLocationReadsService reads,
            IRestaurantPermissionHelper permissions
        )
        {
            _reads = reads;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
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

            try
            {
                var result = await _reads.GetOverviewAsync(
                    new CaptureOverviewQuery
                    {
                        RestaurantId = set.RestaurantId,
                        ScopedLocationIds = set.LocationIds,
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
    }
}
