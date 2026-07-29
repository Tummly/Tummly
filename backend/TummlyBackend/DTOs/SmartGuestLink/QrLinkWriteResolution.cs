using TummlyBackend.Models;

namespace TummlyBackend.DTOs.SmartGuestLink
{
    /// <summary>
    /// Tracked resolve result for guest-facing writes (feedback, STT). Only
    /// returned for Active QR codes — Paused/Archived resolve to null so
    /// callers can 404 without leaking status.
    /// </summary>
    public class QrLinkWriteResolution
    {
        public RestaurantLocation Location { get; set; } = null!;

        public int QrCodeId { get; set; }

        public QrType QrType { get; set; }
    }
}
