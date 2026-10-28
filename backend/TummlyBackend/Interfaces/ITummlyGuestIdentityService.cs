using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ITummlyGuestIdentityService
    {
        /// <summary>
        /// Returns the Tummly Guest that owns a verified contact, creating a new
        /// Tummly Guest with that contact when none exists.
        /// </summary>
        Task<ProvenContact> FindOrCreateByContactAsync(
            string contactType,
            string normalizedValue,
            CancellationToken ct
        );

        /// <summary>
        /// Contact verified by the guest who owns <paramref name="tummlyGuestId"/>.
        /// If another Tummly Guest owns it, merge that guest into
        /// <paramref name="tummlyGuestId"/> (contacts, mutes union, events), delete it.
        /// Returns the contact row id.
        /// </summary>
        Task<int> LinkContactAsync(
            int tummlyGuestId,
            string contactType,
            string normalizedValue,
            CancellationToken ct
        );

        /// <summary>
        /// All Location Guests whose Master Guest NormalizedEmail / NormalizedPhone
        /// equals one of the guest's contacts, any restaurant, and that have at
        /// least one Grant of EmailMarketing or SmsMarketing in the ledger.
        /// Includes RestaurantLocation and Restaurant.
        /// </summary>
        Task<IReadOnlyList<LocationGuest>> FindLocationGuestsAsync(
            int tummlyGuestId,
            CancellationToken ct
        );
    }

    public sealed record ProvenContact(int TummlyGuestId, int ContactId);
}
