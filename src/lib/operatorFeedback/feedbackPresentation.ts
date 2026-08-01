/** Feedback page presentation tokens and copy — Guests / Capture section chrome. */

import type {
  OperatorFeedbackInboxEmptyStateKind,
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTabId,
} from "@/types/operatorFeedback"

export const OPERATOR_FEEDBACK_INBOX_TAB_LABELS: Record<
  OperatorFeedbackInboxTabId,
  string
> = {
  all: "All",
  "needs-attention": "Needs attention",
  new: "New",
  "in-progress": "In progress",
  resolved: "Resolved",
}

export const OPERATOR_FEEDBACK_INBOX_SORT_LABELS: Record<
  OperatorFeedbackInboxSortId,
  string
> = {
  "newest-submitted": "Newest submitted",
  "oldest-submitted": "Oldest submitted",
  "needs-attention-first": "Needs attention first",
  "oldest-unresolved": "Oldest unresolved",
  "recently-updated": "Recently updated",
  "negative-first": "Negative first",
  "positive-first": "Positive first",
  "guest-name-az": "Guest name A–Z",
}

export const OPERATOR_FEEDBACK_INBOX_EMPTY_COPY: Record<
  OperatorFeedbackInboxEmptyStateKind,
  { title: string; helper: string; actionLabel?: string }
> = {
  "no-match": {
    title: "No feedback matches these filters",
    helper: "Try removing a filter or changing your search.",
    actionLabel: "Clear all filters",
  },
  "true-empty": {
    title: "No feedback in this period",
    helper: "Try a wider date range, or check another tab.",
    actionLabel: "Change period",
  },
}

export function feedbackInboxPageRangeLabel(
  from: number,
  to: number,
  total: number
): string {
  return `Showing ${from}–${to} of ${total} feedback items`
}

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
    searchPlaceholder: "Search comments, guest names or issue tags.",
  },
  overflow: {
    exportFeedback: "Export feedback",
    manageSettings: "Manage feedback settings",
    viewHelp: "View feedback help",
  },
  exportDialog: {
    title: "Export feedback",
    subtitle: "Choose which feedback records and fields to include.",
    scopeCurrentTitle: "Current results",
    scopeCurrentHelper: (n: number) =>
      `Download the ${n} feedback items matching your current search and filters.`,
    scopeAllTitle: "All feedback in this period",
    scopeAllHelper: (m: number, location: string, period: string) =>
      `Download all ${m} feedback items from ${location} for ${period}.`,
    fileFormatLabel: "File format",
    formatExcel: "Excel (.xlsx)",
    formatCsv: "CSV (.csv)",
    includeContactLabel: "Include guest contact details",
    includeContactHelper:
      "Includes available guest names, email addresses and mobile numbers.",
    summaryLocation: "Location",
    summaryPeriod: "Period",
    summaryItems: "Feedback items",
    summaryFormat: "Format",
    summaryContact: "Guest contact details",
    contactNotIncluded: "Not included",
    contactIncluded: "Included",
    formatExcelShort: "Excel",
    formatCsvShort: "CSV",
    download: "Download export",
    preparing: "Preparing download…",
    cancel: "Cancel",
    softMaxError:
      "Export exceeds 10,000 rows. Narrow filters and try again.",
    genericError: "Could not export feedback. Please try again.",
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
