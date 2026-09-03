/**
 * Figma Operator Reports — Campaigns report sub-page.
 */

export const CAMPAIGNS_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbCampaignsReport: "Campaigns report",
  title: "Campaigns report",
  pageTitle: "Campaigns report",
  subtitle:
    "See how your messages perform across eligible guests, offers and opt-outs.",
  pageSubtitle:
    "See how your messages perform across eligible guests, offers and opt-outs.",
  generateBrief: "Generate brief",
  export: "Export",

  // Empty state copy
  emptyTitle: "No campaign reports yet",
  emptySubtitle:
    "Send your first campaign to eligible guests and track claims, redemptions and opt-outs here.",
  createCampaign: "Create campaign",

  // Section titles
  performanceSectionTitle: "Campaign performance",
  needsAttentionSectionTitle: "Needs attention",
  needsAttentionSectionSubtitle: "Review issues that may require action.",

  // Table headers
  campaignHeader: "Campaign",
  goalHeader: "Goal",
  channelHeader: "Channel",
  sentHeader: "Sent",
  claimsHeader: "Claims",
  redemptionsHeader: "Redemptions",
  unsubscribesHeader: "Unsubscribes",
  statusHeader: "Status",

  // Action labels
  reviewFeedback: "Review feedback",
  viewUsage: "View usage",
  addCredits: "Add credits",
  manageOffer: "Manage offer",
  viewRedemptions: "View redemptions",
} as const

export type CampaignsReportKpi = {
  label: string
  value: string | number
  delta: string
  positive?: boolean | null
}

export type CampaignsReportPerformanceRow = {
  id: string
  campaign: string
  goal: string
  channel: string
  sent: number
  claims: number
  redemptions: number
  unsubscribes: number
  status: string
}

export type CampaignsReportAttentionItem = {
  id: string
  type: "feedback" | "credits" | "offer"
  title: string
  description: string
  meta: string
  actions: Array<{
    label: string
    target: "feedback" | "credits-usage" | "shop" | "offers" | "redemption-log"
  }>
}

export type CampaignsReportData = {
  kpis: {
    campaignsSent: CampaignsReportKpi
    guestsMessaged: CampaignsReportKpi
    offerClaims: CampaignsReportKpi
    offerRedemptions: CampaignsReportKpi
    unsubscribes: CampaignsReportKpi
    failedSends: CampaignsReportKpi
  }
  performance: CampaignsReportPerformanceRow[]
  attentionItems: CampaignsReportAttentionItem[]
}

export const mockCampaignsReportData: CampaignsReportData = {
  kpis: {
    campaignsSent: {
      label: "Campaigns sent",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    guestsMessaged: {
      label: "Guests messaged",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    offerClaims: {
      label: "Offer claims",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    offerRedemptions: {
      label: "Offer redemptions",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    unsubscribes: {
      label: "Unsubscribes",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    failedSends: {
      label: "Failed sends",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
  },
  performance: [
    {
      id: "1",
      campaign: "Quiet Tuesday offer",
      goal: "Quiet-day boost",
      channel: "SMS",
      sent: 9,
      claims: 41,
      redemptions: 38,
      unsubscribes: 2,
      status: "Sent",
    },
    {
      id: "2",
      campaign: "Weekend Flash Deal",
      goal: "Saturday special",
      channel: "Email",
      sent: 15,
      claims: 60,
      redemptions: 45,
      unsubscribes: 5,
      status: "Scheduled",
    },
    {
      id: "3",
      campaign: "Midweek Motivation",
      goal: "Wednesday surprise",
      channel: "SMS",
      sent: 12,
      claims: 55,
      redemptions: 50,
      unsubscribes: 3,
      status: "Sent",
    },
    {
      id: "4",
      campaign: "Holiday Countdown",
      goal: "Festive offer",
      channel: "SMS",
      sent: 20,
      claims: 70,
      redemptions: 65,
      unsubscribes: 7,
      status: "Pending",
    },
    {
      id: "5",
      campaign: "Morning Energy Boost",
      goal: "Early bird special",
      channel: "Email",
      sent: 18,
      claims: 52,
      redemptions: 47,
      unsubscribes: 4,
      status: "Sent",
    },
  ],
  attentionItems: [
    {
      id: "att-1",
      type: "feedback",
      title: "3 feedback items need attention",
      description:
        "Negative or unresolved feedback has not been reviewed yet.",
      meta: "Warning · 12 minutes ago · All locations",
      actions: [
        {
          label: "Review feedback",
          target: "feedback",
        },
      ],
    },
    {
      id: "att-2",
      type: "credits",
      title: "SMS credits are running low",
      description:
        "You have 84 SMS credits left. Scheduled campaigns may be limited soon.",
      meta: "Warning · Updated 18 minutes ago · Account-wide",
      actions: [
        {
          label: "View usage",
          target: "credits-usage",
        },
        {
          label: "Add credits",
          target: "shop",
        },
      ],
    },
    {
      id: "att-3",
      type: "offer",
      title: "Offer expires in 2 days",
      description:
        "“10% off next order” has 23 claims and 9 redemptions before expiry.",
      meta: "Warning · 1 hour ago · Manchester",
      actions: [
        {
          label: "Manage offer",
          target: "offers",
        },
        {
          label: "View redemptions",
          target: "redemption-log",
        },
      ],
    },
  ],
}
