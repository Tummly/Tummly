namespace TummlyBackend.Models
{
    public abstract record LocationScope;

    public sealed record SingleLocation(string LocationId) : LocationScope;

    public sealed record AllOwnedLocations(string[] LocationIds) : LocationScope;

    public sealed record NamedSubset(string[] LocationIds) : LocationScope;

    public sealed record PeriodWindow(DateOnly Start, DateOnly End);

    public sealed record MetricPoint(decimal Current, decimal? Prior, decimal? PctDelta);

    public enum FlagSeverity
    {
        Info = 0,
        Notable = 1,
        Urgent = 2,
    }

    public sealed record Flag(
        string Code,
        string Description,
        FlagSeverity Severity,
        string[] RelatedFields
    );

    public sealed record LocationMetricPoint(
        string LocationId,
        string LocationName,
        MetricPoint Metric
    );

    public sealed record AccountSection(
        MetricPoint Covers,
        MetricPoint Revenue,
        MetricPoint AvgTicket,
        MetricPoint RepeatVisitRate,
        List<Flag> Flags,
        List<LocationMetricPoint>? RepeatVisitRateByLocation = null
    );

    public sealed record CampaignSummary(
        string Id,
        string Name,
        string Status,
        MetricPoint RedemptionRate,
        MetricPoint Engagement,
        DateOnly StartedAt,
        DateOnly? EndsAt
    );

    public sealed record CampaignsSection(
        List<CampaignSummary> Active,
        List<CampaignSummary> RecentlyEnded,
        List<Flag> Flags
    );

    public sealed record OfferSummary(
        string Id,
        string Name,
        string Status,
        MetricPoint RedemptionRate,
        DateOnly? EndsAt,
        bool HasSuccessorScheduled
    );

    public sealed record OffersSection(
        List<OfferSummary> Active,
        List<OfferSummary> ExpiringUnused,
        List<Flag> Flags
    );

    public sealed record FlaggedFeedbackItem(
        string Id,
        DateOnly ReceivedAt,
        string ShortSummary,
        bool RecoverySent
    );

    public sealed record RecurringThemeSummary(
        string Theme,
        int Count,
        DateOnly? OnsetDate
    );

    public sealed record FeedbackSection(
        MetricPoint SentimentScore,
        List<FlaggedFeedbackItem> Flagged,
        List<RecurringThemeSummary> RecurringThemes,
        int UnresolvedRecoveryCount,
        List<Flag> Flags
    );

    public sealed record GuestsSection(
        MetricPoint NewGuestCount,
        MetricPoint LapsedGuestCount,
        MetricPoint VipAtRiskCount,
        List<Flag> Flags
    );

    public sealed record CaptureSection(
        MetricPoint FunnelStartCount,
        MetricPoint FunnelCompleteCount,
        MetricPoint DropOffRate,
        string? DropOffStageFlag
    );

    public sealed record PriorAssistantAction(
        DateOnly Date,
        string ActionType,
        string ShortDescription
    );

    public sealed record RecentActionsSection(List<PriorAssistantAction> Last30Days);

    public sealed record SnapshotMeta(
        bool IsNewAccount,
        int TotalDaysOfHistory,
        string[] SectionsWithInsufficientData
    );

    public sealed record RestaurantContextSnapshot(
        string SchemaVersion,
        LocationScope Scope,
        PeriodWindow CurrentPeriod,
        PeriodWindow ComparisonPeriod,
        AccountSection Account,
        CampaignsSection Campaigns,
        OffersSection Offers,
        FeedbackSection Feedback,
        GuestsSection Guests,
        CaptureSection Capture,
        RecentActionsSection RecentActions,
        SnapshotMeta Meta
    );
}
