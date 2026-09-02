/**
 * Figma Operator Reports — Feedback report sub-page.
 * Node 3498 / reports-feedback-flow.
 */

export type DatePreset = "7d" | "30d" | "90d" | "month" | "ytd"

export const DATE_PRESET_LABELS: Record<DatePreset, string> = {
  "7d": "Last 7 days",
  "30d": "Last 30 days",
  "90d": "Last 90 days",
  month: "This month",
  ytd: "Year to date",
}

export const FEEDBACK_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbFeedbackReport: "Feedback report",
  title: "Feedback report",
  subtitle:
    "Read private guest feedback, spot common themes and follow up where needed.",
  generateBrief: "Generate brief",
  export: "Export",
  emptyTitle: "No feedback yet",
  emptySubtitle:
    "Once guests submit private feedback, you'll see messages, contactability and follow-up activity here.",
  checkGuestForm: "Check guest form",
  feedbackOverTimeTitle: "Feedback over time",
  feedbackOverTimeSubtitle:
    "Shows private feedback submitted through your Tummly guest form.",
  commonThemesTitle: "Common themes",
  viewSourceFeedback: "View source feedback",
  needsFollowUpTitle: "Needs follow-up",
  needsFollowUpSubtitle: "Feedback that may need a team response.",
  openFeedbackInbox: "Open feedback inbox",
  openAction: "Open",
  feedbackBySourceTitle: "Feedback by source",
  feedbackBySourceSubtitle: "Feedback that may need a team response.",
  sourceInsightText:
    "Delivery inserts created the most feedback and the most follow-up opportunities.",
  feedbackStatusTitle: "Feedback status",
  manageFeedback: "Manage feedback",
} as const

export type FeedbackReportKpi = {
  label: string
  value: string | number
  delta: string
  positive?: boolean | null
}

export type FeedbackReportTheme = {
  id: string
  theme: string
  meta: string
}

export type FeedbackReportFollowUpRow = {
  id: string
  date: string
  guest: string
  source: string
  feedback: string
  reason: string
  status: string
}

export type FeedbackReportSourceRow = {
  source: string
  feedback: number | string
  contactable: number | string
  followUpNeeded: number | string
}

export type FeedbackReportData = {
  kpis: {
    feedbackReceived: FeedbackReportKpi
    contactableFeedback: FeedbackReportKpi
    followUpNeeded: FeedbackReportKpi
    followedUp: FeedbackReportKpi
    resolved: FeedbackReportKpi
  }
  themes: FeedbackReportTheme[]
  followUpList: FeedbackReportFollowUpRow[]
  sourcesList: FeedbackReportSourceRow[]
  statusKpis: {
    newFeedback: FeedbackReportKpi
    reviewed: FeedbackReportKpi
    followUpNeeded: FeedbackReportKpi
    followedUp: FeedbackReportKpi
    resolved: FeedbackReportKpi
  }
}

export const mockFeedbackReportData: FeedbackReportData = {
  kpis: {
    feedbackReceived: {
      label: "Feedback received",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    contactableFeedback: {
      label: "Contactable feedback",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    followUpNeeded: {
      label: "Follow-up needed",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    followedUp: {
      label: "Followed up",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    resolved: {
      label: "Resolved",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
  },
  themes: [
    {
      id: "theme-1",
      theme:
        "Guests commonly mentioned delivery packaging, wait time during busy periods and friendly service. A few comments may need follow-up because the guest shared contact details and described a specific issue.",
      meta: "Based on 42 feedback messages from 6–12 July. Review the original comments before taking action.",
    },
    {
      id: "theme-2",
      theme:
        "Several guests praised the taste and presentation of the main dishes, though some noted inconsistencies in portion size. A couple of remarks highlighted dietary preferences and ingredient freshness.",
      meta: "Derived from 38 feedback submissions collected between 7–13 July. Verify individual reviews for context before responding.",
    },
    {
      id: "theme-3",
      theme:
        "Feedback frequently mentioned restaurant ambiance and music volume, with a few guests suggesting adjustments for a more comfortable atmosphere. Multiple notes touched on reservation process efficiency.",
      meta: "Compiled from 45 guest responses dated 8–14 July. Ensure full comment review to address specific concerns adequately.",
    },
  ],
  followUpList: [
    {
      id: "followup-1",
      date: "12 Jul",
      guest: "Sarah",
      source: "Delivery insert",
      feedback: "“Food was good but the bag had leaked…”",
      reason: "AI suggested",
      status: "Follow-up needed",
    },
    {
      id: "followup-2",
      date: "12 Jul",
      guest: "Sarah",
      source: "Delivery insert",
      feedback: "“Food was good but the bag had leaked…”",
      reason: "AI suggested",
      status: "Follow-up needed",
    },
    {
      id: "followup-3",
      date: "12 Jul",
      guest: "Sarah",
      source: "Delivery insert",
      feedback: "“Food was good but the bag had leaked…”",
      reason: "AI suggested",
      status: "Follow-up needed",
    },
    {
      id: "followup-4",
      date: "12 Jul",
      guest: "Sarah",
      source: "Delivery insert",
      feedback: "“Food was good but the bag had leaked…”",
      reason: "AI suggested",
      status: "Follow-up needed",
    },
    {
      id: "followup-5",
      date: "12 Jul",
      guest: "Sarah",
      source: "Delivery insert",
      feedback: "“Food was good but the bag had leaked…”",
      reason: "AI suggested",
      status: "Follow-up needed",
    },
  ],
  sourcesList: [
    {
      source: "Delivery insert",
      feedback: 11,
      contactable: 32,
      followUpNeeded: 12,
    },
    {
      source: "Counter card",
      feedback: 43,
      contactable: 10,
      followUpNeeded: 4,
    },
    {
      source: "Receipt QR",
      feedback: 54,
      contactable: 12,
      followUpNeeded: 7,
    },
    {
      source: "Table card",
      feedback: 10,
      contactable: 22,
      followUpNeeded: 8,
    },
  ],
  statusKpis: {
    newFeedback: {
      label: "New",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    reviewed: {
      label: "Reviewed",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    followUpNeeded: {
      label: "Follow-up needed",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    followedUp: {
      label: "Followed up",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    resolved: {
      label: "Resolved",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
  },
}
