namespace TummlyBackend.Interfaces
{
    public interface IComplimentaryStarterShopOrderService
    {
        /// <summary>
        /// Ensures one £0 paid complimentary starter materials order exists for
        /// the location when the Billing Account is Pilot or paid (not Free) and
        /// the location is Active. Caller owns the ambient transaction where
        /// needed. Does not queue print-ready generation.
        /// </summary>
        Task<ComplimentaryStarterShopOrderResult> EnsureForLocationAsync(
            int restaurantId,
            int locationId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Ensures complimentary starter orders for every Active owned location
        /// on a Pilot or paid Billing Account. Returns non-empty Shop order ids
        /// (created or existing) for print-ready enqueue.
        /// </summary>
        Task<IReadOnlyList<Guid>> EnsureAllActiveLocationsAsync(
            int restaurantId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record ComplimentaryStarterShopOrderResult(
        Guid ShopOrderId,
        bool Created
    );

    public sealed class NoOpComplimentaryStarterShopOrderService
        : IComplimentaryStarterShopOrderService
    {
        public static readonly NoOpComplimentaryStarterShopOrderService Instance =
            new();

        private NoOpComplimentaryStarterShopOrderService() { }

        public Task<ComplimentaryStarterShopOrderResult> EnsureForLocationAsync(
            int restaurantId,
            int locationId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new ComplimentaryStarterShopOrderResult(Guid.Empty, Created: false)
            );

        public Task<IReadOnlyList<Guid>> EnsureAllActiveLocationsAsync(
            int restaurantId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }
}
