using TummlyBackend.Data;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Per-Feedback exclusive lock for guest replies (plain reply, SMS reply,
    /// reply with Offer). Held for the whole send so a repeat cannot race past
    /// the duplicate checks.
    /// </summary>
    public interface IGuestReplyLock
    {
        /// <summary>
        /// Takes the lock for <c>guest-reply:{feedbackId}</c>. Returns a handle
        /// that releases the lock on dispose, or <c>null</c> when the lock is
        /// not granted in time. SQL Server uses a session-scoped
        /// <c>sp_getapplock</c> on the context connection and does not open a
        /// database transaction. Other providers return a handle at once.
        /// </summary>
        Task<IAsyncDisposable?> TryAcquireAsync(
            ApplicationDbContext context,
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
