import {
  CAMPAIGN_OFFER_TITLE_MAX,
  type CampaignCatalogOfferDetailsDraft,
} from "@/lib/operatorOffers/offerCatalogPresentation"

export const SHOP_OFFER_CARD_SETUP_COPY = {
  title: "Set up your card offer",
  subtitle:
    "Choose the offer guests will receive and we'll add it to your printed cards.",
  offerTypeLabel: "Offer type",
  offerTypePlaceholder: "Select",
  titleLabel: "Offer title",
  titlePlaceholder: "Get 20% off your next visit",
  descriptionLabel: "Offer description",
  descriptionPlaceholder: "Describe what the guest receives…",
  validityLabel: "Offer validity",
  staffInstructionsLabel: "Staff instructions",
  discountPercentageLabel: "Discount percentage",
  discountAmountLabel: "Discount amount (£)",
  freeItemLabel: "Free item",
  purchaseRequirementLabel: "Purchase requirement",
  minimumSpendLabel: "Minimum spend (£)",
  additionalExclusionsLabel: "Additional exclusions",
  replacementItemLabel: "Replacement item",
  expiryDateLabel: "Expiry date",
  importantTitle: "Important:",
  importantBody:
    "This offer will be printed on your cards. Once the order enters production, changes to the offer will not update the printed cards.",
  continueLabel: "Continue",
  cancelLabel: "Cancel",
  previewLabel: "Card preview",
  defaultDescription:
    "Show this offer to a member of staff on your next visit to redeem it.",
  createError: "Could not create your card offer.",
  attachError: "Could not attach your card offer.",
  createThenAttachError:
    "Offer was created but could not be attached. Try again from Offers.",
  guardToast: "Set up your card offer before ordering Guest Cards.",
  loadError: "Could not check your card offer.",
  previewError: "Could not load the card preview.",
} as const

/** Guest Cards SKU — the card-offer rule applies to lines of this SKU. */
export const SHOP_GUEST_CARD_SKU_ID = "qr-guest-cards" as const

/** Print pack viewBox — keep in sync with print-template-pack-v1 card-dev.svg. */
export const SHOP_OFFER_CARD_PREVIEW_ASPECT = "240.94 / 155.91" as const

type ShopOfferCardTitleSource = Pick<
  CampaignCatalogOfferDetailsDraft,
  | "offerType"
  | "discountPercentage"
  | "discountAmount"
  | "freeItemText"
  | "replacementItemText"
>

function isPositiveNumberText(raw: string): boolean {
  const trimmed = raw.trim()
  if (trimmed === "") {
    return false
  }
  const value = Number(trimmed)
  return Number.isFinite(value) && value > 0
}

/**
 * Shop Offer Card title from the selected benefit. Empty when the benefit
 * value is missing, so the title field never shows a stale headline.
 */
export function shopOfferCardAutoTitle(
  offer: ShopOfferCardTitleSource
): string {
  let title = ""
  if (offer.offerType === "percentage_discount") {
    const pct = offer.discountPercentage.trim()
    if (isPositiveNumberText(pct)) {
      title = `Get ${pct}% off your next visit`
    }
  } else if (offer.offerType === "fixed_discount") {
    const amount = offer.discountAmount.trim()
    if (isPositiveNumberText(amount)) {
      title = `Get £${amount} off your next order`
    }
  } else if (offer.offerType === "free_item") {
    const item = offer.freeItemText.trim()
    if (item !== "") {
      title = `Enjoy a free ${item}`
    }
  } else if (offer.offerType === "replacement_item") {
    const item = offer.replacementItemText.trim()
    if (item !== "") {
      title = `Replacement ${item}`
    }
  }
  return title.slice(0, CAMPAIGN_OFFER_TITLE_MAX)
}

/**
 * Merge a Shop Offer Card draft patch. The title follows the benefit value
 * until the operator types in the title field; after that it stays.
 * Unlike the shared Campaign merge, changing the offer type does not reset
 * a title the operator already edited.
 */
export function mergeShopOfferCardDraftPatch(
  draft: CampaignCatalogOfferDetailsDraft,
  patch: Partial<CampaignCatalogOfferDetailsDraft>
): CampaignCatalogOfferDetailsDraft {
  const next: CampaignCatalogOfferDetailsDraft = { ...draft, ...patch }
  if (patch.title !== undefined) {
    return {
      ...next,
      title: patch.title.slice(0, CAMPAIGN_OFFER_TITLE_MAX),
      titleTouched: true,
    }
  }
  if (next.titleTouched) {
    return next
  }
  return { ...next, title: shopOfferCardAutoTitle(next) }
}
