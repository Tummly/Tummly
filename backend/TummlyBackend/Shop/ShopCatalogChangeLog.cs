using System.Text.Json;

namespace TummlyBackend.Shop
{
    /// <summary>
    /// Builds the audit DetailJson for catalogue changes. Only fields whose
    /// value differs appear, each with its old and new value.
    /// </summary>
    public static class ShopCatalogChangeLog
    {
        public static bool HasChanges(
            IReadOnlyDictionary<string, object?> before,
            IReadOnlyDictionary<string, object?> after
        ) => Changed(before, after).Count > 0;

        public static string Detail(
            IReadOnlyDictionary<string, object?> before,
            IReadOnlyDictionary<string, object?> after
        ) => JsonSerializer.Serialize(new { changes = Changed(before, after) });

        private static Dictionary<string, object> Changed(
            IReadOnlyDictionary<string, object?> before,
            IReadOnlyDictionary<string, object?> after
        )
        {
            var changes = new Dictionary<string, object>(StringComparer.Ordinal);
            var keys = before.Keys.Union(after.Keys, StringComparer.Ordinal);
            foreach (var key in keys)
            {
                before.TryGetValue(key, out var oldValue);
                after.TryGetValue(key, out var newValue);
                if (JsonSerializer.Serialize(oldValue) == JsonSerializer.Serialize(newValue))
                {
                    continue;
                }

                changes[key] = new { old = oldValue, @new = newValue };
            }

            return changes;
        }
    }
}
