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

        public CaptureOverviewController(
            ICaptureMultiLocationReadsService reads
        )
        {
            _reads = reads;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverview(
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

            try
            {
                var result = await _reads.GetOverviewAsync(
                    new CaptureOverviewQuery
                    {
                        OwnerUserId = userId,
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
