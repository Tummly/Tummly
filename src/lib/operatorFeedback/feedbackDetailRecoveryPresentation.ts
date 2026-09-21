import type { ContactType, FeedbackWorkflowStatus } from "@/types/dashboard"

/** Wire values from the Location Guest permission ledger. */
export type GuestPermissionWireState =
  | "granted"
  | "withdrawn"
  | "not_recorded"

export type FeedbackDetailPermissionKind =
  | "email-marketing"
  | "sms-marketing"
  | "feedback-follow-up"

export type FeedbackDetailPermissionStates = Record<
  FeedbackDetailPermissionKind,
  GuestPermissionWireState
>

export type FeedbackDetailRestaurantPermissions = Record<
  FeedbackDetailPermissionKind,
  boolean
>

export type FeedbackDetailPermissionSummaryRow = {
  id: "feedback-follow-up" | "marketing" | "email-marketing" | "sms-marketing"
  label: string
  value: string
}

export type FeedbackDetailRecoveryActions = {
  /** FD-01 / FD-02 primary CTA. */
  respondEnabled: boolean
  respondDisableReason: string | null
  /** FD-01 / FD-02 secondary Add Offer control. */
  addOfferEnabled: boolean
  addOfferHelper: string | null
}

export const FEEDBACK_DETAIL_RECOVERY_COPY = {
  followUpSectionTitle: "Follow-up",
  permissionSummaryTitle: "Permission summary",
  respondCta: "Respond to guest",
  addOfferCta: "Add Offer",
  marketingNotAvailableHelper:
    "Marketing permission is not available for this guest.",
  noContactReason: "No valid contact method available.",
  followUpUnavailableReason:
    "Feedback follow-up is not available for this guest.",
  resolvedReason: "This feedback is already resolved.",
  accountRestrictedReason:
    "Account or provider restriction blocks recovery.",
  followUpAvailable: "Available",
  followUpUnavailable: "Unavailable",
  marketingNotGranted: "Not granted",
  marketingGranted: "Granted",
} as const

const DEFAULT_PERMISSION_STATES: FeedbackDetailPermissionStates = {
  "email-marketing": "not_recorded",
  "sms-marketing": "not_recorded",
  "feedback-follow-up": "not_recorded",
}

const DEFAULT_RESTAURANT_PERMISSIONS: FeedbackDetailRestaurantPermissions = {
  "email-marketing": false,
  "sms-marketing": false,
  "feedback-follow-up": false,
}

export function emptyFeedbackDetailPermissionStates(): FeedbackDetailPermissionStates {
  return { ...DEFAULT_PERMISSION_STATES }
}

export function emptyFeedbackDetailRestaurantPermissions(): FeedbackDetailRestaurantPermissions {
  return { ...DEFAULT_RESTAURANT_PERMISSIONS }
}

export function parseGuestPermissionWireState(
  value: string | null | undefined
): GuestPermissionWireState {
  if (value === "granted" || value === "withdrawn" || value === "not_recorded") {
    return value
  }
  return "not_recorded"
}

export function parseFeedbackDetailPermissionStates(
  raw: Partial<Record<FeedbackDetailPermissionKind, string>> | null | undefined
): FeedbackDetailPermissionStates {
  return {
    "email-marketing": parseGuestPermissionWireState(raw?.["email-marketing"]),
    "sms-marketing": parseGuestPermissionWireState(raw?.["sms-marketing"]),
    "feedback-follow-up": parseGuestPermissionWireState(
      raw?.["feedback-follow-up"]
    ),
  }
}

export function parseFeedbackDetailRestaurantPermissions(
  raw: Partial<Record<FeedbackDetailPermissionKind, boolean>> | null | undefined
): FeedbackDetailRestaurantPermissions {
  return {
    "email-marketing": raw?.["email-marketing"] === true,
    "sms-marketing": raw?.["sms-marketing"] === true,
    "feedback-follow-up": raw?.["feedback-follow-up"] === true,
  }
}

export function isFeedbackFollowUpAvailable(input: {
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
}): boolean {
  return (
    input.restaurantPermissions["feedback-follow-up"]
    && input.permissionStates["feedback-follow-up"] === "granted"
  )
}

/**
 * Channel marketing grant matching the Feedback contact method
 * (recovery send uses that channel).
 */
