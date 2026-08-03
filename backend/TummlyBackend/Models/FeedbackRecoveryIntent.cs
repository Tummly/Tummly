namespace TummlyBackend.Models
{
    /// <summary>
    /// Feedback recovery entry intent recorded on guest-response, internal-action,
    /// and recovery-completion facts.
    /// </summary>
    public enum FeedbackRecoveryIntent
    {
        RespondToGuest = 0,
        RecordInternalActionOnly = 1,
        RespondAndRecordInternalAction = 2,
    }
}
