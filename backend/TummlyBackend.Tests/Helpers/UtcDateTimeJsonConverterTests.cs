using System.Text.Json;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class UtcDateTimeJsonConverterTests
    {
        private static readonly JsonSerializerOptions Options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new UtcDateTimeJsonConverter());
            options.Converters.Add(new UtcNullableDateTimeJsonConverter());
            return options;
        }

        [Fact]
        public void Write_EmitsZSuffixForUnspecifiedUtcWallClock()
        {
            var value = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Unspecified);

            var json = JsonSerializer.Serialize(value, Options);

            Assert.Equal("\"2026-07-12T12:00:00.0000000Z\"", json);
        }

        [Fact]
        public void Write_Nullable_EmitsNull()
        {
            DateTime? value = null;

            var json = JsonSerializer.Serialize(value, Options);

            Assert.Equal("null", json);
        }

        [Fact]
        public void RoundTrip_PreservesUtcInstant()
        {
            var original = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);

            var json = JsonSerializer.Serialize(original, Options);
            var parsed = JsonSerializer.Deserialize<DateTime>(json, Options);

            Assert.Equal(original, parsed);
            Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        }
    }
}
