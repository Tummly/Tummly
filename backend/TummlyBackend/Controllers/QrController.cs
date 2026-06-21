using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QrController : ControllerBase
    {
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IOwnedLocationService _ownedLocation;

        public QrController(
            ISmartGuestLinkService smartGuestLink,
            IOwnedLocationService ownedLocation
        )
        {
            _smartGuestLink = smartGuestLink;
            _ownedLocation = ownedLocation;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetQrInfo(
            [FromQuery] int locationId
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

            var loc = ownedLocation.Location!;

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

            var loc = ownedLocation.Location!;

            var qrText = _smartGuestLink.BuildGuestUrl(loc.LinkToken);

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
