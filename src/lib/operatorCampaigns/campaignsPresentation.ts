/** Figma Campaigns overview — nodes 3462:61945 (header) / 3462:61952 (summary) / 4026:45443 (true-empty). */

import type {
  OperatorCampaignsListEmptyStateKind,
  OperatorCampaignsListViewId,
  OperatorCampaignsSortId,
} from "@/types/operatorCampaigns"

export const CAMPAIGNS_PAGE_COPY = {
  title: "Campaigns",
  subtitle:
    "Send permission-based email or SMS campaigns to eligible guests and track what happens next.",
  /** Campaigns list section title (true-empty card). */
  listSectionTitle: "Campaigns",
  listSectionSubtitle:
    "Review drafts, scheduled sends and campaign performance for the selected locations.",
  createCampaign: "Create campaign",
  useTemplate: "Use a template",
  trueEmptyTitle: "No campaigns yet",
  trueEmptyHelper:
    "Create your first permission-based campaign or start with a template.",
  filterSearchTitle: "No results from filters",
  filterSearchHelper: "Try removing a filter or changing your search.",
  viewAllCampaigns: "View all campaigns",
  clearAllFilters: "Clear all filters",
  searchPlaceholder: "Search campaigns, audiences or offers",
  filtersLabel: "Filters",
  sortLabel: "Sort: Recent activity",
  summaryTitle: "Campaign summary",
  summarySubtitle:
    "See audience readiness, campaign activity and attributed outcomes for the selected location and reporting period.",
  marketingEligibleLabel: "Marketing eligible",
  marketingEligibleDescription:
    "Unique guests with a permitted marketing basis and at least one reachable email or mobile channel.",
  campaignsInFlightLabel: "Campaigns in flight",
  campaignsInFlightDescription: "2 scheduled · 1 sending",
  messagesSentLabel: "Messages sent",
  messagesSentDescription: "1,510 email · 332 SMS",
  campaignAttributedRedemptionsLabel: "Campaign-attributed redemptions",
  campaignAttributedRedemptionsDescription: "29 of 186 campaign offer claim",
  recommendationTitle: "Recommended next step",
  recommendationSubtitle:
    "A practical campaign opportunity based on your recent Guest Loop activity.",
  recommendationEmptyCopy:
    "A recommended action will appear once there is enough guest activity.",
  recommendationFailCopy:
    "Could not load a recommendation. Please try again.",
  recommendationRetry: "Retry",
  recommendationReviewDraft: "Review campaign draft",
  recommendationViewAudience: "View eligible audience",
  recommendationNotNow: "Not now",
  recommendationOpportunityLabel: "Opportunity",
  recommendationEligibleAudienceLabel: "Eligible audience",
  recommendationWhyLabel: "Why this is recommended",
  recommendationWhyIntro: "These guests:",
  recommendationSuggestedChannelLabel: "Suggested channel",
  recommendationEstimatedUsageLabel: "Estimated usage",
  recommendationAudienceDisclaimer:
    "These counts are live Guest Loop signals for this location — not full Campaign eligibility.",
  recommendationAudienceClose: "Close",
} as const

export const CAMPAIGNS_MESSAGING_USAGE_ANCHOR_ID = "campaigns-messaging-usage"

export const CAMPAIGNS_HELP_ARTICLE_SLUG = "campaigns"

export const OPERATOR_CAMPAIGNS_DEFAULT_SORT_ID: OperatorCampaignsSortId =
  "recent-activity"

export const OPERATOR_CAMPAIGNS_SORT_LABELS: Record<
  OperatorCampaignsSortId,
  string
> = {
  "recent-activity": "Recent activity",
  "send-date": "Send date",
  "name-az": "Name A–Z",
}

export const OPERATOR_CAMPAIGNS_SORT_OPTIONS: readonly [
  OperatorCampaignsSortId,
  string,
][] = (
  Object.entries(OPERATOR_CAMPAIGNS_SORT_LABELS) as [
    OperatorCampaignsSortId,
    string,
  ][]
)

export const CAMPAIGNS_HEADER_OVERFLOW_ACTIONS = [
  { id: "view-messaging-usage", label: "View messaging usage" },
  { id: "campaign-help", label: "Campaign help" },
] as const

export const OPERATOR_CAMPAIGNS_LIST_VIEW_LABELS: Record<
  OperatorCampaignsListViewId,
  string
> = {
  all: "All",
  "needs-attention": "Needs attention",
  drafts: "Drafts",
  "in-flight": "In flight",
  sent: "Sent",
}

