using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QrController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _configuration;

        public QrController(
            ApplicationDbContext context,
            IConfiguration configuration
        )
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetQrInfo(
            [FromQuery] int locationId
        )
        {
            var location = await ResolveOwnedLocationAsync(
                locationId
            );

            if (location == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location not found."
                });
            }

            if (location.Value.isForbidden)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message = "You do not have access to this location."
                    }
                );
            }

            var loc = location.Value.location;

            return Ok(new
            {
                success = true,
                locationName = loc.LocationName,
                owner = loc.LocalContact,
                phone = loc.LocationPhone
            });
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadQr(
            [FromQuery] int locationId
        )
        {
            var location = await ResolveOwnedLocationAsync(
                locationId
            );

            if (location == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location not found."
                });
            }

            if (location.Value.isForbidden)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message = "You do not have access to this location."
                    }
                );
            }

            var loc = location.Value.location;

            var frontendBaseUrl = GetFrontendBaseUrl();

            var qrText =
                $"{frontendBaseUrl}/scan/{loc.LinkToken}";

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] byteImage = qrCode.GetGraphic(20);

                var sanitizedLocationName =
                    SanitizeFileName(loc.LocationName);

                var fileName =
                    $"QR_{sanitizedLocationName}.png";

                return File(
                    byteImage,
                    "image/png",
                    fileName
                );
            }
        }

        /*
         =========================================
         OWNERSHIP CHECK HELPER
         =========================================
        */

        private async Task<(
            RestaurantLocation location,
            bool isForbidden
        )?> ResolveOwnedLocationAsync(int locationId)
        {
            var location = await _context.RestaurantLocations
                .Include(l => l.Restaurant)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                return null;
            }

            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                return (location, true);
            }

            if (
                location.Restaurant == null
                || location.Restaurant.OwnerUserId != userId
            )
            {
                return (location, true);
            }

            return (location, false);
        }

        /*
         =========================================
         FRONTEND BASE URL FROM CONFIG
         =========================================
        */

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]
                    ?.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new Exception(
                    "Frontend:BaseUrl is not configured."
                );
            }

            if (
                !Uri.TryCreate(
                    frontendBaseUrl,
                    UriKind.Absolute,
                    out var uri
                ) ||
                (
                    uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeHttp
                )
            )
            {
                throw new Exception(
                    "Frontend:BaseUrl must be an absolute http(s) URL."
                );
            }

            return frontendBaseUrl;
        }

        /*
         =========================================
         FILENAME SANITIZER
         =========================================
        */

        private static string SanitizeFileName(
            string name
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "location";
            }

            var sanitized = name
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "location";
            }

            return Uri.EscapeDataString(sanitized);
        }
    }
}