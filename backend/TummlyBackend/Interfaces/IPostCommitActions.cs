namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Work that must run only after the outermost database transaction
    /// commits (emails, for example). The webhook calls
    /// <see cref="BeginDeferral"/> before it opens its transaction, so code
    /// that joins that transaction queues its side effects instead of sending
    /// them. The webhook then calls <see cref="RunAsync"/> after the commit, or
    /// <see cref="Discard"/> when the transaction does not commit.
    /// </summary>
    public interface IPostCommitActions
    {
        bool IsDeferring { get; }

        void BeginDeferral();

        void Enqueue(Func<CancellationToken, Task> action);

        /// <summary>
        /// Runs and clears the queued actions and ends deferral. A failing
        /// action is logged and does not stop the others.
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops queued actions and ends deferral.
        /// </summary>
        void Discard();
    }
}