export const OPERATOR_CAMPAIGNS_LIST_VIEW_ORDER: readonly OperatorCampaignsListViewId[] =
  ["all", "needs-attention", "drafts", "in-flight", "sent"] as const

export const OPERATOR_CAMPAIGNS_VIEW_SCOPED_EMPTY_COPY: Record<
  Exclude<OperatorCampaignsListViewId, "all">,
  { title: string; helper: string }
> = {
  "needs-attention": {
    title: "No campaigns need attention",
    helper:
      "Campaigns that need a decision or fix will appear here when schedule and send are available.",
  },
  drafts: {
    title: "No drafts yet",
    helper: "Create a campaign or use a template to start a draft.",
  },
  "in-flight": {
    title: "No campaigns in flight",
    helper: "Scheduled and sending campaigns will appear here.",
  },
  sent: {
    title: "No sent campaigns yet",
    helper: "Sent campaigns will appear here after you send.",
  },
}

export function campaignsListEmptyCopy(input: {
  kind: OperatorCampaignsListEmptyStateKind
  activeViewId: OperatorCampaignsListViewId
}): { title: string; helper: string } {
  if (input.kind === "true-empty") {
    return {
      title: CAMPAIGNS_PAGE_COPY.trueEmptyTitle,
      helper: CAMPAIGNS_PAGE_COPY.trueEmptyHelper,
    }
  }

  if (input.kind === "filter-search") {
    return {
      title: CAMPAIGNS_PAGE_COPY.filterSearchTitle,
      helper: CAMPAIGNS_PAGE_COPY.filterSearchHelper,
    }
  }

  if (input.activeViewId === "all") {
    return {
      title: CAMPAIGNS_PAGE_COPY.trueEmptyTitle,
      helper: CAMPAIGNS_PAGE_COPY.trueEmptyHelper,
    }
  }

  return OPERATOR_CAMPAIGNS_VIEW_SCOPED_EMPTY_COPY[input.activeViewId]
}

/** Fixed sibling summary KPIs — ignore Campaigns date window (slice 1 mocks). */
export const CAMPAIGNS_SUMMARY_MOCK_KPIS = [
  {
    id: "campaigns-in-flight" as const,
    label: CAMPAIGNS_PAGE_COPY.campaignsInFlightLabel,
    description: CAMPAIGNS_PAGE_COPY.campaignsInFlightDescription,
    value: 3,
  },
  {
    id: "messages-sent" as const,
    label: CAMPAIGNS_PAGE_COPY.messagesSentLabel,
    description: CAMPAIGNS_PAGE_COPY.messagesSentDescription,
    value: 1842,
  },
  {
    id: "campaign-attributed-redemptions" as const,
    label: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsLabel,
    description: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsDescription,
    value: 0,
  },
]

export const CAMPAIGNS_PAGE_SIZE = 25

export const CAMPAIGNS_PAGE_META_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-muted-foreground"

/** True-empty action row — Figma 4026:45652 (12px gap). */
export const CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS =
  "mt-[30px] flex items-center justify-center gap-3"

/** Helper width from Figma true-empty (4026:45651). Main Bg / Subtitle grey. */
export const CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[306px] text-sm font-medium leading-[18px] text-[var(--op-color-gray-550)]"

/** Messaging usage nested meter / plan tiles — Figma 3462:62679. */
export const CAMPAIGNS_MESSAGING_USAGE_METERS_ROW_CLASS =
  "grid grid-cols-1 gap-3 sm:grid-cols-2"

export const CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS =
  "flex flex-col gap-3 rounded-op-md border border-op-border-default p-6"

export const CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS =
  "m-0 text-lg font-medium leading-normal text-op-card-title-color"

export const CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS =
  "m-0 text-op-sm font-normal leading-normal text-op-card-subtitle-color"

export const CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS =
  "m-0 text-xs font-normal leading-normal text-op-card-subtitle-color"

export const CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS =
  "flex items-center gap-2.5"

export const CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS =
  "relative h-1.5 min-w-0 flex-1 overflow-hidden rounded-[4px] bg-op-card-border"

export const CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS =
  "absolute top-1/2 left-0 h-2 -translate-y-1/2 rounded-[4px] bg-[var(--op-color-green-500)]"

export const CAMPAIGNS_MESSAGING_USAGE_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-[18px]"

export const CAMPAIGNS_SEARCH_MISS_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-op-card-title-color"
