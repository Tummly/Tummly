using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fail-soft Shop order lifecycle emails (confirmed + dispatched).
    /// </summary>
    public interface IShopOrderEmailNotifier
    {
        /// <summary>
        /// Sends confirmation after payment reaches Paid (incl. complimentary).
        /// </summary>
        Task TryNotifyConfirmedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Sends dispatched mail after Processing → InTransit.
        /// </summary>
        Task TryNotifyDispatchedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class NoOpShopOrderEmailNotifier : IShopOrderEmailNotifier
    {
        public static readonly NoOpShopOrderEmailNotifier Instance = new();

        private NoOpShopOrderEmailNotifier() { }

        public Task TryNotifyConfirmedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task TryNotifyDispatchedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
