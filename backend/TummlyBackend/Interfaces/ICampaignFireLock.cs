using TummlyBackend.Data;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Per-Campaign exclusive claim for a fire / send run. Two app instances or
    /// a manual fire and the background sweep must not both send the same
    /// Campaign.
    /// </summary>
    public interface ICampaignFireLock
    {
        /// <summary>
        /// Takes the claim for <c>campaign-fire:{campaignId}</c> without waiting.
        /// Returns a handle that releases the claim on dispose, or <c>null</c>
        /// when another run holds it. SQL Server uses a session-scoped
        /// <c>sp_getapplock</c> on the context connection. Other providers return
        /// a handle at once.
        /// </summary>
        Task<IAsyncDisposable?> TryAcquireAsync(
            ApplicationDbContext context,
            int campaignId,
            CancellationToken cancellationToken = default
        );
    }
}
