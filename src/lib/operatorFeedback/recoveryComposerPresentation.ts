import { isAxiosError } from "axios"

import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"

/**
 * Recovery composer (RC-01 / RC-02 / RC-03) — status banner + send-state copy.
 * Exact handoff strings; no alternate recovery model.
 */

export const RECOVERY_COMPOSER_COPY = {
  pageTitle: "Respond to guest",
  serviceReply:
    "Service reply to this guest's Feedback. Marketing permission is not needed.",
  addOfferCta: "Add Offer",
  noPermission: "This guest has not asked for a reply to their Feedback.",
  providerUnavailable:
    "The message provider is unavailable. Try again later.",
  /** Server refusal after a permanent Email hard bounce. Names no guest data. */
  emailDeliveryUnavailable:
    "Email delivery unavailable. Use a verified alternative contact method.",
  /** Server refusal after SMS STOP on this sender. Permission is unchanged. */
  smsProviderRestriction:
    "SMS not sent. This number has opted out of SMS from this sender, and delivery is currently blocked. This does not change the guest's Feedback follow-up permission.",
  sendFailed: "Could not send the response. Please try again.",
} as const

/** RC-01 / RC-02 status wash — Main Bg (light gray-55 / dark gray-995). */
export const RECOVERY_COMPOSER_STATUS_BANNER_CLASS =
  "flex w-full flex-col gap-3 rounded-[4px] bg-op-background-primary p-[18px]"

export type RecoveryComposerStatusBanner = {
  kind: "service-only"
  message: string
}

export type RecoveryComposerSendFailureKind =
  | "no_permission"
  | "provider_unavailable"
  | "refused"
  | "failed"

export type RecoveryComposerSendFailure = {
  kind: RecoveryComposerSendFailureKind
  message: string
}

/** Recovery composer status banner — service reply; marketing is not required. */
export function buildRecoveryComposerStatusBanner(): RecoveryComposerStatusBanner {
  return {
    kind: "service-only",
    message: RECOVERY_COMPOSER_COPY.serviceReply,
  }
}

function errorMessage(error: unknown): string {
  if (typeof error === "string") {
    return error
  }
  return getUserFacingApiErrorMessage(error, "")
}

/** Reason code the server returns with a service-reply refusal, if any. */
function refusalCode(error: unknown): string | null {
  if (!isAxiosError(error)) {
    return null
  }
  const data: unknown = error.response?.data
  if (typeof data !== "object" || data === null) {
    return null
  }
  const code = (data as { code?: unknown }).code
  return typeof code === "string" ? code : null
}

/**
 * Maps a service-reply refusal by its server code. Null when the error carries
 * no known refusal code, so the caller falls back to message matching.
 */
export function mapRecoveryComposerRefusalCode(
  error: unknown
): RecoveryComposerSendFailure | null {
  switch (refusalCode(error)) {
    case "feedback_follow_up_not_granted":
      return { kind: "no_permission", message: RECOVERY_COMPOSER_COPY.noPermission }
    case "sms_provider_restriction":
      return {
        kind: "refused",
        message: RECOVERY_COMPOSER_COPY.smsProviderRestriction,
      }
    case "email_delivery_unavailable":
      return {
        kind: "refused",
        message: RECOVERY_COMPOSER_COPY.emailDeliveryUnavailable,
      }
    default:
      return null
  }
}

/** Map API / transport failures to RC-03 send-state copy. */
export function mapRecoveryComposerSendFailure(
  error: unknown
): RecoveryComposerSendFailure {
  // Coded refusals first. The SMS STOP copy mentions "feedback follow-up
  // permission", which must not read as a permission refusal.
  const coded = mapRecoveryComposerRefusalCode(error)
  if (coded !== null) {
    return coded
  }

  // 409 refusals (duplicate reply, open recovery Offer) and 502 SMS send
  // failures carry a message the operator needs to read. Keep it instead of
  // the generic copy.
  if (
    isAxiosError(error)
    && (error.response?.status === 409 || error.response?.status === 502)
  ) {
    const refusal = errorMessage(error)
    if (refusal !== "") {
      return { kind: "refused", message: refusal }
    }
  }

  const message = errorMessage(error).toLowerCase()

  if (message.includes("opted out of sms from this sender")) {
    return {
      kind: "refused",
      message: RECOVERY_COMPOSER_COPY.smsProviderRestriction,
    }
  }

  if (message.includes("feedback follow-up permission")) {
    return {
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    }
  }

  if (message.includes("email delivery unavailable")) {
    return {
      kind: "refused",
      message: RECOVERY_COMPOSER_COPY.emailDeliveryUnavailable,
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
