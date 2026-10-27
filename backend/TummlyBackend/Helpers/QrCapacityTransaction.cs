using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Data;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Opens the transaction that holds the <c>BillingAccounts</c> lock while a
    /// QR capacity check and its write run (same pattern as Capture).
    /// </summary>
    public static class QrCapacityTransaction
    {
        /// <summary>
        /// Starts a transaction on SQL Server. Returns null when one is
        /// already open (the caller joins it) or when the provider has no
        /// row lock to hold.
        /// </summary>
        public static async Task<IDbContextTransaction?> BeginIfNeededAsync(
            ApplicationDbContext db,
            CancellationToken ct
        )
        {
            if (db.Database.CurrentTransaction != null)
            {
                return null;
            }

            if (!db.Database.IsSqlServer())
            {
                return null;
            }

            return await db.Database.BeginTransactionAsync(ct);
        }
    }
}
