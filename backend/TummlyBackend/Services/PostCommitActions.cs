using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Scoped queue of work to run after the outermost commit. See
    /// <see cref="IPostCommitActions"/>.
    /// </summary>
    public sealed class PostCommitActions : IPostCommitActions
    {
        private readonly List<Func<CancellationToken, Task>> _actions = new();
        private readonly ILogger<PostCommitActions> _logger;

        public PostCommitActions(ILogger<PostCommitActions> logger)
        {
            _logger = logger;
        }

        public bool IsDeferring { get; private set; }

        public void BeginDeferral() => IsDeferring = true;

        public void Enqueue(Func<CancellationToken, Task> action) =>
            _actions.Add(action);

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var pending = _actions.ToList();
            Discard();
            foreach (var action in pending)
            {
                try
                {
                    await action(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The transaction is committed; a failed email must not undo it.
                    _logger.LogError(ex, "A post-commit action failed.");
                }
            }
        }

        public void Discard()
        {
            _actions.Clear();
            IsDeferring = false;
        }
    }
}
