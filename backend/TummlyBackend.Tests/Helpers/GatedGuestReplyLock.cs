using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Test lock. Grants every request unless <see cref="RejectAll"/> is set,
    /// which simulates a lock timeout.
    /// </summary>
    public sealed class GatedGuestReplyLock : IGuestReplyLock
    {
        public bool RejectAll { get; set; }

        public int Acquisitions { get; private set; }

        public Task<IAsyncDisposable?> TryAcquireAsync(
            ApplicationDbContext context,
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            if (RejectAll)
            {
                return Task.FromResult<IAsyncDisposable?>(null);
            }

            Acquisitions += 1;
            return Task.FromResult<IAsyncDisposable?>(PassLease.Instance);
        }

        private sealed class PassLease : IAsyncDisposable
        {
            public static PassLease Instance { get; } = new();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
