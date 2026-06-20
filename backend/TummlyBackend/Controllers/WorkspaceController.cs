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

        private readonly IConfiguration _configuration;

        public WorkspaceController(
            ApplicationDbContext context,
            IConfiguration configuration
        )
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] int locationId)
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]
                    ?.Trim().TrimEnd('/');

            var data = new
            {
                restaurantName = "Tummly Partner",
                totalGuests = 1240,
                newMembers = 21,
                feedbackCount = 342,
                offersClaimed = 187,
                offersRedeemed = 63,
                needsRecoveryCount = 6,
                topIssue = "Speed",
                qrMaterialsUrl =
                    $"{frontendBaseUrl}/api/qr/download?locationId={locationId}"
            };

            return Ok(data);
        }
    }
}