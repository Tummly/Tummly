using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TummlyBackend.Data;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route banega: /api/qr
    public class QrController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QrController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Info endpoint (Data ke liye)
        [HttpGet("info")]
        public async Task<IActionResult> GetQrInfo([FromQuery] int locationId)
        {
            // Database se location dhoondein
            var location = await _context.RestaurantLocations
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                return NotFound(new { message = $"Location with ID {locationId} not found." });
            }

            return Ok(new
            {
                locationName = location.LocationName,
                owner = location.LocalContact,
                phone = location.LocationPhone
            });
        }

        // 2. Download endpoint (QR Image ke liye)
        [HttpGet("download")]
        public async Task<IActionResult> DownloadQr([FromQuery] int locationId)
        {
            var location = await _context.RestaurantLocations
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
            {
                return NotFound(new { message = $"Location with ID {locationId} not found." });
            }

            // QR Code generation
            string qrText = $"https://tummly.com/scan/{locationId}";

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] byteImage = qrCode.GetGraphic(20);
                return File(byteImage, "image/png", $"QR_{location.LocationName}.png");
            }
        }
    }
}