namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator follow-up lifecycle for one Feedback. Distinct from
    /// ClassificationStatus and from derived Needs attention.
    /// </summary>
    public enum FeedbackWorkflowStatus
    {
        New = 0,

        InProgress = 1,

        Resolved = 2,
    }
}
