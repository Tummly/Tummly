using TummlyBackend.Data;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Per-contact exclusive lock for Guest Form submit (WS1 concurrent submit).
    /// </summary>
    public interface IGuestFormSubmitLock
    {
        /// <summary>
        /// Tries to take an exclusive lock for
        /// <c>guest-form:{restaurantId}:{sha256 hex of contactKey}</c>.
        /// SQL Server uses <c>sp_getapplock</c> (requires an open transaction).
        /// Non-SQL Server providers return <c>true</c> (no-op).
        /// </summary>
        Task<bool> TryAcquireAsync(
            ApplicationDbContext context,
            int restaurantId,
            string contactKey,
            CancellationToken ct = default
        );
    }
}
