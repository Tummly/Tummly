namespace TummlyBackend.Helpers
{
    public static class ShopCancelReasons
    {
        public const string OrderedByMistake = "ordered_by_mistake";
        public const string IncorrectQuantity = "incorrect_quantity";
        public const string IncorrectLocation = "incorrect_location";
        public const string DeliveryDetailsChanged = "delivery_details_changed";
        public const string NoLongerRequired = "no_longer_required";
        public const string Other = "other";

        private static readonly HashSet<string> ValidSlugs = new(StringComparer.Ordinal)
        {
            OrderedByMistake,
            IncorrectQuantity,
            IncorrectLocation,
            DeliveryDetailsChanged,
            NoLongerRequired,
            Other,
        };

        public static bool IsValidSlug(string? slug)
        {
            return !string.IsNullOrWhiteSpace(slug)
                && ValidSlugs.Contains(slug.Trim());
        }
    }
}
