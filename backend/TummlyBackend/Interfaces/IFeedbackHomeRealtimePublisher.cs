namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pushes thin Feedback/Home classification-terminal signals to the owner Operator.
    /// </summary>
    public interface IFeedbackHomeRealtimePublisher
    {
        Task PublishClassificationTerminalAsync(
            int userId,
            int feedbackId,
            int locationId
        );
    }
}
