using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
/// <summary>
/// Ticket 15 placeholder kept for tests that inject a no-op applier.
/// Production DI uses <see cref="RevolutOrderCompletedApplier"/>.
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
