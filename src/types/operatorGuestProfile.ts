import type { GuestMarketingStatusLabel } from "@/types/operatorGuests"

export type OperatorGuestProfileTabId =
  | "overview"
  | "feedbacks"
  | "offers"
  | "campaigns"
  | "activity"
  | "notes"

export type OperatorGuestProfileEligibilityStatus =
  | "eligible"
  | "unsubscribed"
  | "not_provided"
  | "not_recorded"

export type OperatorGuestProfileLatestFeedbackRow = {
  id: number
  classificationDisplay: "positive" | "neutral" | "negative" | null
  dateDisplay: string
  locationName: string
  sourceDisplay: string
  /** Truncated preview for the table cell. */
  feedbackDisplay: string
  /** Full comment for hover tooltip. */
  feedbackFullDisplay: string
  issueTagLabels: string[] | null
  recoveryDisplay: string
}

export type OperatorGuestProfileLatestOfferRow = {
  id: number
  title: string
  statusDisplay: string
  sourceDisplay: string
  dateDisplay: string
}

export type OperatorGuestProfileLatestCampaignRow = {
  id: number
  campaignId: number
  campaignName: string
  channelDisplay: string
  outcomeDisplay: string
  dateDisplay: string
}

export type OperatorGuestFeedbacksSortId =
  | "recent-activity"
  | "oldest-first"

export type OperatorGuestFeedbacksTableEmptyStateKind =
  | "virgin-empty"
  | "filtered-empty"

export type OperatorGuestProfileFeedbacksRow = {
  id: number
  dateDisplay: string
  /** Truncated preview for the table cell. */
  feedbackDisplay: string
  /** Full comment for hover tooltip. */
  feedbackFullDisplay: string
  classificationDisplay: "positive" | "neutral" | "negative" | null
  issueTagLabels: string[] | null
  recoveryDisplay: string
  locationDisplay: string
}

export type OperatorGuestFeedbacksViewModel = {
  tableRows: OperatorGuestProfileFeedbacksRow[]
  tableEmptyState: OperatorGuestFeedbacksTableEmptyStateKind | null
  totalCount: number
  sortLabel: string
  pageSize: number
  currentPage: number
  pageRangeLabel: string
  /** False when unfiltered total is 0 (locked empty chrome). */
  toolbarEnabled: boolean
}

export type OperatorGuestActivitySortId =
  | "recent-activity"
  | "oldest-first"

export type OperatorGuestActivityTimelineEmptyStateKind =
  | "virgin-empty"
  | "filtered-empty"

export type OperatorGuestProfileActivityRow = {
  id: number
  headline: string
  body: string
  metaDisplay: string
}

export type OperatorGuestActivityViewModel = {
  timelineRows: OperatorGuestProfileActivityRow[]
  timelineEmptyState: OperatorGuestActivityTimelineEmptyStateKind | null
  totalCount: number
  sortLabel: string
  pageSize: number
  currentPage: number
  pageRangeLabel: string
  /** False when unfiltered total is 0 (no Filters/Sort chrome). */
  toolbarEnabled: boolean
}

export type OperatorGuestProfileNoteRow = {
  id: number
  body: string
  authorDisplayName: string
  createdAtDisplay: string
  /** True when the note body has been edited since creation. */
  isEdited: boolean
}

export type OperatorGuestProfileViewModel = {
  id: string
  locationId: number
  name: string
  marketingStatusLabel: GuestMarketingStatusLabel
  guestSinceDisplay: string
  lastActivityDisplay: string | null
  identitySubtitle: string
  lastInteractionLabel: string
  profileSummary: {
    emailDisplay: string
    mobileDisplay: string
    firstCapturedDisplay: string
    locationName: string
    feedbackSubmissionCount: number
    offerClaimsAndRedemptions: number
    lastInteractionDisplay: string
    lastInteractionLabel: string
    guestTagsDisplay: string
    guestTags: Array<{ id: string; name: string }>
  }
  overviewDetails: {
    guestSinceDisplay: string
    totalInteractions: number
    feedbackReceived: number
    offersClaimed: number
    campaignsSent: number
    lastActivityDisplay: string
  }
  contactEligibility: Array<{
    channel: "email" | "sms"
    channelLabel: "Email" | "SMS"
    status: OperatorGuestProfileEligibilityStatus
    statusLabel: string
    detailDisplay: string
  }>
  latestFeedback: OperatorGuestProfileLatestFeedbackRow[]
  latestOffers: OperatorGuestProfileLatestOfferRow[]
  latestCampaigns: OperatorGuestProfileLatestCampaignRow[]
  recentNotes: OperatorGuestProfileNoteRow[]
}
