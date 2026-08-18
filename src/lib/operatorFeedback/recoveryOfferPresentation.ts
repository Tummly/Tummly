import type {
  RespondToGuestChannel,
  RespondToGuestToneId,
  RespondToGuestWriteEntry,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import { CAMPAIGN_EXISTING_OFFER_PICKER_COPY } from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"

export {
  EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS as RECOVERY_EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS,
  EXISTING_OFFER_PICKER_CARD_CLASS as RECOVERY_EXISTING_OFFER_PICKER_CARD_CLASS,
  EXISTING_OFFER_PICKER_CARD_META_CLASS as RECOVERY_EXISTING_OFFER_PICKER_CARD_META_CLASS,
  EXISTING_OFFER_PICKER_CARD_TITLE_CLASS as RECOVERY_EXISTING_OFFER_PICKER_CARD_TITLE_CLASS,
  EXISTING_OFFER_PICKER_ICON_WELL_CLASS as RECOVERY_EXISTING_OFFER_PICKER_ICON_WELL_CLASS,
  EXISTING_OFFER_PICKER_PANEL_CLASS as RECOVERY_EXISTING_OFFER_PICKER_PANEL_CLASS,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"

/** Fixed purpose for Respond with a recovery offer. */
export const RECOVERY_OFFER_PURPOSE_ID = "include_a_recovery_offer" as const
export const RECOVERY_OFFER_PURPOSE_LABEL = "Include a recovery offer"

export type RecoveryOfferTypeId =
  | "percentage_discount"
  | "fixed_discount"
  | "free_item"
  | "replacement_item"

export type RecoveryOfferPurchaseRequirementId =
  | "no_purchase_required"
  | "with_any_purchase"
  | "with_minimum_spend"

export type RecoveryOfferValidityId =
  | "7_days_after_issue"
  | "14_days_after_issue"
  | "30_days_after_issue"
  | "choose_expiry_date"

/** Offer-step stance for Respond with a recovery offer (no “No offer”). */
export type RecoveryOfferStanceId = "create-and-select" | "existing-offer"

export type RecoveryOfferStanceOption = {
  id: RecoveryOfferStanceId
  title: string
  description: string
  disabled: boolean
}

export type RecoveryOfferStanceOptionViewModel = RecoveryOfferStanceOption & {
  selected: boolean
}

export const RECOVERY_OFFER_STEP_COPY = {
  stepHeading: "Choose a recovery offer",
  stepDescription:
    "Create an offer from the catalog, or attach an existing Active catalog offer.",
  attachedSummaryEdit: "Edit",
  attachedSummaryFallbackTitle: "Attached offer",
  createOfferError: "Could not create this offer. Try again.",
} as const

export const RECOVERY_OFFER_STANCE_OPTIONS: readonly RecoveryOfferStanceOption[] =
  [
    {
      id: "existing-offer",
      title: "Existing offer",
      description:
        "Best for short, time-sensitive messages and simple offer reminders.",
      disabled: false,
    },
    {
      id: "create-and-select",
      title: "Create and select",
      description:
        "Define the benefit, validity and redemption rules, then attach the new catalog offer.",
      disabled: false,
    },
  ] as const

/** Reuse Campaign Existing picker chrome; recovery-scoped empty helper. */
export const RECOVERY_EXISTING_OFFER_PICKER_COPY = {
  ...CAMPAIGN_EXISTING_OFFER_PICKER_COPY,
  emptyHelper: "No active offers yet.",
} as const

export type RespondWithRecoveryOfferWizardStep =
  | "setup"
  | "offer"
  | "write"
  | "review"
  | "success"

export const RECOVERY_OFFER_TITLE_MAX = 60
export const RECOVERY_OFFER_DESCRIPTION_MAX = 240

export const DEFAULT_STAFF_INSTRUCTIONS =
  "Ask the guest to show this unique code. Redeem once at the till."

export type RecoveryOfferTypeOption = {
  id: RecoveryOfferTypeId
  label: string
  /** Figma offer-type card subtitle — display-only chrome. */
  description: string
}

export const RECOVERY_OFFER_TYPE_OPTIONS: readonly RecoveryOfferTypeOption[] = [
  {
    id: "percentage_discount",
    label: "Percentage discount",
    description:
      "Give the guest a percentage off their next eligible purchase.",
  },
  {
    id: "fixed_discount",
    label: "Fixed discount",
    description:
      "Give the guest a fixed monetary amount off their next eligible purchase.",
  },
  {
    id: "free_item",
    label: "Free item",
    description:
      "Offer one specified item with or without a qualifying purchase.",
  },
  {
    id: "replacement_item",
    label: "Replacement item",
    description:
      "Allow the guest to receive a replacement for a specific item.",
  },
] as const

export type RecoveryOfferPurchaseRequirementOption = {
  id: RecoveryOfferPurchaseRequirementId
  label: string
}

export const RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS: readonly RecoveryOfferPurchaseRequirementOption[] =
  [
    { id: "no_purchase_required", label: "No purchase required" },
    { id: "with_any_purchase", label: "With any purchase" },
    { id: "with_minimum_spend", label: "With a minimum spend" },
  ] as const

export const RECOVERY_OFFER_VALIDITY_OPTIONS: readonly {
  id: RecoveryOfferValidityId
  label: string
}[] = [
  { id: "7_days_after_issue", label: "7 days after issue" },
  { id: "14_days_after_issue", label: "14 days after issue" },
  { id: "30_days_after_issue", label: "30 days after issue" },
  { id: "choose_expiry_date", label: "Choose an expiry date" },
] as const

export type RecoveryOfferDetailsDraft = {
  offerType: RecoveryOfferTypeId | null
  discountPercentage: string
  discountAmount: string
  freeItemText: string
  purchaseRequirement: RecoveryOfferPurchaseRequirementId | null
  minimumSpend: string
  additionalExclusions: string
  replacementItemText: string
  title: string
  /** When true, title was edited by the operator — stop auto-overwrite. */
  titleTouched: boolean
  description: string
  validity: RecoveryOfferValidityId
  expiryDate: string
  staffInstructions: string
  offerComplete: boolean
}

export type RespondWithRecoveryOfferDraft = {
  channel: RespondToGuestChannel | null
  tone: RespondToGuestToneId | null
  includeNotes: string
  setupComplete: boolean
  /** Durable catalog Recovery offer attach. Null when none. */
  offerId: number | null
  offer: RecoveryOfferDetailsDraft
  subject: string
  message: string
  messageComplete: boolean
  writeEntry: RespondToGuestWriteEntry
}

export function emptyRecoveryOfferDetailsDraft(): RecoveryOfferDetailsDraft {
  return {
    offerType: null,
    discountPercentage: "",
    discountAmount: "",
    freeItemText: "",
    purchaseRequirement: null,
    minimumSpend: "",
    additionalExclusions: "",
    replacementItemText: "",
    title: "",
    titleTouched: false,
    description: "",
    validity: "30_days_after_issue",
    expiryDate: "",
    staffInstructions: DEFAULT_STAFF_INSTRUCTIONS,
    offerComplete: false,
  }
}

export function emptyRespondWithRecoveryOfferDraft(): RespondWithRecoveryOfferDraft {
  return {
    channel: null,
    tone: null,
    includeNotes: "",
    setupComplete: false,
    offerId: null,
    offer: emptyRecoveryOfferDetailsDraft(),
    subject: "",
    message: "",
    messageComplete: false,
    writeEntry: "chooser",
  }
}

export function canContinueRespondWithRecoveryOfferSetup(input: {
  channel: RespondToGuestChannel | null
  tone: RespondToGuestToneId | null
}): boolean {
  return input.channel != null && input.tone != null
}

/** Continue Offer step when a durable Active Recovery attach is present. */
export function canContinueRecoveryOfferAttach(input: {
  offerId: number | null
  attachedOfferStatus: string | null
}): boolean {
  return input.offerId != null && input.attachedOfferStatus === "active"
}

function parsePositiveNumber(raw: string): number | null {
  const trimmed = raw.trim()
  if (trimmed === "") {
    return null
  }
  const value = Number(trimmed)
  if (!Number.isFinite(value) || value <= 0) {
    return null
  }
  return value
}

export function canContinueRecoveryOfferDetails(
  offer: RecoveryOfferDetailsDraft
): boolean {
  if (offer.offerType == null) {
    return false
  }
  if (offer.title.trim() === "") {
    return false
  }
  if (offer.title.trim().length > RECOVERY_OFFER_TITLE_MAX) {
    return false
  }
  if (offer.description.trim() === "") {
    return false
  }
  if (offer.description.trim().length > RECOVERY_OFFER_DESCRIPTION_MAX) {
    return false
  }

  if (offer.offerType === "percentage_discount") {
    if (parsePositiveNumber(offer.discountPercentage) == null) {
      return false
    }
  } else if (offer.offerType === "fixed_discount") {
    if (parsePositiveNumber(offer.discountAmount) == null) {
      return false
    }
  } else if (offer.offerType === "free_item") {
    if (offer.freeItemText.trim() === "") {
      return false
    }
    if (offer.purchaseRequirement == null) {
      return false
    }
    if (
      offer.purchaseRequirement === "with_minimum_spend"
      && parsePositiveNumber(offer.minimumSpend) == null
    ) {
      return false
    }
  } else if (offer.offerType === "replacement_item") {
    if (offer.replacementItemText.trim() === "") {
      return false
    }
  }

  if (offer.validity === "choose_expiry_date" && offer.expiryDate.trim() === "") {
    return false
  }

  return true
}

/**
 * Auto title from the selected benefit. Operator may edit afterward.
 */
export function autoTitleForRecoveryOffer(
  offer: Pick<
    RecoveryOfferDetailsDraft,
    | "offerType"
    | "discountPercentage"
    | "discountAmount"
    | "freeItemText"
    | "replacementItemText"
  >
): string {
  if (offer.offerType === "percentage_discount") {
    const pct = offer.discountPercentage.trim()
    if (pct !== "" && parsePositiveNumber(pct) != null) {
      return `${pct}% off`.slice(0, RECOVERY_OFFER_TITLE_MAX)
    }
    return "Percentage discount"
  }
  if (offer.offerType === "fixed_discount") {
    const amount = offer.discountAmount.trim()
    if (amount !== "" && parsePositiveNumber(amount) != null) {
      return `£${amount} off`.slice(0, RECOVERY_OFFER_TITLE_MAX)
    }
    return "Fixed discount"
  }
  if (offer.offerType === "free_item") {
    const item = offer.freeItemText.trim()
    if (item !== "") {
      return `Free ${item}`.slice(0, RECOVERY_OFFER_TITLE_MAX)
    }
    return "Free item"
  }
  if (offer.offerType === "replacement_item") {
    const item = offer.replacementItemText.trim()
    if (item !== "") {
      return `Replacement: ${item}`.slice(0, RECOVERY_OFFER_TITLE_MAX)
    }
    return "Replacement item"
  }
  return ""
}

export function labelForRecoveryOfferType(
  offerType: RecoveryOfferTypeId | null
): string | null {
  if (offerType == null) {
    return null
  }
  return (
    RECOVERY_OFFER_TYPE_OPTIONS.find((o) => o.id === offerType)?.label ?? null
  )
}

export function labelForRecoveryOfferValidity(
  validity: RecoveryOfferValidityId
): string {
  return (
    RECOVERY_OFFER_VALIDITY_OPTIONS.find((o) => o.id === validity)?.label
    ?? validity
  )
}

export function labelForPurchaseRequirement(
  id: RecoveryOfferPurchaseRequirementId | null
): string | null {
  if (id == null) {
    return null
  }
  return (
    RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS.find((o) => o.id === id)
      ?.label ?? null
  )
}

/**
 * Resume at furthest incomplete compose step (PRD).
 * Success is not draft-resumeable.
 */
export function furthestRespondWithRecoveryOfferStep(
  draft: RespondWithRecoveryOfferDraft
): Extract<
  RespondWithRecoveryOfferWizardStep,
  "setup" | "offer" | "write" | "review"
> {
  if (!draft.setupComplete) {
    return "setup"
  }
  if (draft.offerId == null || !draft.offer.offerComplete) {
    return "offer"
  }
  if (!draft.messageComplete) {
    return "write"
  }
  return "review"
}

/** Compact summary line for Review / Guest response offer card. */
export function formatRecoveryOfferSummaryLine(
  offer: RecoveryOfferDetailsDraft
): string {
  const typeLabel = labelForRecoveryOfferType(offer.offerType) ?? "Offer"
  const title = offer.title.trim() || typeLabel
  return title
}

export type ConfirmedRecoveryOfferPayload = {
  offerType: RecoveryOfferTypeId
  title: string
  description: string
  validity: RecoveryOfferValidityId
  expiryDate: string | null
  discountPercentage: number | null
  discountAmount: number | null
  freeItemText: string | null
  purchaseRequirement: RecoveryOfferPurchaseRequirementId | null
  minimumSpend: number | null
  additionalExclusions: string | null
  replacementItemText: string | null
  staffInstructions: string | null
}

export function toConfirmedRecoveryOfferPayload(
  offer: RecoveryOfferDetailsDraft
): ConfirmedRecoveryOfferPayload | null {
  if (!canContinueRecoveryOfferDetails(offer) || offer.offerType == null) {
    return null
  }

  return {
    offerType: offer.offerType,
    title: offer.title.trim(),
    description: offer.description.trim(),
    validity: offer.validity,
    expiryDate:
      offer.validity === "choose_expiry_date"
        ? offer.expiryDate.trim()
        : null,
    discountPercentage:
      offer.offerType === "percentage_discount"
        ? parsePositiveNumber(offer.discountPercentage)
        : null,
    discountAmount:
      offer.offerType === "fixed_discount"
        ? parsePositiveNumber(offer.discountAmount)
        : null,
    freeItemText:
      offer.offerType === "free_item" ? offer.freeItemText.trim() : null,
    purchaseRequirement:
      offer.offerType === "free_item" ? offer.purchaseRequirement : null,
    minimumSpend:
      offer.offerType === "free_item"
      && offer.purchaseRequirement === "with_minimum_spend"
        ? parsePositiveNumber(offer.minimumSpend)
        : null,
    additionalExclusions:
      offer.offerType === "free_item"
      && offer.additionalExclusions.trim() !== ""
        ? offer.additionalExclusions.trim()
        : null,
    replacementItemText:
      offer.offerType === "replacement_item"
        ? offer.replacementItemText.trim()
        : null,
    staffInstructions:
      offer.staffInstructions.trim() === ""
        ? null
        : offer.staffInstructions.trim(),
  }
}
