using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/workspace")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkspaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] int locationId)
        {
            var data = new
            {
                restaurantName = "Tummly Partner",
                totalGuests = 1240,
                newMembers = 21,
                feedbackCount = 342,
                offersClaimed = 187,
                offersRedeemed = 63,
                needsRecoveryCount = 6,
                topIssue = "Speed"
            };

            return Ok(data);
        }
    }
}
