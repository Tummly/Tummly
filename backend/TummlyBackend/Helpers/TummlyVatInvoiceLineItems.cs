using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    public sealed record TummlyVatInvoiceLineItemDto(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("unitNetPence")] int UnitNetPence,
        [property: JsonPropertyName("vatRateBps")] int VatRateBps,
        [property: JsonPropertyName("amountNetPence")] int AmountNetPence
    );

    public static class TummlyVatInvoiceLineItems
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string Serialize(IReadOnlyList<TummlyVatInvoiceLineItemDto> lines)
        {
            return JsonSerializer.Serialize(lines, JsonOptions);
        }

        public static IReadOnlyList<TummlyVatInvoiceLineItemDto> ParseOrEmpty(
            string? json
        )
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<
                    List<TummlyVatInvoiceLineItemDto>
                >(json, JsonOptions);
                return parsed is { Count: > 0 } ? parsed : [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
