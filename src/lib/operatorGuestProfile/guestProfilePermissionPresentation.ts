import {
  parseFeedbackDetailPermissionStates,
  parseFeedbackDetailRestaurantPermissions,
  type FeedbackDetailPermissionKind,
  type FeedbackDetailPermissionStates,
  type FeedbackDetailRestaurantPermissions,
  type GuestPermissionWireState,
} from "@/lib/operatorFeedback/feedbackDetailRecoveryPresentation"

/**
 * Guest profile permission summary (handoff surface 4).
 * Separate rows for Feedback follow-up / Email marketing / SMS marketing.
 * Do not label a guest simply “consented” when only follow-up is available.
 */

export const GUEST_PROFILE_PERMISSION_COPY = {
  sectionTitle: "Permissions",
  feedbackFollowUp: "Feedback follow-up",
  emailMarketing: "Email marketing",
  smsMarketing: "SMS marketing",
  granted: "Granted",
  notGranted: "Not granted",
  withdrawn: "Withdrawn",
  suppressed: "Suppressed",
  invalidContact: "Invalid contact",
  available: "Available",
  unavailable: "Unavailable",
} as const

export type GuestProfilePermissionDisplayState =
  | "granted"
  | "not_granted"
  | "withdrawn"
  | "suppressed"
  | "invalid_contact"
  | "available"
  | "unavailable"

export type GuestProfilePermissionSummaryRow = {
  id: FeedbackDetailPermissionKind
  label: string
  value: string
  state: GuestProfilePermissionDisplayState
}

function hasContactValue(value: string | null | undefined): boolean {
  return (value ?? "").trim() !== ""
}

function marketingDisplayState(input: {
  kind: "email-marketing" | "sms-marketing"
  wireState: GuestPermissionWireState
  restaurantEnabled: boolean
  contactPresent: boolean
}): GuestProfilePermissionDisplayState {
  if (!input.contactPresent) {
    return "invalid_contact"
  }
  if (!input.restaurantEnabled) {
    return "suppressed"
  }
  if (input.wireState === "granted") {
    return "granted"
  }
  if (input.wireState === "withdrawn") {
    return "withdrawn"
  }
  return "not_granted"
}

function followUpDisplayState(input: {
  wireState: GuestPermissionWireState
  restaurantEnabled: boolean
}): GuestProfilePermissionDisplayState {
  if (!input.restaurantEnabled) {
    return "unavailable"
  }
  if (input.wireState === "granted") {
    return "available"
  }
  if (input.wireState === "withdrawn") {
    return "withdrawn"
  }
  return "unavailable"
}

function labelForDisplayState(
  state: GuestProfilePermissionDisplayState
): string {
  const copy = GUEST_PROFILE_PERMISSION_COPY
  switch (state) {
    case "granted":
      return copy.granted
    case "not_granted":
      return copy.notGranted
    case "withdrawn":
      return copy.withdrawn
    case "suppressed":
      return copy.suppressed
    case "invalid_contact":
      return copy.invalidContact
    case "available":
      return copy.available
    case "unavailable":
      return copy.unavailable
  }
}

/**
 * Build the three permission rows for Guest profile.
 */
export function buildGuestProfilePermissionSummary(input: {
  email: string | null | undefined
  mobile: string | null | undefined
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
}): GuestProfilePermissionSummaryRow[] {
  const copy = GUEST_PROFILE_PERMISSION_COPY

  const followUpState = followUpDisplayState({
    wireState: input.permissionStates["feedback-follow-up"],
    restaurantEnabled: input.restaurantPermissions["feedback-follow-up"],
  })

  const emailState = marketingDisplayState({
    kind: "email-marketing",
    wireState: input.permissionStates["email-marketing"],
    restaurantEnabled: input.restaurantPermissions["email-marketing"],
    contactPresent: hasContactValue(input.email),
  })

  const smsState = marketingDisplayState({
    kind: "sms-marketing",
    wireState: input.permissionStates["sms-marketing"],
    restaurantEnabled: input.restaurantPermissions["sms-marketing"],
    contactPresent: hasContactValue(input.mobile),
  })

  return [
    {
      id: "feedback-follow-up",
      label: copy.feedbackFollowUp,
      state: followUpState,
      value: labelForDisplayState(followUpState),
    },
    {
      id: "email-marketing",
      label: copy.emailMarketing,
      state: emailState,
      value: labelForDisplayState(emailState),
    },
    {
      id: "sms-marketing",
      label: copy.smsMarketing,
      state: smsState,
      value: labelForDisplayState(smsState),
    },
  ]
}

export function buildGuestProfilePermissionSummaryFromDetails(input: {
  email: string | null | undefined
  mobile: string | null | undefined
  permissionStates?: Partial<
    Record<FeedbackDetailPermissionKind, string>
  > | null
  restaurantPermissionEnabled?: Partial<
    Record<FeedbackDetailPermissionKind, boolean>
  > | null
}): GuestProfilePermissionSummaryRow[] | null {
  if (
    input.permissionStates == null
    && input.restaurantPermissionEnabled == null
  ) {
    return null
  }

  return buildGuestProfilePermissionSummary({
    email: input.email,
    mobile: input.mobile,
    permissionStates: parseFeedbackDetailPermissionStates(
      input.permissionStates
    ),
    restaurantPermissions: parseFeedbackDetailRestaurantPermissions(
      input.restaurantPermissionEnabled
    ),
  })
}
