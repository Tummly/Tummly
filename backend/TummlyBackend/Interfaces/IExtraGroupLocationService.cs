using TummlyBackend.DTOs.BillingCredits;

namespace TummlyBackend.Interfaces
{
    public interface IExtraGroupLocationService
    {
        Task<ExtraLocationResultDto?> SubmitAsync(
            int userId,
            int restaurantId,
            string action,
            string? idempotencyKey = null
        );

        /// <summary>
        /// After Revolut <c>ORDER_COMPLETED</c> for an extra-Location add:
        /// increment count, move Contracted Pricebook, clear slot, grant floor
        /// <c>plan_migration</c> from <c>credits_monthly_added</c>.
        /// </summary>
        Task<ExtraGroupLocationApplyResult> ApplyAddOnOrderCompletedAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Apply a scheduled extra remove: decrement <c>PaidExtraLocationCount</c>
        /// by 1, clear the slot, write no debit this period.
        /// </summary>
        Task<ExtraGroupLocationApplyResult> ApplyScheduledRemoveAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class ExtraGroupLocationApplyResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<Guid> InsertedAllocationIds { get; init; } = [];

        public static ExtraGroupLocationApplyResult Ok(
            IReadOnlyList<Guid>? inserted = null
        ) =>
            new()
            {
                Succeeded = true,
                InsertedAllocationIds = inserted ?? [],
            };

        public static ExtraGroupLocationApplyResult Fail(string code) =>
            new() { Succeeded = false, Code = code };
    }
}
