namespace TummlyBackend.Interfaces
{
    public interface IComplimentaryStarterShopOrderService
    {
        /// <summary>
        /// Ensures one £0 paid complimentary starter materials order exists for
        /// the location. Caller owns the ambient transaction and SaveChanges.
        /// Does not queue print-ready generation.
        /// </summary>
        Task<ComplimentaryStarterShopOrderResult> EnsureForLocationAsync(
            int restaurantId,
            int locationId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record ComplimentaryStarterShopOrderResult(
        Guid ShopOrderId,
        bool Created
    );
}
