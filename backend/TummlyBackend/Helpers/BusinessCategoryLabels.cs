namespace TummlyBackend.Helpers
{
    public static class BusinessCategoryLabels
    {
        private static readonly IReadOnlyDictionary<string, string> Labels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["takeaway"] = "Takeaway / quick-service restaurant",
                ["cafe"] = "Café / coffee shop",
                ["bakery"] = "Bakery / dessert shop",
                ["casual-dining"] = "Casual dining restaurant",
                ["food-truck"] = "Food truck / mobile food business",
                ["pub-bar"] = "Pub / bar / hospitality venue",
                ["multi-site"] = "Multi-site restaurant group",
                ["other"] = "Other",
            };

        public static string? ResolveLabel(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            return Labels.TryGetValue(category.Trim(), out var label)
                ? label
                : category.Trim();
        }
    }
}
