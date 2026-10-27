namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Settles an awaiting-payment Shop order on the buyer's return from
    /// Revolut, without waiting for the webhook. Revolut is the source of
    /// truth. Only a completed Revolut order changes anything, through the
    /// same paid path as <c>ORDER_COMPLETED</c>. Never releases a QR slot
    /// reservation and never refunds.
    /// </summary>
    public interface IShopOrderPaymentConfirmService
    {
        /// <summary>
        /// Returns true when a completed Revolut order was applied. Returns
        /// false when nothing changed: the order is not awaiting payment, no
        /// Revolut order exists, no attempt is completed, or Revolut could not
        /// be read.
        /// </summary>
        Task<bool> ConfirmAsync(
            Guid shopOrderId,
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
