using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/guests")]
    [Authorize]
    public class GuestsController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IGuestsListService _guestsList;

        public GuestsController(
            IOwnedLocationService ownedLocation,
            IGuestsListService guestsList
        )
        {
            _ownedLocation = ownedLocation;
            _guestsList = guestsList;
        }

        [HttpGet]
        public async Task<IActionResult> GetGuests(
            [FromQuery] int locationId,
            [FromQuery] string smartGroup = "all-guests",
            [FromQuery] string? q = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _guestsList.GetListAsync(
                    locationId,
                    ownedLocation.Location!.LocationName,
                    smartGroup,
                    q,
                    sort,
                    page,
                    pageSize
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
