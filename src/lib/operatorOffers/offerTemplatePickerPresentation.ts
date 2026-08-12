/** Figma Choose an offer template — node 4783:30859. */

export const OFFER_TEMPLATE_PICKER_COPY = {
  title: "Choose an offer template",
  subtitle:
    "Start with a practical offer structure, then review the benefit, terms, validity and redemption rules.",
  searchPlaceholder: "Search offer templates",
  useTemplate: "Use template",
  preview: "Preview",
  loadError: "Could not load offer templates. Please try again.",
  emptyError: "Offer template catalogue is empty.",
  retry: "Retry",
  searchMiss: "No offer templates match your search.",
  benefitMeta: "Suggested benefit",
  validityMeta: "Suggested validity",
  sourceMeta: "Suggested source",
  titlePlaceholderMeta: "Offer title placeholder",
  startingDescriptionMeta: "Starting description",
  closeAriaLabel: "Close offer template picker",
} as const

/**
 * Reuse Campaign picker layout/token classes — chrome matches; copy is Offers-owned.
 * Import from Campaign presentation so spacing stays aligned with Operator tokens.
 */
export {
  CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS as OFFER_TEMPLATE_PICKER_OVERLAY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS as OFFER_TEMPLATE_PICKER_CONTENT_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS as OFFER_TEMPLATE_PICKER_BODY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_TITLE_CLASS as OFFER_TEMPLATE_PICKER_TITLE_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS as OFFER_TEMPLATE_PICKER_SUBTITLE_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SEARCH_WRAP_CLASS as OFFER_TEMPLATE_PICKER_SEARCH_WRAP_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS as OFFER_TEMPLATE_PICKER_SEARCH_FIELD_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_GRID_CLASS as OFFER_TEMPLATE_PICKER_GRID_CLASS,
  CAMPAIGN_TEMPLATE_CARD_CLASS as OFFER_TEMPLATE_CARD_CLASS,
  CAMPAIGN_TEMPLATE_CARD_TITLE_CLASS as OFFER_TEMPLATE_CARD_TITLE_CLASS,
  CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS as OFFER_TEMPLATE_CARD_DESCRIPTION_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS as OFFER_TEMPLATE_CARD_META_ROW_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_LABEL_CLASS as OFFER_TEMPLATE_CARD_META_LABEL_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_VALUE_CLASS as OFFER_TEMPLATE_CARD_META_VALUE_CLASS,
  CAMPAIGN_TEMPLATE_CARD_ACTIONS_CLASS as OFFER_TEMPLATE_CARD_ACTIONS_CLASS,
} from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
