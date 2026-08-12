/**
 * Offers catalog create/edit draft helpers — field set mirrors Recovery offer patterns.
 * APIs stay on /api/offers (not Feedback recovery endpoints).
 */

import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

export type CampaignCatalogOfferTypeId =
  | "percentage_discount"
  | "fixed_discount"
  | "free_item"
  | "replacement_item"

export type CampaignCatalogOfferPurchaseRequirementId =
  | "no_purchase_required"
  | "with_any_purchase"
  | "with_minimum_spend"

export type CampaignCatalogOfferValidityId =
  | "7_days_after_issue"
  | "14_days_after_issue"
  | "30_days_after_issue"
  | "choose_expiry_date"

export const CAMPAIGN_OFFER_TITLE_MAX = 60
export const CAMPAIGN_OFFER_DESCRIPTION_MAX = 240

/** Figma Create Offer — default staff instructions. */
export const OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS =
  "Ask the guest for their unique offer code. Check the code in Tummly and select Confirm redemption before applying the offer."

/** @deprecated Prefer OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS */
export const CAMPAIGN_OFFER_DEFAULT_STAFF_INSTRUCTIONS =
  OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS

export type CampaignCatalogOfferTypeOption = {
  id: CampaignCatalogOfferTypeId
  label: string
  description: string
}

export const CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS: readonly CampaignCatalogOfferTypeOption[] =
  [
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

export const CAMPAIGN_CATALOG_OFFER_PURCHASE_REQUIREMENT_OPTIONS: readonly {
  id: CampaignCatalogOfferPurchaseRequirementId
  label: string
}[] = [
  { id: "no_purchase_required", label: "No purchase required" },
  { id: "with_any_purchase", label: "With any purchase" },
  { id: "with_minimum_spend", label: "With a minimum spend" },
] as const

export const CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS: readonly {
  id: CampaignCatalogOfferValidityId
  label: string
}[] = [
  { id: "7_days_after_issue", label: "7 days after issue" },
  { id: "14_days_after_issue", label: "14 days after issue" },
  { id: "30_days_after_issue", label: "30 days after issue" },
  { id: "choose_expiry_date", label: "Choose an expiry date" },
] as const

export type CampaignCatalogOfferDetailsDraft = {
  offerType: CampaignCatalogOfferTypeId | null
  discountPercentage: string
  discountAmount: string
  freeItemText: string
  purchaseRequirement: CampaignCatalogOfferPurchaseRequirementId | null
  minimumSpend: string
  additionalExclusions: string
  replacementItemText: string
  title: string
  /** When true, title was edited by the operator — stop auto-overwrite. */
  titleTouched: boolean
  description: string
  validity: CampaignCatalogOfferValidityId
  expiryDate: string
  staffInstructions: string
}

export function emptyCampaignCatalogOfferDetailsDraft(): CampaignCatalogOfferDetailsDraft {
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
    staffInstructions: OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS,
  }
}

/**
 * Auto title from the selected benefit — Create Offer Figma `4770:101858`.
 * Operator may edit afterward (titleTouched).
 */
export function autoTitleForCatalogOffer(
  offer: Pick<
    CampaignCatalogOfferDetailsDraft,
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
      return `${pct}% off your next visit`.slice(0, CAMPAIGN_OFFER_TITLE_MAX)
    }
    return "Percentage discount"
  }
  if (offer.offerType === "fixed_discount") {
    const amount = offer.discountAmount.trim()
    if (amount !== "" && parsePositiveNumber(amount) != null) {
      return `£${amount} off your next order`.slice(0, CAMPAIGN_OFFER_TITLE_MAX)
    }
    return "Fixed discount"
  }
  if (offer.offerType === "free_item") {
    const item = offer.freeItemText.trim()
    if (item !== "") {
      return `Enjoy a free ${item}`.slice(0, CAMPAIGN_OFFER_TITLE_MAX)
    }
    return "Free item"
  }
  if (offer.offerType === "replacement_item") {
    const item = offer.replacementItemText.trim()
    if (item !== "") {
      return `Replacement ${item}`.slice(0, CAMPAIGN_OFFER_TITLE_MAX)
    }
    return "Replacement item"
  }
  return ""
}

