namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Runs classification for one Feedback and persists the terminal result.
    /// </summary>
    public interface IFeedbackClassificationProcessor
    {
        Task ProcessAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
