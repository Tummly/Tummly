using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    public sealed class ShopMaterialPackRow
    {
        [JsonPropertyName("qty")]
        public int Qty { get; set; }

        [JsonPropertyName("netPence")]
        public int NetPence { get; set; }
    }

    public static class ShopMaterialPacks
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize(IEnumerable<ShopMaterialPackRow> rows)
        {
            var cleaned = Normalize(rows);
            return JsonSerializer.Serialize(cleaned, JsonOptions);
        }

        public static IReadOnlyList<ShopMaterialPackRow> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                var rows = JsonSerializer.Deserialize<List<ShopMaterialPackRow>>(
                    json,
                    JsonOptions
                );
                return rows == null ? [] : Normalize(rows);
            }
            catch (JsonException)
            {
                return [];
            }
        }

        public static bool TryValidate(
            IEnumerable<ShopMaterialPackRow>? rows,
            out string? error
        )
        {
            var list = Normalize(rows ?? []);
            if (list.Count == 0)
            {
                error = "At least one pack tier is required.";
                return false;
            }

            if (list.Any(row => row.Qty < 1))
            {
                error = "Pack quantity must be at least 1.";
                return false;
            }

            if (list.Any(row => row.NetPence < 0))
            {
                error = "Pack price must be zero or greater.";
                return false;
            }

            if (list.Select(row => row.Qty).Distinct().Count() != list.Count)
            {
                error = "Pack quantities must be unique.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryResolve(
            string? packsJson,
            int packQty,
            out ShopMaterialPackRow? pack
        )
        {
            pack = Parse(packsJson)
                .FirstOrDefault(row => row.Qty == packQty);
            return pack != null;
        }

        public static bool TryResolve(
            IReadOnlyList<ShopMaterialPackRow> packs,
            int packQty,
            out ShopMaterialPackRow? pack
        )
        {
            pack = packs.FirstOrDefault(row => row.Qty == packQty);
            return pack != null;
        }

        public static int LowestNetPence(IEnumerable<ShopMaterialPackRow> rows) =>
            Normalize(rows).Min(row => row.NetPence);

        public static int LowestQty(IEnumerable<ShopMaterialPackRow> rows) =>
            Normalize(rows).Min(row => row.Qty);

        public static IReadOnlyList<ShopMaterialPackRow> SinglePack(
            int qty,
            int netPence
        ) =>
            [
                new ShopMaterialPackRow { Qty = qty, NetPence = netPence },
            ];

        private static List<ShopMaterialPackRow> Normalize(
            IEnumerable<ShopMaterialPackRow> rows
        ) =>
            rows
                .Select(row => new ShopMaterialPackRow
                {
                    Qty = row.Qty,
                    NetPence = row.NetPence,
                })
                .OrderBy(row => row.Qty)
                .ToList();
    }
}
