namespace TummlyBackend.Models
{
    /// <summary>
    /// Shared internal-action category for Feedback recovery recorder
    /// (Record-only and Respond and record).
    /// </summary>
    public enum FeedbackInternalActionCategory
    {
        TeamBriefed = 0,
        OrderOrServiceProcessReviewed = 1,
        DeliveryIssueInvestigated = 2,
        ProductQualityChecked = 3,
        CleaningIssueAddressed = 4,
        StaffFollowUpCompleted = 5,
        OtherAction = 6,
    }
}
