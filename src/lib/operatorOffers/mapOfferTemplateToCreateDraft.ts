import {
  emptyCampaignCatalogOfferDetailsDraft,
  OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS,
  type CampaignCatalogOfferDetailsDraft,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import type { OfferTemplateSeedItem } from "@/lib/operatorOffers/offerTemplateSeed"

function substituteRestaurantName(
  text: string,
  restaurantName: string
): string {
  return text.replaceAll("{{restaurant_name}}", restaurantName)
}

/**
 * Soft-fill Create Offer draft from an Offer template (ticket 08 / 19).
 * Does not POST — catalogue create waits for Save.
 */
export function mapOfferTemplateToCreateDraft(
  template: OfferTemplateSeedItem,
  restaurantName: string
): CampaignCatalogOfferDetailsDraft {
  const isCustom = template.id === "custom-offer"
  const title =
    isCustom || template.offerTitlePlaceholder == null
      ? ""
      : substituteRestaurantName(template.offerTitlePlaceholder, restaurantName)

  return {
    ...emptyCampaignCatalogOfferDetailsDraft(),
    offerType: template.softOfferType,
    title,
    description: substituteRestaurantName(
      template.startingDescription,
      restaurantName
    ),
    validity: template.softValidity,
    expiryDate: "",
    staffInstructions: OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS,
  }
}
