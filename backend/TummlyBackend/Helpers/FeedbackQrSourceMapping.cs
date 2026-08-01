using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator-facing QR source label for Feedback details (header + Submission).
    /// Catalog / Smart Guest → QR type label; Digital guest link → Link name.
    /// </summary>
    public static class FeedbackQrSourceMapping
    {
        private static readonly Dictionary<QrType, string> QrTypeLabels =
            new()
            {
                [QrType.CounterCard] = "Counter card",
                [QrType.PackagingSticker] = "Packaging sticker",
                [QrType.DeliveryInsert] = "Delivery insert",
                [QrType.WindowSticker] = "Window sticker",
                [QrType.SmartGuest] = "Smart Guest",
                [QrType.DigitalGuestLink] = "Digital guest link",
            };

        /// <summary>
        /// Display string for the Feedback’s QR source, or null when unknown.
        /// </summary>
        public static string? ToDisplay(QrCode? qrCode)
        {
            if (qrCode == null)
            {
                return null;
            }

            if (qrCode.QrType == QrType.DigitalGuestLink)
            {
                if (!string.IsNullOrWhiteSpace(qrCode.LinkName))
                {
                    return qrCode.LinkName.Trim();
                }

                return QrTypeLabels[QrType.DigitalGuestLink];
            }

            return QrTypeLabels.TryGetValue(qrCode.QrType, out var label)
                ? label
                : null;
        }
    }
}
