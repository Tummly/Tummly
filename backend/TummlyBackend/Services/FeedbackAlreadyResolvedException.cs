namespace TummlyBackend.Services
{
    /// <summary>
    /// Thrown when an operator attempts Feedback close-out while already Resolved.
    /// </summary>
    public sealed class FeedbackAlreadyResolvedException : InvalidOperationException
    {
        public FeedbackAlreadyResolvedException()
            : base(
                "Feedback is already Resolved. Reopen before closing out again."
            )
        {
        }
    }
}
