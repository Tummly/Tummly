using System.Linq.Expressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// The dispatch evidence rule. A Dispatched or Delivered order needs a real
    /// carrier or supplier handoff: the carrier name, the carrier reference, the
    /// dispatch time, and the source and Admin actor that recorded it. A
    /// tracking URL is supporting information and never counts as evidence.
    /// </summary>
    public static class ShopOrderDispatchEvidence
    {
        /// <summary>
        /// Evidence is complete. Written as an expression so EF Core can filter
        /// on it; <see cref="IsComplete"/> compiles the same rule for in-memory
        /// checks. Keep the two in step (tested for parity).
        /// </summary>
        public static readonly Expression<Func<ShopOrder, bool>> CompleteExpression = row =>
            row.DispatchedAtUtc != null
            && row.DispatchedByAdminUserId != null
            && row.DispatchSource != null
            && row.DispatchSource != ""
            && row.DispatchCarrierName != null
            && row.DispatchCarrierName != ""
            && row.DispatchCarrierReference != null
            && row.DispatchCarrierReference != "";

        /// <summary>
        /// Stored status says Dispatched or Delivered, but the evidence is not
        /// complete. Admin must record evidence or correct the status.
        /// </summary>
        public static readonly Expression<Func<ShopOrder, bool>> MissingExpression = row =>
            (
                row.FulfilmentStatus == ShopFulfilmentStatuses.InTransit
                || row.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
            )
            && !(
                row.DispatchedAtUtc != null
                && row.DispatchedByAdminUserId != null
                && row.DispatchSource != null
                && row.DispatchSource != ""
                && row.DispatchCarrierName != null
                && row.DispatchCarrierName != ""
                && row.DispatchCarrierReference != null
                && row.DispatchCarrierReference != ""
            );

        private static readonly Func<ShopOrder, bool> CompleteFunc = CompleteExpression.Compile();

        private static readonly Func<ShopOrder, bool> MissingFunc = MissingExpression.Compile();

        public static bool IsComplete(ShopOrder order) => CompleteFunc(order);

        public static bool IsMissing(ShopOrder order) => MissingFunc(order);
    }
}
