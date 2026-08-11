import type {
  CampaignCatalogOfferTypeId,
  CampaignCatalogOfferValidityId,
} from "@/lib/operatorOffers/offerCatalogPresentation"

/** Product-global Offer template catalogue v1 (ticket 08 / 19). */
export type OfferTemplateSeedItem = {
  id: string
  title: string
  summary: string
  suggestedBenefit?: string
  softOfferType: CampaignCatalogOfferTypeId | null
  suggestedValidity?: string
  softValidity: CampaignCatalogOfferValidityId
  /** Card display only — never written to Create Offer draft. */
  suggestedSource?: string
  offerTitlePlaceholder?: string
  startingDescription: string
}

export const OFFER_TEMPLATE_SEED: readonly OfferTemplateSeedItem[] = [
  {
    id: "welcome-new-guests",
    title: "Welcome new guests",
    summary:
      "Give newly joined guest-club members a simple reason to return.",
    suggestedBenefit: "Percentage discount or free item",
    softOfferType: "percentage_discount",
    suggestedValidity: "30 days after issue",
    softValidity: "30_days_after_issue",
    suggestedSource: "Guest form signup",
    offerTitlePlaceholder: "Welcome to {{restaurant_name}}",
    startingDescription:
      "Enjoy a welcome offer on your next eligible visit to {{restaurant_name}}.",
  },
  {
    id: "thank-recent-guests",
    title: "Thank recent guests",
    summary: "Thank guests after recent feedback or guest-list activity.",
    suggestedBenefit: "Percentage or fixed discount",
    softOfferType: "percentage_discount",
    suggestedValidity: "30 days after issue",
    softValidity: "30_days_after_issue",
    suggestedSource: "Private feedback or guest signup",
    offerTitlePlaceholder: "A thank-you from {{restaurant_name}}",
    startingDescription:
      "Thank you for sharing your experience with us. Enjoy this offer on your next eligible visit.",
  },
  {
    id: "encourage-quieter-time",
    title: "Encourage a quieter time",
    summary: "Create an offer for a day or time selected by the restaurant.",
    suggestedBenefit: "Percentage discount, fixed discount or free item",
    softOfferType: "percentage_discount",
    suggestedValidity: "Operator selected",
    softValidity: "choose_expiry_date",
    offerTitlePlaceholder: "A little extra for your next {{day_or_time}} visit",
    startingDescription:
      "Use this offer during the selected day or time at {{restaurant_name}}.",
  },
  {
    id: "reconnect-with-guests",
    title: "Reconnect with guests",
    summary:
      "Create a return offer for guests with no recent activity recorded in Tummly.",
    suggestedBenefit: "Percentage or fixed discount",
    softOfferType: "percentage_discount",
    suggestedValidity: "14 days after issue",
    softValidity: "14_days_after_issue",
    suggestedSource: "No recent Tummly activity",
    offerTitlePlaceholder: "We would love to welcome you again",
    startingDescription:
      "Enjoy this offer on your next eligible visit to {{restaurant_name}}.",
  },
  {
    id: "completed-recovery-offer",
    title: "Completed recovery offer",
    summary:
      "Provide a controlled offer after a private feedback case has been reviewed.",
    suggestedBenefit: "Replacement item, free item or fixed discount",
    softOfferType: "replacement_item",
    suggestedValidity: "14 days after issue",
    softValidity: "14_days_after_issue",
    suggestedSource: "Completed feedback recovery",
    offerTitlePlaceholder: "A recovery offer from {{restaurant_name}}",
    startingDescription:
      "Please use this offer during your next eligible visit. Our team will verify your unique offer code.",
  },
  {
    id: "promote-new-item",
    title: "Promote a new item",
    summary: "Introduce an operator-provided menu item or restaurant update.",
    suggestedBenefit: "Free item or percentage discount",
    softOfferType: "free_item",
    suggestedValidity: "Operator selected",
    softValidity: "choose_expiry_date",
    offerTitlePlaceholder: "Try something new at {{restaurant_name}}",
    startingDescription:
      "Discover {{item_name}} at {{restaurant_name}} and use this offer during the available period.",
  },
  {
    id: "custom-offer",
    title: "Custom offer",
    summary:
      "Start without suggested offer content and configure everything yourself.",
    softOfferType: null,
    softValidity: "30_days_after_issue",
    startingDescription:
      "Enjoy this offer on your next eligible visit to {{restaurant_name}}.",
  },
] as const

export function getOfferTemplateById(
  id: string
): OfferTemplateSeedItem | undefined {
  return OFFER_TEMPLATE_SEED.find((item) => item.id === id)
}

export async function loadOfferTemplateSeed(): Promise<OfferTemplateSeedItem[]> {
  return [...OFFER_TEMPLATE_SEED]
}
