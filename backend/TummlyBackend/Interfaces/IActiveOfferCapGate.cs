using TummlyBackend.Billing;

namespace TummlyBackend.Interfaces
{
    public interface IActiveOfferCapGate
    {
        Task<ActiveOfferCapDecision> DenyIncrementAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
