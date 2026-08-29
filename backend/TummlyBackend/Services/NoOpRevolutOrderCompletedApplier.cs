using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Ticket 15 placeholder: claim store and retrieve gate only. Mint /
    /// apply payment land on ticket 16+.
    /// </summary>
    public sealed class NoOpRevolutOrderCompletedApplier
        : IRevolutOrderCompletedApplier
    {
        public Task ApplyAsync(
            RevolutOrderCompletedApplyRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
