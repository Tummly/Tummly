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
  defaultDescription: "Offer printed on physical Offer Cards.",
  createError: "Could not create your card offer.",
  attachError: "Could not attach your card offer.",
  createThenAttachError:
    "Offer was created but could not be attached. Try again from Offers.",
  guardToast: "Set up your card offer before ordering Offer Cards.",
  loadError: "Could not check your card offer.",
  previewError: "Could not load the card preview.",
} as const

export const SHOP_OFFER_CARD_SKU_ID = "offer-card" as const

/** Print pack viewBox — keep in sync with print-template-pack-v1 card-dev.svg. */
export const SHOP_OFFER_CARD_PREVIEW_ASPECT = "240.94 / 155.91" as const
