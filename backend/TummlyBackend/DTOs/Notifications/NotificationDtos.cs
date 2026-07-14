namespace TummlyBackend.DTOs.Notifications
{
    public class ProduceNotificationRequest
    {
        public int UserId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string? CtaLabel { get; set; }

        public string? CtaHref { get; set; }

        public string? DedupeKey { get; set; }
    }

    public enum ProduceNotificationStatus
    {
        Created,
        NoOpPreferenceOff,
        NoOpDedupe,
        RejectedUnknownType,
        RejectedInvalidCta,
    }

    public class NotificationDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Category { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public string? CtaLabel { get; set; }

        public string? CtaHref { get; set; }

        public string? Capability { get; set; }

        public string? DedupeKey { get; set; }
    }

    public class ProduceNotificationResult
    {
        public ProduceNotificationStatus Status { get; set; }

        public NotificationDto? Notification { get; set; }
    }

    public class NotificationPreferencesDto
    {
        public bool ProductUpdates { get; set; } = true;

        public bool AccountNotices { get; set; } = true;

        public bool WeeklyBriefReminders { get; set; } = true;

        public bool TipsAndPlaybooks { get; set; } = true;

        public bool CampaignAndReportUpdates { get; set; } = true;
    }

    /// <summary>
    /// Filter for mark-all-visible. Null category = All tab; UnreadOnly for Unread tab.
    /// </summary>
    public class NotificationListFilter
    {
        public string? Category { get; set; }

        public bool UnreadOnly { get; set; }
    }

    /// <summary>
    /// Result of shell-connect seed ensure. Newly created seeds toast via SignalR;
    /// <see cref="ReToast"/> lists existing unread seeds that need a client re-toast.
    /// </summary>
    public class EnsureSeedsResult
    {
        public IReadOnlyList<NotificationDto> ReToast { get; set; } = [];
    }
}