/**
 * Merge a Create/Edit Offer draft patch and refresh the auto title when allowed.
 */
export function mergeCampaignCatalogOfferDraftPatch(
  draft: CampaignCatalogOfferDetailsDraft,
  patch: Partial<CampaignCatalogOfferDetailsDraft>
): CampaignCatalogOfferDetailsDraft {
  let next: CampaignCatalogOfferDetailsDraft = { ...draft, ...patch }

  if (patch.title !== undefined) {
    next = {
      ...next,
      title: patch.title.slice(0, CAMPAIGN_OFFER_TITLE_MAX),
      titleTouched: true,
    }
    return next
  }

  if (
    patch.offerType !== undefined
    && patch.offerType !== draft.offerType
  ) {
    next = { ...next, titleTouched: false }
  }

  if (!next.titleTouched) {
    next = { ...next, title: autoTitleForCatalogOffer(next) }
  }

  return next
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

export function canConfirmCampaignCatalogOfferDetails(
  offer: CampaignCatalogOfferDetailsDraft
): boolean {
  if (offer.offerType == null) {
    return false
  }
  if (offer.title.trim() === "") {
    return false
  }
  if (offer.title.trim().length > CAMPAIGN_OFFER_TITLE_MAX) {
    return false
  }
  if (offer.description.trim() === "") {
    return false
  }
  if (offer.description.trim().length > CAMPAIGN_OFFER_DESCRIPTION_MAX) {
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

export type CreateCatalogOfferRequestBody = {
  locationId: number
  offerType: CampaignCatalogOfferTypeId
  title: string
  description: string
  validity: CampaignCatalogOfferValidityId
  expiryDate?: string | null
  discountPercentage?: number | null
  discountAmount?: number | null
  freeItemText?: string | null
  purchaseRequirement?: CampaignCatalogOfferPurchaseRequirementId | null
  minimumSpend?: number | null
  additionalExclusions?: string | null
  replacementItemText?: string | null
  staffInstructions?: string | null
}

export function toCreateCatalogOfferRequestBody(input: {
  locationId: number
  draft: CampaignCatalogOfferDetailsDraft
}): CreateCatalogOfferRequestBody | null {
  const { draft, locationId } = input
  if (!canConfirmCampaignCatalogOfferDetails(draft) || draft.offerType == null) {
    return null
  }

  const body: CreateCatalogOfferRequestBody = {
    locationId,
    offerType: draft.offerType,
    title: draft.title.trim(),
    description: draft.description.trim(),
    validity: draft.validity,
    staffInstructions:
      draft.staffInstructions.trim() === ""
        ? null
        : draft.staffInstructions.trim(),
  }

  if (draft.offerType === "percentage_discount") {
    body.discountPercentage = parsePositiveNumber(draft.discountPercentage)
  } else if (draft.offerType === "fixed_discount") {
    body.discountAmount = parsePositiveNumber(draft.discountAmount)
  } else if (draft.offerType === "free_item") {
    body.freeItemText = draft.freeItemText.trim()
    body.purchaseRequirement = draft.purchaseRequirement
    if (draft.purchaseRequirement === "with_minimum_spend") {
      body.minimumSpend = parsePositiveNumber(draft.minimumSpend)
    }
    body.additionalExclusions =
      draft.additionalExclusions.trim() === ""
        ? null
        : draft.additionalExclusions.trim()
  } else {
    body.replacementItemText = draft.replacementItemText.trim()
  }

  if (draft.validity === "choose_expiry_date") {
    body.expiryDate = draft.expiryDate.trim()
  }

  return body
}

const CATALOG_OFFER_TYPE_IDS = new Set<string>([
  "percentage_discount",
  "fixed_discount",
  "free_item",
  "replacement_item",
])

const CATALOG_OFFER_VALIDITY_IDS = new Set<string>([
  "7_days_after_issue",
  "14_days_after_issue",
  "30_days_after_issue",
  "choose_expiry_date",
])

const CATALOG_OFFER_PURCHASE_REQUIREMENT_IDS = new Set<string>([
  "no_purchase_required",
  "with_any_purchase",
  "with_minimum_spend",
])

function asCatalogOfferTypeId(
  value: string
): CampaignCatalogOfferTypeId | null {
  return CATALOG_OFFER_TYPE_IDS.has(value)
    ? (value as CampaignCatalogOfferTypeId)
    : null
}

function asCatalogOfferValidityId(
  value: string
): CampaignCatalogOfferValidityId {
  return CATALOG_OFFER_VALIDITY_IDS.has(value)
    ? (value as CampaignCatalogOfferValidityId)
    : "30_days_after_issue"
}

function asCatalogOfferPurchaseRequirementId(
  value: string | null
): CampaignCatalogOfferPurchaseRequirementId | null {
  if (value == null) {
    return null
  }
  return CATALOG_OFFER_PURCHASE_REQUIREMENT_IDS.has(value)
    ? (value as CampaignCatalogOfferPurchaseRequirementId)
    : null
}

/** Map a persisted catalog definition into the create/edit drawer draft shape. */
export function catalogOfferDetailToDraft(
  offer: CatalogOfferDetail
): CampaignCatalogOfferDetailsDraft {
  return {
    offerType: asCatalogOfferTypeId(offer.offerType),
    discountPercentage:
      offer.discountPercentage == null ? "" : String(offer.discountPercentage),
    discountAmount:
      offer.discountAmount == null ? "" : String(offer.discountAmount),
    freeItemText: offer.freeItemText ?? "",
    purchaseRequirement: asCatalogOfferPurchaseRequirementId(
      offer.purchaseRequirement
    ),
    minimumSpend:
      offer.minimumSpend == null ? "" : String(offer.minimumSpend),
    additionalExclusions: offer.additionalExclusions ?? "",
    replacementItemText: offer.replacementItemText ?? "",
    title: offer.title,
    titleTouched: true,
    description: offer.description,
    validity: asCatalogOfferValidityId(offer.validity),
    expiryDate: offer.expiryDate ?? "",
    staffInstructions:
      offer.staffInstructions?.trim()
      || OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS,
  }
}

function normalizeDraftText(value: string): string {
  return value.trim()
}

/** True when benefit values or validity/expiry differ from the edit baseline. */
export function isDirtyBenefitOrValidity(
  baseline: CampaignCatalogOfferDetailsDraft,
  current: CampaignCatalogOfferDetailsDraft
): boolean {
  if (baseline.validity !== current.validity) {
    return true
  }
  if (
    normalizeDraftText(baseline.expiryDate)
    !== normalizeDraftText(current.expiryDate)
  ) {
    return true
  }

  const offerType = current.offerType ?? baseline.offerType
  if (offerType === "percentage_discount") {
    return (
      normalizeDraftText(baseline.discountPercentage)
      !== normalizeDraftText(current.discountPercentage)
    )
  }
  if (offerType === "fixed_discount") {
    return (
      normalizeDraftText(baseline.discountAmount)
      !== normalizeDraftText(current.discountAmount)
    )
  }
  if (offerType === "free_item") {
    return (
      normalizeDraftText(baseline.freeItemText)
      !== normalizeDraftText(current.freeItemText)
    )
  }
  if (offerType === "replacement_item") {
    return (
      normalizeDraftText(baseline.replacementItemText)
      !== normalizeDraftText(current.replacementItemText)
    )
  }

  return false
}

/** Soft confirm when the offer already has issues and benefit/validity changed. */
export function shouldConfirmEditOfferSave(input: {
  issueCount: number
  dirtyBenefitOrValidity: boolean
}): boolean {
  return input.issueCount >= 1 && input.dirtyBenefitOrValidity
}
