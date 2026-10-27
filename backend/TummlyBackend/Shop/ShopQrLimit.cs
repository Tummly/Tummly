namespace TummlyBackend.Shop
{
    /// <summary>
    /// Error codes and copy for the Shop QR capacity checks at place and pay.
    /// </summary>
    public static class ShopQrLimit
    {
        public const string ErrorCode = "qr_limit_reached";

        public const string Message =
            "This order needs a new QR code and your plan's active QR code limit is reached.";

        public const string EntitlementsUnavailableCode =
            "plan_entitlements_unavailable";

        public const string EntitlementsUnavailableMessage =
            "Plan entitlements are unavailable.";
    }
}
