import { maskEmail } from "@/lib/signInOtp"
import type { ContactType } from "@/types/dashboard"
import type { StartRecoveryContactCapability } from "@/lib/operatorFeedback/startRecoveryPresentation"

export type RespondToGuestChannel = "email" | "sms"

export type RespondToGuestPurposeId =
  | "acknowledge_feedback"
  | "apologise_and_confirm_follow_up"
  | "ask_for_more_information"
  | "confirm_operational_action"
  | "create_custom_response"

export type RespondToGuestToneId =
  | "warm_and_apologetic"
  | "direct_and_practical"
  | "appreciative"
  | "use_restaurant_tone"

export type RespondToGuestWizardStep =
  | "setup"
  | "write"
  | "review"
  | "success"

export type RespondToGuestWriteEntry = "chooser" | "editor"

export type RespondToGuestDraft = {
  channel: RespondToGuestChannel | null
  purpose: RespondToGuestPurposeId | null
  tone: RespondToGuestToneId | null
  includeNotes: string
  subject: string
  message: string
  setupComplete: boolean
  messageComplete: boolean
  /** Guest response: chooser (Prepare / Write manually) vs editor. */
  writeEntry: RespondToGuestWriteEntry
}

export const RESPOND_TO_GUEST_PURPOSE_OPTIONS: readonly {
  id: RespondToGuestPurposeId
  label: string
}[] = [
  { id: "acknowledge_feedback", label: "Acknowledge the feedback" },
  {
    id: "apologise_and_confirm_follow_up",
    label: "Apologise and confirm follow-up",
  },
  { id: "ask_for_more_information", label: "Ask for more information" },
  {
    id: "confirm_operational_action",
    label: "Confirm an operational action",
  },
  { id: "create_custom_response", label: "Create a custom response" },
] as const

export const RESPOND_TO_GUEST_TONE_OPTIONS: readonly {
  id: RespondToGuestToneId
  label: string
}[] = [
  { id: "warm_and_apologetic", label: "Warm and apologetic" },
  { id: "direct_and_practical", label: "Direct and practical" },
  { id: "appreciative", label: "Appreciative" },
  { id: "use_restaurant_tone", label: "Use restaurant tone" },
] as const

export function emptyRespondToGuestDraft(): RespondToGuestDraft {
  return {
    channel: null,
    purpose: null,
    tone: null,
    includeNotes: "",
    subject: "",
    message: "",
    setupComplete: false,
    messageComplete: false,
    writeEntry: "chooser",
  }
}

export function availableRespondToGuestChannels(
  capability: StartRecoveryContactCapability
): RespondToGuestChannel[] {
  if (capability === "email_available") {
    return ["email"]
  }
  if (capability === "sms_available") {
    return ["sms"]
  }
  return []
}

export function defaultRespondToGuestChannel(
  capability: StartRecoveryContactCapability
): RespondToGuestChannel | null {
  const channels = availableRespondToGuestChannels(capability)
  return channels[0] ?? null
}

/** Masked destination for UI and confirm — never raw email/phone. */
export function maskRespondToGuestDestination(
  contactType: ContactType,
  guestContact: string
): string {
  const trimmed = guestContact.trim()
  if (contactType === "Email") {
    return maskEmail(trimmed)
  }
  if (contactType === "Phone") {
    const digits = trimmed.replace(/\D/g, "")
    if (digits.length < 4) {
      return "••••"
    }
    return `••••${digits.slice(-4)}`
  }
  return "••••"
}

export function canContinueRespondToGuestSetup(input: {
  channel: RespondToGuestChannel | null
  purpose: RespondToGuestPurposeId | null
  tone: RespondToGuestToneId | null
}): boolean {
  return input.channel != null && input.purpose != null && input.tone != null
}

export function canContinueRespondToGuestMessage(input: {
  channel: RespondToGuestChannel | null
  subject: string | null
  message: string
}): boolean {
  if (input.channel == null) {
    return false
  }
  if (input.message.trim() === "") {
    return false
  }
  if (input.channel === "email" && (input.subject ?? "").trim() === "") {
    return false
  }
  return true
}

/**
 * Resume at furthest incomplete compose step (PRD: left/furthest incomplete).
 * Success is not draft-resumeable.
 */
export function furthestRespondToGuestStep(
  draft: RespondToGuestDraft
): Extract<RespondToGuestWizardStep, "setup" | "write" | "review"> {
  if (!draft.setupComplete) {
    return "setup"
  }
  if (!draft.messageComplete) {
    return "write"
  }
  return "review"
}

export function labelForRespondToGuestPurpose(
  purpose: RespondToGuestPurposeId | null
): string | null {
  if (purpose == null) {
    return null
  }
  return (
    RESPOND_TO_GUEST_PURPOSE_OPTIONS.find((o) => o.id === purpose)?.label
    ?? null
  )
}

export function labelForRespondToGuestTone(
  tone: RespondToGuestToneId | null
): string | null {
  if (tone == null) {
    return null
  }
  return RESPOND_TO_GUEST_TONE_OPTIONS.find((o) => o.id === tone)?.label ?? null
}
