using System.Text.Json;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// JSON array of QR code ids remembered for Activate location capture.
    /// </summary>
    public static class CaptureLocationPauseRestore
    {
        private static readonly JsonSerializerOptions JsonOptions = new();

        public static IReadOnlyList<int> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<int[]>(json, JsonOptions)
                    ?? Array.Empty<int>();
            }
            catch (JsonException)
            {
                return Array.Empty<int>();
            }
        }

        public static int Count(string? json) => Parse(json).Count;

        public static string? Serialize(IEnumerable<int> ids)
        {
            var list = ids.Distinct().OrderBy(id => id).ToList();
            if (list.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(list, JsonOptions);
        }

        public static string? Remove(string? json, int qrCodeId)
        {
            var remaining = Parse(json).Where(id => id != qrCodeId).ToList();
            return Serialize(remaining);
        }
    }
}
