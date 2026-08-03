namespace TummlyBackend.Services
{
    /// <summary>
    /// Thrown when redemption code allocation exhausts every retry due to
    /// repeated unique-index collisions. Deliberately does not derive from
    /// <see cref="InvalidOperationException"/> so it is not mapped to 401
    /// Unauthorized by the generic controller catch used for auth failures.
    /// </summary>
    public sealed class FeedbackRecoveryOfferCodeAllocationException : Exception
    {
        public FeedbackRecoveryOfferCodeAllocationException()
            : base(
                "Could not allocate a unique redemption code. Please try again."
            )
        {
        }
    }
}
