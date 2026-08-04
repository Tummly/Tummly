namespace TummlyBackend.Services
{
    /// <summary>
    /// Detected Tags cannot be edited while ClassificationStatus is Pending
    /// (or otherwise not Succeeded/Failed).
    /// </summary>
    public sealed class FeedbackDetectedTagsConflictException
        : InvalidOperationException
    {
        public FeedbackDetectedTagsConflictException()
            : base(
                "Detected tags can only be edited when classification has succeeded or failed."
            )
        {
        }
    }
}
