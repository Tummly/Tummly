/** Figma Campaigns overview — nodes 3462:61945 (header) / 3462:61952 (summary) / 4026:45443 (true-empty). */

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
} as const

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

export const CAMPAIGNS_PAGE_META_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-muted-foreground"

/** True-empty action row — Figma 4026:45652 (12px gap). */
export const CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS =
  "mt-[30px] flex items-center justify-center gap-3"

/** Helper width from Figma true-empty (4026:45651). */
export const CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[306px] text-sm font-medium leading-[18px] text-muted-foreground dark:text-[#7c7c7c]"
