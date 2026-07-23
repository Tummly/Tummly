export type GuestFeedbackSentiment = "positive" | "neutral" | "negative" | "none"

export type GuestMarketingStatusLabel =
  | "Eligible — Email"
  | "Eligible — SMS"
  | "Not eligible"
  | "Suppressed"

export type OperatorGuestFixture = {
  id: string
  name: string
  email: string
  mobile: string | null
  marketingStatusLabel: GuestMarketingStatusLabel
  marketingEligible: boolean
  locationName: string
  capturedAt: string
  needsRecovery: boolean
  latestFeedbackSentiment: GuestFeedbackSentiment
  feedbackSubmissionCount: number
  hasOffer: boolean
  offerRedeemedAt: string | null
  lastInteractionAt: string | null
  lastInteractionLabel: string
}

export type OperatorGuestSmartGroupId =
  | "all-guests"
  | "new-guests"
  | "needs-recovery"
  | "positive-feedback"
  | "offer-not-redeemed"
  | "recent-redeemers"
  | "dormant-guests"

export type OperatorGuestSortId =
  | "recent-activity"
  | "newest-guests"
  | "oldest-guests"
  | "guest-name-az"
  | "guest-name-za"
  | "most-feedback-submissions"
  | "most-recent-redemption"

export type OperatorGuestOverviewKpiId =
  | "total-guests"
  | "new-this-month"
  | "marketing-eligible"
  | "needs-recovery"

export type OperatorGuestTableRow = {
  id: string
  name: string
  email: string
  mobile: string | null
  marketingStatusLabel: GuestMarketingStatusLabel
  locationName: string
  latestFeedbackSentiment: GuestFeedbackSentiment
  feedbackSubmissionCount: number
  lastInteractionLabel: string
  lastInteractionAt: string | null
}

export type OperatorGuestSmartGroupTab = {
  id: OperatorGuestSmartGroupId
  label: string
  count: number
}

export type OperatorGuestOverviewKpi = {
  id: OperatorGuestOverviewKpiId
  label: string
  description: string
  value: number
}

export type OperatorGuestsTableEmptyStateKind =
  | "no-guests-found"
  | "no-guests-yet"

export type OperatorGuestsViewModel = {
  overviewKpis: OperatorGuestOverviewKpi[]
  smartGroupTabs: OperatorGuestSmartGroupTab[]
  activeSmartGroupId: OperatorGuestSmartGroupId
  tableRows: OperatorGuestTableRow[]
  tableEmptyState: OperatorGuestsTableEmptyStateKind | null
  totalFilteredCount: number
  sortLabel: string
  pageSize: number
  currentPage: number
  pageRangeLabel: string
}
