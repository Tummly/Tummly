namespace TummlyBackend.Helpers
{
    public static class LegalStructureOptions
    {
        public static readonly IReadOnlyDictionary<string, string> Labels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sole-trader"] = "Sole trader",
                ["partnership"] = "Partnership",
                ["limited-company"] = "Limited company (Ltd)",
                ["llp"] = "LLP",
                ["plc"] = "PLC",
                ["other"] = "Other",
            };

        public static bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return Labels.ContainsKey(value.Trim());
        }

        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            foreach (var key in Labels.Keys)
            {
                if (string.Equals(key, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            return null;
        }
    }
}
