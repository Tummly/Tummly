namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator tried to add/order a Published SKU with AvailableInShop off.
    /// </summary>
    public sealed class ShopSkuNotAvailableException : InvalidOperationException
    {
        public const string ErrorCode = "sku_not_available";

        public ShopSkuNotAvailableException()
            : base("This product is not available for order.")
        {
        }
    }
}
