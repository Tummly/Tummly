import type { OperatorGuestProfileTabId } from "@/types/operatorGuestProfile"

export const GUEST_PROFILE_NOT_PROVIDED = "Not provided"

export const GUEST_PROFILE_TABS: ReadonlyArray<{
  id: OperatorGuestProfileTabId
  label: string
}> = [
  { id: "overview", label: "Overview" },
  { id: "feedbacks", label: "Feedbacks" },
  { id: "offers", label: "Offers" },
  { id: "campaigns", label: "Campaigns" },
  { id: "activity", label: "Activity" },
  { id: "notes", label: "Notes" },
]

export const GUEST_PROFILE_UNAVAILABLE_TITLE = "Guest unavailable"
export const GUEST_PROFILE_UNAVAILABLE_HELPER =
  "This guest isn’t available for the selected location."
export const GUEST_PROFILE_BACK_TO_GUESTS_LABEL = "Back to Guests"

export const GUEST_PROFILE_BREADCRUMB_GUESTS = "Guests"

export const GUEST_PROFILE_DEFAULT_SORT_LABEL = "Sort: Recent activity"

export const GUEST_PROFILE_CONTACT_STATUS_LABELS = {
  eligible: "Eligible",
  unsubscribed: "Unsubscribed",
  not_provided: GUEST_PROFILE_NOT_PROVIDED,
} as const

export const GUEST_PROFILE_EMPTY_COPY = {
  overviewLatestFeedback: {
    sectionTitle: "Latest feedback",
    emptyTitle: "No feedback yet",
    emptyHelper: "Feedback submitted by this guest will appear here.",
  },
  overviewLatestOffer: {
    sectionTitle: "Latest offer activity",
    emptyTitle: "No offer activity",
    emptyHelper: "Offers claimed by this guest will appear here.",
  },
  overviewLatestCampaign: {
    sectionTitle: "Latest campaign activity",
    emptyTitle: "No campaigns sent",
    emptyHelper: "This guest has not been included in a campaign yet.",
  },
  overviewRecentNotes: {
    sectionTitle: "Recent notes",
    emptyTitle: "No notes added",
    emptyHelper: "There are no notes about this guest.",
  },
  activityTab: {
    sectionTitle: "Activity",
    emptyTitle: "No activity yet",
    emptyHelper:
      "This guest's activity will appear here as they interact with the restaurant.",
  },
  notesTab: {
    sectionTitle: "Notes",
    emptyTitle: "No notes added",
    emptyHelper: "Internal notes about this guest will appear here.",
  },
  feedbacksTab: {
    sectionTitle: "Feedback history",
    emptyTitle: "No feedback found",
    emptyHelper: "This guest has not submitted any feedback yet.",
    searchPlaceholder: "Search feedback",
  },
  offersTab: {
    sectionTitle: "Offer activity",
    emptyTitle: "No offer activity",
    emptyHelper: "Offers claimed by this guest will appear here.",
    searchPlaceholder: "Search offers",
  },
  campaignsTab: {
    sectionTitle: "Campaign history",
    emptyTitle: "No campaigns sent",
    emptyHelper: "Campaigns sent to this guest will appear here.",
    searchPlaceholder: "Search campaigns",
  },
} as const
