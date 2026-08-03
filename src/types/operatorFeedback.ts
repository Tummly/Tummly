import type { FilterChip } from "@/lib/operatorFilterSheet"
import type { FeedbackWorkflowStatus } from "@/types/dashboard"

export type OperatorFeedbackInboxTabId =
  | "all"
  | "needs-attention"
  | "new"
  | "in-progress"
  | "resolved"

export type OperatorFeedbackInboxSortId =
  | "newest-submitted"
  | "oldest-submitted"
  | "needs-attention-first"
  | "oldest-unresolved"
  | "recently-updated"
  | "negative-first"
  | "positive-first"
  | "guest-name-az"

export type OperatorFeedbackInboxEmptyStateKind = "no-match" | "true-empty"

export type OperatorFeedbackInboxTab = {
  id: OperatorFeedbackInboxTabId
  label: string
  count: number
}

export type OperatorFeedbackInboxDigitalGuestLink = {
  id: number
  linkName: string
}

export type OperatorFeedbackInboxTableRow = {
  id: number
  feedbackPreview: string
  feedbackFull: string
  sentiment: "positive" | "neutral" | "negative" | null
  issueTagLabels: string[] | null
  guestName: string
  locationSourceDisplay: string
  submittedDisplay: string
  workflowStatus: FeedbackWorkflowStatus
  canReopen: boolean
  canMarkNoActionNeeded: boolean
}

export type OperatorFeedbackInboxViewModel = {
  tabs: OperatorFeedbackInboxTab[]
  tableRows: OperatorFeedbackInboxTableRow[]
  tableEmptyState: OperatorFeedbackInboxEmptyStateKind | null
  searchQuery: string
  sortId: OperatorFeedbackInboxSortId
  sortLabel: string
  filterChips: readonly FilterChip[]
  filterChipCount: number
  /** Filtered inbox total for the active tab ∧ search ∧ filters (export Current results). */
  filteredTotalCount: number
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
  digitalGuestLinks: readonly OperatorFeedbackInboxDigitalGuestLink[]
}

export type OperatorFeedbackSummaryKpiId =
  | "total"
  | "positive"
  | "neutral"
  | "negative"

export type OperatorFeedbackSummaryKpi = {
  id: OperatorFeedbackSummaryKpiId
  label: string
  value: number
  /** e.g. "40% of feedback"; null for Total. */
  shareLabel: string | null
  /** PoP helper under the share/trend line — always set for the KPI strip. */
  comparisonLabel: string
}

export type OperatorFeedbackSummarySection =
  | { kind: "empty" }
  | { kind: "kpis"; kpis: OperatorFeedbackSummaryKpi[] }

export type OperatorFeedbackPageViewModel = {
  locationId: number
  locationName: string
  dateRangeLabel: string
  updatedRelativeLabel: string
  needsAttentionCount: number
  summary: OperatorFeedbackSummarySection
  inbox: OperatorFeedbackInboxViewModel
}
