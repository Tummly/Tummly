namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Promotes Restaurant.AccountType Single → Multi when the shell can show
    /// more than one Owned location (Active or Paused).
    /// </summary>
    public interface IRestaurantAccountTypePromotion
    {
        /// <summary>
        /// Ensures routing AccountType matches Active/Paused location count.
        /// Returns the restaurant AccountType after any promotion.
        /// </summary>
        Task<string> EnsureRoutingAccountTypeAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
