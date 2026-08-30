namespace TummlyBackend.Interfaces
{
    public interface IIncludedPeriodMintService
    {
        /// <summary>
        /// Revolut ORDER_COMPLETED after retrieve <c>state: completed</c>.
        /// Caller applies payment to the Billing Account row first, in the same lock.
        /// </summary>
        Task<IncludedPeriodMintResult> MintOnOrderCompletedAsync(
            IncludedPeriodOrderCompletedRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Included-period job for one account: ended included-class expiry catch-up,
        /// then Annual current open slice while Active.
        /// </summary>
        Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Same as <see cref="ProcessJobForRestaurantAsync(int, CancellationToken)"/>
        /// with an explicit job clock (batch passes <c>nowUtc</c>).
        /// </summary>
        Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
            int restaurantId,
            DateTime? nowUtc,
            CancellationToken cancellationToken = default
        );
    }

    public interface IIncludedPeriodJob
    {
        Task<IncludedPeriodJobResult> ProcessAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class IncludedPeriodOrderCompletedRequest
    {
        public int RestaurantId { get; init; }

        public bool PaymentCompleted { get; init; }

        public DateTime CycleStartUtc { get; init; }

        public DateTime? NextCycleStartUtc { get; init; }

        public DateTime? CycleEndUtc { get; init; }
    }

    public sealed class IncludedPeriodMintResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<Guid> InsertedAllocationIds { get; init; } = [];

        public int ExpiryRowsWritten { get; init; }

        public static IncludedPeriodMintResult Skipped(string code)
        {
            return new IncludedPeriodMintResult
            {
                Succeeded = true,
                Code = code,
            };
        }

        public static IncludedPeriodMintResult Ok(
            IReadOnlyList<Guid> insertedAllocationIds,
            int expiryRowsWritten
        )
        {
            return new IncludedPeriodMintResult
            {
                Succeeded = true,
                InsertedAllocationIds = insertedAllocationIds,
                ExpiryRowsWritten = expiryRowsWritten,
            };
        }
    }

    public sealed class IncludedPeriodJobResult
    {
        public int Processed { get; init; }

        public int Minted { get; init; }

        public int Failed { get; init; }
    }
}
