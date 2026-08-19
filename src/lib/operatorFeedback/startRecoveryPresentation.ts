import {
  OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS,
} from "@/lib/operatorUi/operatorWizardChromePresentation"
import type {
  ContactType,
  FeedbackWorkflowStatus,
  LocationGuestMarketingPreference,
} from "@/types/dashboard"
import { isLocationGuestMarketingIneligible } from "@/lib/operatorGuests/locationGuestMarketingPreference"

export type StartRecoveryContactCapability =
  | "email_available"
  | "sms_available"
  | "no_contact"

export type StartRecoveryIntentId =
  | "respond-to-guest"
  | "respond-and-record-internal-action"
  | "record-internal-action-only"
  | "respond-with-recovery-offer"

export type StartRecoveryIntentCard = {
  id: StartRecoveryIntentId
  title: string
  description: string
  enabled: boolean
  disableReason: string | null
}

const NO_CONTACT_REASON = "No contact method available"
const OFFERS_OPT_OUT_REASON = "Guest has opted out of offers"

const INTENT_DEFINITIONS: readonly {
  id: StartRecoveryIntentId
  title: string
  description: string
}[] = [
  {
    id: "respond-to-guest",
    title: "Respond to the guest",
    description:
      "Prepare and send a private response using an available contact method.",
  },
  {
    id: "respond-and-record-internal-action",
    title: "Respond and record an internal action",
    description:
      "Prepare a guest response and record what the restaurant will review or change.",
  },
  {
    id: "record-internal-action-only",
    title: "Record an internal action only",
    description:
      "Document what the restaurant reviewed or changed without contacting the guest.",
  },
  {
    id: "respond-with-recovery-offer",
    title: "Respond with a recovery offer",
    description:
      "Prepare a controlled offer and include it in the guest’s recovery response.",
  },
] as const

/** Capability-only Contact for Start recovery — never raw email/phone. */
export function deriveStartRecoveryContactCapability(
  contactType: ContactType,
  guestContact: string
): StartRecoveryContactCapability {
  const hasContact = guestContact.trim() !== ""
  if (!hasContact || contactType === "Unknown") {
    return "no_contact"
  }
  if (contactType === "Email") {
    return "email_available"
  }
  if (contactType === "Phone") {
    return "sms_available"
  }
  return "no_contact"
}

export function startRecoveryContactCapabilityLabel(
  capability: StartRecoveryContactCapability
): string {
  switch (capability) {
    case "email_available":
      return "Email available"
    case "sms_available":
      return "SMS available"
    case "no_contact":
      return "No contact"
  }
}

/**
 * PRD Start recovery intent cards — order, copy, and disable rules.
 * Resolved disables all; No contact gates Respond*; offers opt-out gates offer only.
 */
export function buildStartRecoveryIntents(input: {
  contactCapability: StartRecoveryContactCapability
  marketingPreference: LocationGuestMarketingPreference | undefined
  workflowStatus: FeedbackWorkflowStatus
}): StartRecoveryIntentCard[] {
  const isResolved = input.workflowStatus === "resolved"
  const hasNoContact = input.contactCapability === "no_contact"
  const marketingIneligible = isLocationGuestMarketingIneligible(
    input.marketingPreference
  )

  return INTENT_DEFINITIONS.map((definition) => {
    if (isResolved) {
      return {
        ...definition,
        enabled: false,
        disableReason: null,
      }
    }

    if (definition.id === "record-internal-action-only") {
      return {
        ...definition,
        enabled: true,
        disableReason: null,
      }
    }

    if (definition.id === "respond-with-recovery-offer") {
      if (hasNoContact) {
        return {
          ...definition,
          enabled: false,
          disableReason: NO_CONTACT_REASON,
        }
      }
      if (marketingIneligible) {
        return {
          ...definition,
          enabled: false,
          disableReason: OFFERS_OPT_OUT_REASON,
        }
      }
      return {
        ...definition,
        enabled: true,
        disableReason: null,
      }
    }

    // Respond to guest / Respond and record
    if (hasNoContact) {
      return {
        ...definition,
        enabled: false,
        disableReason: NO_CONTACT_REASON,
      }
    }

    return {
      ...definition,
      enabled: true,
      disableReason: null,
    }
  })
}

/**
 * Intent cards + Feedback summary reuse shared wizard selectable-card chrome.
 */
export const START_RECOVERY_INTENT_CARD_SURFACE_CLASS =
  OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS

export const START_RECOVERY_INTENT_CARD_CLASS =
  `h-auto w-full items-center justify-start rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent ${START_RECOVERY_INTENT_CARD_SURFACE_CLASS}`

export const START_RECOVERY_INTENT_CARD_SELECTED_CLASS =
  OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS

export const START_RECOVERY_INTENT_CARD_IDLE_CLASS =
  OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS

export const START_RECOVERY_INTENT_CARD_DISABLED_CLASS =
  OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS

export const START_RECOVERY_SUMMARY_CLASS =
  `flex w-full flex-1 flex-col gap-6 rounded-[6px] border p-4 sm:p-5 ${START_RECOVERY_INTENT_CARD_SURFACE_CLASS}`

/** Summary row rules — `--op-divider` (`#e5e5e5` light / `#262626` dark). */
export const START_RECOVERY_SUMMARY_DIVIDER_CLASS = "bg-op-divider"
