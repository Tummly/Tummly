using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IGuestUpsertService
    {
        /// <summary>
        /// Resolve-or-create Master Guest + Location Guest for a Feedback submit
        /// on the current DbContext (caller owns SaveChanges).
        /// </summary>
        Task<LocationGuest> ResolveOrCreateAsync(
            int restaurantId,
            int restaurantLocationId,
            string guestName,
            string guestContact,
            ContactType contactType,
            bool offersOptOut,
            DateTime? eventAt = null,
            CancellationToken cancellationToken = default
        );
    }
}
