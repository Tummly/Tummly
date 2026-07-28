namespace TummlyBackend.Models
{
    /// <summary>
    /// Catalog kind of a per-location QR code. Default set minted for every
    /// Owned location at Guest Loop provisioning (four placement types plus
    /// Smart Guest). See CONTEXT.md "QR type".
    /// </summary>
    public enum QrType
    {
        CounterCard = 0,

        PackagingSticker = 1,

        DeliveryInsert = 2,

        WindowSticker = 3,

        SmartGuest = 4,
    }
}
