namespace TummlyBackend.Models
{
    /// <summary>
    /// Shared reason recorded on a Feedback close-out fact.
    /// </summary>
    public enum FeedbackCloseOutReason
    {
        PositiveNoFollowUp = 0,

        DuplicateSubmission = 1,

        TestOrInvalid = 2,

        AlreadyHandledOutside = 3,

        NoAppropriateFollowUp = 4,

        Other = 5,
    }
}
