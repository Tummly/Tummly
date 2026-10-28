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

        /// <summary>
        /// Operator "in progress" bucket: Confirmed, Preparing or In production.
        /// Matches the labels in <see cref="ShopOrderFulfilmentLabels.ToDisplayLabel"/>.
        /// </summary>
        public static readonly Expression<Func<ShopOrder, bool>> OperatorInProgressExpression =
            Or(
                row => row.FulfilmentStatus == ShopFulfilmentStatuses.Processing,
                MissingExpression
            );

        /// <summary>Operator "Dispatched" bucket: carrier evidence, not yet delivered.</summary>
        public static readonly Expression<Func<ShopOrder, bool>> OperatorDispatchedExpression =
            And(
                CompleteExpression,
                Or(
                    row => row.FulfilmentStatus == ShopFulfilmentStatuses.InTransit,
                    row =>
                        row.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        && row.DeliveredAtUtc == null
                )
            );

        /// <summary>Operator "Delivered" bucket: carrier evidence and a delivery time.</summary>
        public static readonly Expression<Func<ShopOrder, bool>> OperatorDeliveredExpression =
            And(
                CompleteExpression,
                row =>
                    row.FulfilmentStatus == ShopFulfilmentStatuses.Delivered
                    && row.DeliveredAtUtc != null
            );

        private static readonly Func<ShopOrder, bool> CompleteFunc = CompleteExpression.Compile();

        private static readonly Func<ShopOrder, bool> MissingFunc = MissingExpression.Compile();

        public static bool IsComplete(ShopOrder order) => CompleteFunc(order);

        public static bool IsMissing(ShopOrder order) => MissingFunc(order);

        public static Expression<Func<ShopOrder, bool>> Or(
            Expression<Func<ShopOrder, bool>> left,
            Expression<Func<ShopOrder, bool>> right
        ) => Combine(left, right, Expression.OrElse);

        public static Expression<Func<ShopOrder, bool>> And(
            Expression<Func<ShopOrder, bool>> left,
            Expression<Func<ShopOrder, bool>> right
        ) => Combine(left, right, Expression.AndAlso);

        private static Expression<Func<ShopOrder, bool>> Combine(
            Expression<Func<ShopOrder, bool>> left,
            Expression<Func<ShopOrder, bool>> right,
            Func<Expression, Expression, BinaryExpression> join
        )
        {
            var parameter = left.Parameters[0];
            var rightBody = new ParameterRebinder(right.Parameters[0], parameter)
                .Visit(right.Body);
            return Expression.Lambda<Func<ShopOrder, bool>>(
                join(left.Body, rightBody),
                parameter
            );
        }

        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterRebinder(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node) =>
                node == _from ? _to : base.VisitParameter(node);
        }
    }
}
