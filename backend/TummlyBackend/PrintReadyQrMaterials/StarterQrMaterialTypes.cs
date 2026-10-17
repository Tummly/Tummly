using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Capture / print-ready starter placement set — one QrType per Published SKU
    /// on an active recipe with <c>IncludeInComplimentaryStarter</c> (same source
    /// of truth as complimentary £0 Shop lines).
    /// </summary>
    public static class StarterQrMaterialTypes
    {
        /// <summary>
        /// Published SKUs on active complimentary-starter recipes, ordered by
        /// recipe then SKU. Shared by Capture mint and complimentary £0 orders.
        /// </summary>
        public static IQueryable<ShopMaterialSku> QueryPublishedStarterSkus(
            ApplicationDbContext context
        )
        {
            return context.ShopMaterialSkus
                .AsNoTracking()
                .Where(row =>
                    row.Status == ShopMaterialSkuStatuses.Published
                    && row.PlacementRecipe != null
                    && row.PlacementRecipe.IsActive
                    && row.PlacementRecipe.IncludeInComplimentaryStarter
                )
                .OrderBy(row => row.PlacementRecipe!.Id)
                .ThenBy(row => row.Id);
        }

        public static async Task<IReadOnlyList<QrType>> LoadAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default
        )
        {
            var keys = await QueryPublishedStarterSkus(context)
                .Select(row => row.PlacementRecipe!.PlacementKey)
                .ToListAsync(cancellationToken);

            var types = new List<QrType>(keys.Count);
            var seen = new HashSet<QrType>();
            foreach (var key in keys)
            {
                if (
                    !Enum.TryParse<QrType>(key, ignoreCase: false, out var qrType)
                    || !seen.Add(qrType)
                )
                {
                    continue;
                }

                types.Add(qrType);
            }

            return types;
        }
    }
}
