namespace TummlyBackend.Models
{
    public class NotificationPreference
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public bool ProductUpdates { get; set; } = true;

        public bool AccountNotices { get; set; } = true;

        public bool WeeklyBriefReminders { get; set; } = true;

        public bool TipsAndPlaybooks { get; set; } = true;

        public bool CampaignAndReportUpdates { get; set; } = true;
    }
}
