/** Feedback page presentation tokens and copy — Guests / Capture section chrome. */

export const FEEDBACK_PAGE_COPY = {
  title: "Feedback",
  subtitle:
    "Review private guest feedback, identify recurring issues and manage follow-up actions.",
  summariseWithAi: "Summarise with AI",
  reviewNeedsAttention: (n: number) => `Review needs attention (${n})`,
  summary: {
    title: "Feedback summary",
    subtitle:
      "Sentiment mix for private guest feedback in the selected period.",
    emptyTitle: "No feedback received during this period",
    emptyHelper:
      "Try a wider date range or check that your QR placements are active.",
    changePeriod: "Change period",
    viewCapture: "View Capture",
  },
  inbox: {
    title: "Feedback inbox",
    subtitle:
      "Review and manage follow-up for private guest feedback at this location.",
  },
  overflow: {
    exportFeedback: "Export feedback",
    manageSettings: "Manage feedback settings",
    viewHelp: "View feedback help",
  },
} as const

export const FEEDBACK_HEADER_OVERFLOW_ACTIONS = [
  { id: "export-feedback", label: FEEDBACK_PAGE_COPY.overflow.exportFeedback },
  {
    id: "manage-feedback-settings",
    label: FEEDBACK_PAGE_COPY.overflow.manageSettings,
  },
  { id: "view-feedback-help", label: FEEDBACK_PAGE_COPY.overflow.viewHelp },
] as const

export const FEEDBACK_PAGE_META_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-muted-foreground"
