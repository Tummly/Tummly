import type { ContactType, LocationGuestMarketingPreference } from "@/types/dashboard"
import {
  parseFeedbackDetailPermissionStates,
  parseFeedbackDetailRestaurantPermissions,
  resolveMarketingGrantedChannel,
  type FeedbackDetailPermissionStates,
  type FeedbackDetailRestaurantPermissions,
} from "@/lib/operatorFeedback/feedbackDetailRecoveryPresentation"

/**
 * Recovery composer (RC-01 / RC-02 / RC-03) — status banner + send-state copy.
 * Exact handoff strings; no alternate recovery model.
 */

export const RECOVERY_COMPOSER_COPY = {
  pageTitle: "Respond to guest",
  serviceOnlyBanner:
    "Service response only — promotional Offers are unavailable because this guest has not granted marketing permission.",
  marketingEligibleEmail: "Marketing eligible for Email.",
  marketingEligibleSms: "Marketing eligible for SMS.",
  addOfferCta: "Add Offer",
  eligibilityChanged:
    "Marketing eligibility changed. The attached Offer was removed. Promotional Offers are unavailable until marketing permission is granted again.",
  noPermission: "This guest does not have marketing permission for Offers.",
  providerUnavailable:
    "The message provider is unavailable. Try again later.",
  sendFailed: "Could not send the response. Please try again.",
} as const

export type RecoveryComposerMarketingChannel = "email" | "sms"

export type RecoveryComposerStatusBanner = {
  kind: "service-only" | "marketing-eligible"
  message: string
}

export type RecoveryComposerSendFailureKind =
  | "eligibility_changed"
  | "no_permission"
  | "provider_unavailable"
  | "failed"

export type RecoveryComposerSendFailure = {
  kind: RecoveryComposerSendFailureKind
  message: string
}

/**
 * Channel marketing grant for the Feedback contact (Email → email, Phone → sms).
 */
export function resolveRecoveryComposerMarketingChannel(input: {
  contactType: ContactType
  guestContact: string
  permissionStates: FeedbackDetailPermissionStates
  restaurantPermissions: FeedbackDetailRestaurantPermissions
}): RecoveryComposerMarketingChannel | null {
  const kind = resolveMarketingGrantedChannel(input)
  if (kind === "email-marketing") {
    return "email"
  }
  if (kind === "sms-marketing") {
    return "sms"
  }
  return null
}

function marketingPreferenceFallbackChannel(input: {
  contactType: ContactType
  guestContact: string
  marketingPreference?: LocationGuestMarketingPreference | null
}): RecoveryComposerMarketingChannel | null {
  if (input.marketingPreference !== "allowed") {
    return null
  }
  if (input.guestContact.trim() === "" || input.contactType === "Unknown") {
    return null
  }
  if (input.contactType === "Email") {
    return "email"
  }
  if (input.contactType === "Phone") {
    return "sms"
  }
  return null
}

/**
 * Resolve marketing channel from Feedback details.
 * When ledger wire fields are present, use them. When omitted (legacy fixtures),
 * fall back to the marketingPreference rollup for the contact channel.
 */
export function resolveRecoveryComposerMarketingChannelFromDetails(input: {
  contactType: ContactType
  guestContact: string
  permissionStates?: Partial<
    Record<"email-marketing" | "sms-marketing" | "feedback-follow-up", string>
  > | null
  restaurantPermissionEnabled?: Partial<
    Record<"email-marketing" | "sms-marketing" | "feedback-follow-up", boolean>
  > | null
  marketingPreference?: LocationGuestMarketingPreference | null
}): RecoveryComposerMarketingChannel | null {
  const hasLedgerPayload =
    input.permissionStates != null || input.restaurantPermissionEnabled != null

  if (hasLedgerPayload) {
    return resolveRecoveryComposerMarketingChannel({
      contactType: input.contactType,
      guestContact: input.guestContact,
      permissionStates: parseFeedbackDetailPermissionStates(
        input.permissionStates
      ),
      restaurantPermissions: parseFeedbackDetailRestaurantPermissions(
        input.restaurantPermissionEnabled
      ),
    })
  }

  return marketingPreferenceFallbackChannel(input)
}

/** RC-01 / RC-02 status banner from current marketing channel grant. */
export function buildRecoveryComposerStatusBanner(
  marketingChannel: RecoveryComposerMarketingChannel | null
): RecoveryComposerStatusBanner {
  if (marketingChannel === "email") {
    return {
      kind: "marketing-eligible",
      message: RECOVERY_COMPOSER_COPY.marketingEligibleEmail,
    }
  }
  if (marketingChannel === "sms") {
    return {
      kind: "marketing-eligible",
      message: RECOVERY_COMPOSER_COPY.marketingEligibleSms,
    }
  }
  return {
    kind: "service-only",
    message: RECOVERY_COMPOSER_COPY.serviceOnlyBanner,
  }
}

/**
 * When marketing grant is lost while an Offer is attached, detach before send.
 */
export function shouldDetachOfferOnEligibilityLoss(input: {
  previousChannel: RecoveryComposerMarketingChannel | null
  nextChannel: RecoveryComposerMarketingChannel | null
  hasAttachedOffer: boolean
}): boolean {
  return (
    input.hasAttachedOffer
    && input.previousChannel != null
    && input.nextChannel == null
  )
}

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }
  if (typeof error === "string") {
    return error
  }
  return ""
}

/** Map API / transport failures to RC-03 send-state copy. */
export function mapRecoveryComposerSendFailure(
  error: unknown
): RecoveryComposerSendFailure {
  const message = errorMessage(error).toLowerCase()

  if (
    message.includes("opted out")
    || message.includes("marketing permission")
    || message.includes("not granted marketing")
  ) {
    return {
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    }
  }

  if (
    message.includes("eligibility")
    || message.includes("permission changed")
  ) {
    return {
      kind: "eligibility_changed",
      message: RECOVERY_COMPOSER_COPY.eligibilityChanged,
    }
  }

  if (
    message.includes("provider")
    || message.includes("unavailable")
    || message.includes("not available")
    || message.includes("503")
    || message.includes("service unavailable")
  ) {
    return {
      kind: "provider_unavailable",
      message: RECOVERY_COMPOSER_COPY.providerUnavailable,
    }
  }

  return {
    kind: "failed",
    message: RECOVERY_COMPOSER_COPY.sendFailed,
  }
}
