namespace TummlyBackend.Models
{
    /// <summary>
    /// Duplicate-premises review state of a Location. PendingReview and
    /// Rejected block activation, Pilot start, complimentary Starter Kit
    /// fulfilment and active QR capacity until an Admin clears the Location.
    /// Stored as an int. Do not renumber.
    /// </summary>
    public enum PremisesReviewStatus
    {
        None = 0,

        PendingReview = 1,

        Cleared = 2,

        Rejected = 3,
    }
}
