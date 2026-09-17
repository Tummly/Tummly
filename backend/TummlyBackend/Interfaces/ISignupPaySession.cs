using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut Hosted Checkout for self-serve signup paid plans
    /// (<c>signup_plan</c> intent; restaurant does not exist yet).
    /// </summary>
    public interface ISignupPaySession
    {
        Task<string> StartAsync(
            PendingSignup pending,
            string targetPlan,
            string targetCadenceApi,
            CancellationToken cancellationToken = default
        );
    }
}
