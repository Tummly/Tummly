using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// SQL Server returns UTC wall-clock values as <see cref="DateTimeKind.Unspecified"/>.
    /// Emit them with a <c>Z</c> suffix so browsers do not treat them as local time.
    /// </summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var value = reader.GetDateTime();
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime value,
            JsonSerializerOptions options
        )
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
            writer.WriteStringValue(utc.ToString("O"));
        }
    }

    public sealed class UtcNullableDateTimeJsonConverter
        : JsonConverter<DateTime?>
    {
        private static readonly UtcDateTimeJsonConverter Inner = new();

        public override DateTime? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return Inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime? value,
            JsonSerializerOptions options
        )
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            Inner.Write(writer, value.Value, options);
        }
    }
}
