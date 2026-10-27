using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Result of a QR capacity check. <paramref name="Limit"/> is the
    /// per-location cap shown in the "QR code limit reached" dialog.
    /// <paramref name="EntitlementsAvailable"/> is false when the plan could
    /// not be resolved (the order needs a new slot and nothing can be said
    /// about the cap). <paramref name="OrderPayable"/> is false when the order
    /// is no longer awaiting payment once the billing-account lock is held
    /// (a sweeper or webhook moved it); nothing was written.
    /// </summary>
    public sealed record QrCapacityCheck(
        bool Ok,
        int Limit,
        bool EntitlementsAvailable = true,
        bool OrderPayable = true
    );

    /// <summary>
    /// Shop checkout hold on QR capacity (spec 4.4). One reservation per
    /// (order, QR type) for each type that needs a new slot at the location.
    /// </summary>
    public interface IShopQrSlotReservationService
    {
        /// <summary>
        /// Place-time check. Takes no lock past the call and writes nothing.
        /// </summary>
        Task<QrCapacityCheck> CheckForPlaceAsync(
            int restaurantId,
            int locationId,
            IReadOnlyCollection<QrType> types,
            CancellationToken ct
        );

        /// <summary>
        /// Pay-time reservation. Opens its own transaction (unless one is
        /// open) and takes the billing-account lock. Writes one
        /// <c>open</c> reservation per type that needs a slot. A failed check
        /// writes nothing.
        /// </summary>
        Task<QrCapacityCheck> ReserveForPayAsync(
            ShopOrder order,
            CancellationToken ct
        );

        /// <summary>
        /// Sets the order's <c>open</c> reservations to <c>released</c>.
        /// </summary>
        Task ReleaseForOrderAsync(Guid shopOrderId, CancellationToken ct);

        /// <summary>
        /// Converts the paid order's reservations to QR codes. Runs inside the
        /// caller's transaction after the billing-account lock. Returns the QR
        /// code ids. Held reservations are not checked against the limit. A
        /// type with no held reservation is checked; when the limit is full the
        /// order gets review reason
        /// <see cref="ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment"/>
        /// and that type gets no code.
        /// </summary>
        Task<IReadOnlyList<int>> ConvertForPaidOrderAsync(
            ShopOrder order,
            CancellationToken ct
        );
    }
}
