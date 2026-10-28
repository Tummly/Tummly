using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Shop
{
    /// <summary>
    /// Readiness gate for switching a SKU's Available in Shop ON. A SKU needs
    /// a published state, an image, a priced pack, a delivery price and a
    /// supplier name. Turning Available in Shop OFF is never gated. An edit to a
    /// SKU that is ON is refused if it adds a blocker the SKU did not have.
    /// </summary>
    public static class ShopSkuReadiness
    {
        public const string SkuNotReadyCode = "sku_not_ready";

        public static IReadOnlyList<string> Blockers(ShopMaterialSku sku)
        {
            var blockers = new List<string>();

            if (sku.Status != ShopMaterialSkuStatuses.Published)
            {
                blockers.Add("Publish the product.");
            }

            if (sku.Images.Count == 0)
            {
                blockers.Add("Add at least one product image.");
            }

            if (!ShopMaterialPacks.Parse(sku.PacksJson).Any(pack => pack.NetPence > 0))
            {
                blockers.Add("Add a pack with a price above £0.00.");
            }

            if (sku.DeliveryNetPence <= 0)
            {
                blockers.Add("Set a delivery price above £0.00.");
            }

            if (string.IsNullOrWhiteSpace(sku.SupplierName))
            {
                blockers.Add("Add a supplier name.");
            }

            return blockers;
        }
    }
}
