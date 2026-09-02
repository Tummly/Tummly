namespace TummlyBackend.Interfaces
{
    public interface IShopOrderNumberAllocator
    {
        /// <summary>
        /// Allocates the next per-restaurant public order number as ORD-{n}.
        /// </summary>
        Task<string> AllocateNextOrderNumberAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
