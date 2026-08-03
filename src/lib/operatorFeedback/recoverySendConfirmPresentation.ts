/**
 * Figma-literal send/record confirm copy for recovery wizards (U-12 / ticket 39).
 * Masked destination stays required by PRD for guest-send intents.
 */

export type RecoverySendConfirmIntent =
  | "respond_to_guest"
  | "respond_and_record_internal_action"
  | "record_internal_action_only"
  | "respond_with_recovery_offer"

export type RecoverySendConfirmStatus = "idle" | "saving" | "error"

export type RecoverySendConfirmCopy = {
  title: string
  description: string
  confirmLabel: string
}

function trimmedDestination(
  maskedDestination: string | null
): string | null {
  const trimmed = maskedDestination?.trim()
  return trimmed != null && trimmed.length > 0 ? trimmed : null
}

export function recoverySendConfirmCopy(input: {
  intent: RecoverySendConfirmIntent
  maskedDestination: string | null
  sendStatus: RecoverySendConfirmStatus
}): RecoverySendConfirmCopy {
  const destination = trimmedDestination(input.maskedDestination)
  const retryLabel =
    input.sendStatus === "error" ? ("Send again" as const) : null

  if (input.intent === "respond_to_guest") {
    return {
      title: "Send this response?",
      description:
        destination != null
          ? `This will send the message to ${destination} and record the response against this feedback.`
          : "This will send the message and record the response against this feedback.",
      confirmLabel: retryLabel ?? "Send response",
    }
  }

  if (input.intent === "respond_and_record_internal_action") {
    return {
      title: "Send response and record internal action?",
      description:
        destination != null
          ? `This will send the response to ${destination} and record the internal follow-up against this feedback.`
          : "This will send the response and record the internal follow-up against this feedback.",
      confirmLabel: retryLabel ?? "Send and record",
    }
  }

  if (input.intent === "record_internal_action_only") {
    return {
      title: "Record internal follow up?",
      description:
        "This will record the internal follow-up against this feedback.",
      // Figma debt — PRD keeps guest-send phrasing on the confirm primary.
      confirmLabel: "Send and record",
    }
  }

  return {
    title: "Send response and issue offer?",
    description:
      destination != null
        ? `This will send the response to ${destination} and activate the recovery offer for this guest.`
        : "This will send the response and activate the recovery offer for this guest.",
    confirmLabel: "Send and issue offer",
  }
}
