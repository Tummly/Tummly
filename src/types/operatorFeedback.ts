export type OperatorFeedbackInboxTabId =
  | "all"
  | "needs-attention"
  | "new"
  | "in-progress"
  | "resolved"

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
  /** PoP helper; null when previous Total is 0 (soften/hide). */
  comparisonLabel: string | null
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
}
