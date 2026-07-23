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

/** Guest Profile header ⋮ — Figma 3388:12934; styles match Guests row Actions. */
export const GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS = [
  { id: "manage-tags", label: "Manage tags" },
  {
    id: "manage-marketing-permissions",
    label: "Manage marketing permissions",
  },
  { id: "export-guest-record", label: "Export guest record" },
  { id: "delete-guest-data", label: "Delete guest data" },
] as const

export type GuestProfileHeaderOverflowActionId =
  (typeof GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS)[number]["id"]

export const GUEST_PROFILE_DEFAULT_SORT_LABEL = "Sort: Recent activity"

export const OPERATOR_GUEST_FEEDBACKS_SORT_LABELS = {
  "recent-activity": "Recent activity",
  "oldest-first": "Oldest first",
} as const

export const OPERATOR_GUEST_FEEDBACKS_DEFAULT_SORT_ID =
  "recent-activity" as const

export const OPERATOR_GUEST_ACTIVITY_SORT_LABELS = {
  "recent-activity": "Recent activity",
  "oldest-first": "Oldest first",
} as const

export const OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID =
  "recent-activity" as const

export const GUEST_PROFILE_FEEDBACKS_FILTERED_EMPTY = {
  emptyTitle: "No feedback found",
  emptyHelper: "Try changing your search or removing some filters.",
  clearLabel: "Clear search and filters",
} as const

export const GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY = {
  emptyTitle: "No activity found",
  emptyHelper: "Try removing some filters.",
  clearLabel: "Clear filters",
} as const

/** Activity type filter tokens (toolbar) → API `type` query. */
export const OPERATOR_GUEST_ACTIVITY_TYPE_LABELS = {
  "guest-joined": "Guest joined",
  feedback: "Feedback",
  classification: "Classification",
  note: "Note",
  tag: "Tag",
  "profile-update": "Profile update",
} as const

export const GUEST_PROFILE_CONTACT_STATUS_LABELS = {
  eligible: "Eligible",
  unsubscribed: "Unsubscribed",
  not_provided: GUEST_PROFILE_NOT_PROVIDED,
} as const

/** Honesty: Source until first-class capture ships. */
export const GUEST_PROFILE_FEEDBACK_SOURCE_LABEL = "Guest QR form"

/** Honesty: Recovery until recovery domain ships. */
export const GUEST_PROFILE_FEEDBACK_RECOVERY_PLACEHOLDER = "—"

export const GUEST_PROFILE_OPEN_FEEDBACK_LABEL = "Open feedback"
export const GUEST_PROFILE_VIEW_ALL_FEEDBACKS_LABEL = "View all feedbacks"

export const GUEST_PROFILE_ADD_NOTE_LABEL = "Add note"
export const GUEST_PROFILE_NOTE_COMPOSE = {
  dialogTitle: "Internal notes",
  dialogDescription:
    "Add information that may help your team manage this guest relationship. Notes are never shown to the guest.",
  fieldLabel: "Add a note",
  placeholder: "Write an internal note about this guest…",
  saveLabel: "Add note",
  cancelLabel: "Cancel",
  maxLength: 5000,
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

export const GUEST_EDIT_PAGE = {
  title: "Edit guest details",
  subtitle:
    "Update guest information, tags and internal notes. Consent and unsubscribe records are protected for compliance.",
  saveLabel: "Save changes",
  cancelLabel: "Cancel",
  breadcrumbCurrent: "Edit guest details",
  guestInformation: {
    sectionTitle: "Guest information",
    helper:
      "Basic guest details captured from QR forms, campaigns or manual updates.",
  },
  consent: {
    sectionTitle: "Consent and contact permissions",
    helper:
      "Shows what this guest has agreed to receive. Consent changes are recorded in the audit log.",
    statusLabels: {
      eligible: "Opted in",
      unsubscribed: "Unsubscribed",
      not_provided: "Not opted in",
    },
    sourcePlaceholder: "—",
    datePlaceholder: "—",
  },
  tags: {
    sectionTitle: "Tags and groups",
    helper: "Organise this guest with tags and see which smart groups they match.",
    tagsLabel: "Tags",
    selectPlaceholder: "Select",
    applyLabel: "Apply tags",
    cancelLabel: "Cancel",
    smartGroupsLabel: "Smart groups",
    smartGroupsEmptyHelper: "Smart groups will appear here when available.",
  },
  internalNotes: {
    sectionTitle: "Internal notes",
    helper: "Private notes visible only to your team.",
  },
  recentFeedback: {
    sectionTitle: "Recent feedback",
    helper: "Latest feedback submitted by this guest.",
    emptyTitle: "No feedback yet",
    emptyHelper: "Feedback submitted by this guest will appear here.",
  },
  offers: {
    sectionTitle: "Offers and redemptions",
    helper: "Offers claimed or redeemed by this guest.",
    emptyTitle: "No offer activity",
    emptyHelper: "Offers claimed by this guest will appear here.",
  },
  campaigns: {
    sectionTitle: "Campaign history",
    helper: "Messages sent to this guest through Tummly.",
    emptyTitle: "No campaigns sent",
    emptyHelper: "This guest has not been included in a campaign yet.",
  },
  dataPrivacy: {
    sectionTitle: "Data and privacy",
    helper: "Manage privacy-related actions for this guest.",
    guestIdLabel: "Guest ID",
    dataOwnerLabel: "Data owner",
    exportLabel: "Export guest data",
    exportContent: "Download guest record",
    exportAction: "Export",
    deleteLabel: "Delete request",
    deleteContent: "Start deletion workflow",
    deleteAction: "Request deletion",
    deleteDialogTitle: "Delete guest data?",
    deleteDialogDescription:
      "This permanently deletes this guest's record for this location only. Notes, tags, and activity for this location are removed. Feedback stays but is unlinked. This cannot be undone.",
    deleteDialogConfirm: "Delete guest data",
    deleteDialogCancel: "Cancel",
    auditLabel: "Audit history",
    auditContent: "View changes to this record",
    auditAction: "View audit log",
    copyLabel: "Copy",
    dataOwnerPlaceholder: "—",
  },
  provenancePlaceholders: {
    contactMethod: "—",
    sourceQr: "—",
  },
} as const
