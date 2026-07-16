namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fire-and-forget enqueue of Feedback ids for async AI classification.
    /// </summary>
    public interface IFeedbackClassificationQueue
    {
        ValueTask EnqueueAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
