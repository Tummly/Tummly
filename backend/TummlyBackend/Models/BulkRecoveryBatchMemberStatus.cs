namespace TummlyBackend.Models
{
    public enum BulkRecoveryBatchMemberStatus
    {
        Pending = 0,
        Sent = 1,
        Skipped = 2,
        Failed = 3,
    }
}
