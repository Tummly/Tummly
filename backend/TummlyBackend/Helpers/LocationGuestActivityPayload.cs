using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    public sealed class LocationGuestActivityPayload
    {
        [JsonPropertyName("tagName")]
        public string? TagName { get; set; }

        [JsonPropertyName("guestTagId")]
        public int? GuestTagId { get; set; }

        [JsonPropertyName("authorDisplayName")]
        public string? AuthorDisplayName { get; set; }

        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; set; }

        [JsonPropertyName("changedFields")]
        public IReadOnlyList<string>? ChangedFields { get; set; }

        [JsonPropertyName("fromPreference")]
        public string? FromPreference { get; set; }

        [JsonPropertyName("toPreference")]
        public string? ToPreference { get; set; }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string? Serialize(LocationGuestActivityPayload? payload)
        {
            if (payload == null)
            {
                return null;
            }

            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        public static LocationGuestActivityPayload? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<LocationGuestActivityPayload>(
                json,
                JsonOptions
            );
        }
    }
}