export function resolveMarketingGrantedChannel(input: {
  contactType: ContactType
  guestContact: string
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
}): "email-marketing" | "sms-marketing" | null {
  const hasContact = input.guestContact.trim() !== ""
  if (!hasContact || input.contactType === "Unknown") {
    return null
  }

  if (input.contactType === "Email") {
    if (
      input.restaurantPermissions["email-marketing"]
      && input.permissionStates["email-marketing"] === "granted"
    ) {
      return "email-marketing"
    }
    return null
  }

  if (input.contactType === "Phone") {
    if (
      input.restaurantPermissions["sms-marketing"]
      && input.permissionStates["sms-marketing"] === "granted"
    ) {
      return "sms-marketing"
    }
    return null
  }

  return null
}

/**
 * FD permission summary rows — follow-up always; marketing as
 * Not granted or the granted Email/SMS channel row.
 */
export function buildFeedbackDetailPermissionSummary(input: {
  contactType: ContactType
  guestContact: string
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
}): FeedbackDetailPermissionSummaryRow[] {
  const followUpAvailable = isFeedbackFollowUpAvailable(input)
  const marketingChannel = resolveMarketingGrantedChannel(input)

  const rows: FeedbackDetailPermissionSummaryRow[] = [
    {
      id: "feedback-follow-up",
      label: "Feedback follow-up",
      value: followUpAvailable
        ? FEEDBACK_DETAIL_RECOVERY_COPY.followUpAvailable
        : FEEDBACK_DETAIL_RECOVERY_COPY.followUpUnavailable,
    },
  ]

  if (marketingChannel === "email-marketing") {
    rows.push({
      id: "email-marketing",
      label: "Email marketing",
      value: FEEDBACK_DETAIL_RECOVERY_COPY.marketingGranted,
    })
  } else if (marketingChannel === "sms-marketing") {
    rows.push({
      id: "sms-marketing",
      label: "SMS marketing",
      value: FEEDBACK_DETAIL_RECOVERY_COPY.marketingGranted,
    })
  } else {
    rows.push({
      id: "marketing",
      label: "Marketing",
      value: FEEDBACK_DETAIL_RECOVERY_COPY.marketingNotGranted,
    })
  }

  return rows
}

/**
 * FD-01 / FD-02 / FD-03 CTA enablement for Feedback detail.
 */
export function deriveFeedbackDetailRecoveryActions(input: {
  contactType: ContactType
  guestContact: string
  workflowStatus: FeedbackWorkflowStatus
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
  /** Provider / billing / account lock from the host when known. */
  accountRestricted?: boolean
}): FeedbackDetailRecoveryActions {
  const copy = FEEDBACK_DETAIL_RECOVERY_COPY
  const hasContact =
    input.guestContact.trim() !== "" && input.contactType !== "Unknown"
  const followUpAvailable = isFeedbackFollowUpAvailable(input)
  const marketingChannel = resolveMarketingGrantedChannel(input)
  const accountRestricted = input.accountRestricted === true

  if (input.workflowStatus === "resolved") {
    return {
      respondEnabled: false,
      respondDisableReason: copy.resolvedReason,
      addOfferEnabled: false,
      addOfferHelper: copy.marketingNotAvailableHelper,
    }
  }

  if (accountRestricted) {
    return {
      respondEnabled: false,
      respondDisableReason: copy.accountRestrictedReason,
      addOfferEnabled: false,
      addOfferHelper: copy.marketingNotAvailableHelper,
    }
  }

  if (!hasContact) {
    return {
      respondEnabled: false,
      respondDisableReason: copy.noContactReason,
      addOfferEnabled: false,
      addOfferHelper: copy.marketingNotAvailableHelper,
    }
  }

  if (!followUpAvailable) {
    return {
      respondEnabled: false,
      respondDisableReason: copy.followUpUnavailableReason,
      addOfferEnabled: false,
      addOfferHelper: copy.marketingNotAvailableHelper,
    }
  }

  // FD-01 / FD-02 — follow-up available + valid contact.
  if (marketingChannel == null) {
    return {
      respondEnabled: true,
      respondDisableReason: null,
      addOfferEnabled: false,
      addOfferHelper: copy.marketingNotAvailableHelper,
    }
  }

  return {
    respondEnabled: true,
    respondDisableReason: null,
    addOfferEnabled: true,
    addOfferHelper: null,
  }
}
