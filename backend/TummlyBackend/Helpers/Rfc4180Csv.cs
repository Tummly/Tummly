using System.Text;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Hand-rolled RFC 4180 CSV writer (no external CSV package).
    /// </summary>
    public static class Rfc4180Csv
    {
        public static string EscapeField(string? value)
        {
            var field = value ?? string.Empty;

            var needsQuotes =
                field.Contains(',')
                || field.Contains('"')
                || field.Contains('\r')
                || field.Contains('\n');

            if (!needsQuotes)
            {
                return field;
            }

            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        public static byte[] WriteUtf8(
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<string>> rows
        )
        {
            var builder = new StringBuilder();
            AppendRow(builder, headers);

            foreach (var row in rows)
            {
                AppendRow(builder, row);
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static void AppendRow(
            StringBuilder builder,
            IReadOnlyList<string> fields
        )
        {
            for (var i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EscapeField(fields[i]));
            }

            builder.Append("\r\n");
        }
    }
}
