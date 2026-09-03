namespace TummlyBackend.Models
{
    /// <summary>
    /// Catalog kind of a per-location QR code. Default set minted for every
    /// Owned location at Guest Loop provisioning (four placement types plus
    /// Smart Guest). Digital guest link is operator-created (many per location).
    /// See CONTEXT.md "QR type".
    /// </summary>
    public enum QrType
    {
        CounterCard = 0,

        PackagingSticker = 1,

        DeliveryInsert = 2,

        WindowSticker = 3,

        SmartGuest = 4,

        DigitalGuestLink = 5,

        /// <summary>
        /// Receipt sticker placement minted on Shop fulfilment (ticket 20).
        /// </summary>
        ReceiptSticker = 6,
    }
}

