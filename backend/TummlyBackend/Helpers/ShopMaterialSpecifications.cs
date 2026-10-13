using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    public sealed class ShopMaterialSpecificationRow
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    public static class ShopMaterialSpecifications
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize(
            IEnumerable<ShopMaterialSpecificationRow> rows
        )
        {
            var cleaned = rows
                .Select(row => new ShopMaterialSpecificationRow
                {
                    Label = (row.Label ?? string.Empty).Trim(),
                    Value = (row.Value ?? string.Empty).Trim(),
                })
                .Where(row =>
                    row.Label.Length > 0 || row.Value.Length > 0
                )
                .ToList();
            return JsonSerializer.Serialize(cleaned, JsonOptions);
        }

        public static IReadOnlyList<ShopMaterialSpecificationRow> Parse(
            string? json
        )
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                var rows = JsonSerializer.Deserialize<
                    List<ShopMaterialSpecificationRow>
                >(json, JsonOptions);
                return rows ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
