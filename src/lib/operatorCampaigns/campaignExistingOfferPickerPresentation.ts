/**
 * Campaign wizard Existing-offer inline picker — Figma 4744:63758 / tickets 11 + 30.
 */

import { formatOfferValidityLabel } from "@/lib/operatorOffers/offerListPresentation"
import type { CampaignCatalogOfferTypeId } from "@/lib/operatorOffers/offerCatalogPresentation"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"

export const CAMPAIGN_EXISTING_OFFER_PICKER_COPY = {
  searchPlaceholder: "Search offers",
  useRule: "Use rule: Single use per guest",
  selectLabel: "Select",
  viewDetailsLabel: "View details",
  /** Offer Details route is live — open in a new tab from the picker (ticket 36). */
  viewDetailsEnabled: true,
  emptyHelper: "No active offers yet. Create one to attach it to this campaign.",
  createNewOfferLabel: "Create a new offer",
  loadError: "Could not load offers. Try again.",
  retryLabel: "Retry",
  searchMissHelper: "No offers match your search.",
} as const

export type CampaignExistingOfferPickerCard = {
  id: number
  title: string
  offerType: string
  /** Lucide-aligned type key for UI icons; unknown types fall back to Tag. */
  offerTypeIconId: CampaignCatalogOfferTypeId | "unknown"
  validUntilLabel: string
  useRuleLabel: string
  metaLine: string
}

const CATALOG_TYPE_IDS = new Set<string>([
  "percentage_discount",
  "fixed_discount",
  "free_item",
  "replacement_item",
])

export function formatExistingOfferPickerValidUntil(
  validity: string,
  expiryDate: string | null
): string {
  return `Valid until: ${formatOfferValidityLabel(validity, expiryDate)}`
}

export function resolveExistingOfferPickerTypeIconId(
  offerType: string
): CampaignCatalogOfferTypeId | "unknown" {
  if (CATALOG_TYPE_IDS.has(offerType)) {
    return offerType as CampaignCatalogOfferTypeId
  }
  return "unknown"
}

export function mapCatalogOfferToExistingPickerCard(
  item: CatalogOffersListItem
): CampaignExistingOfferPickerCard {
  const validUntilLabel = formatExistingOfferPickerValidUntil(
    item.validity,
    item.expiryDate
  )
  const useRuleLabel = CAMPAIGN_EXISTING_OFFER_PICKER_COPY.useRule
  return {
    id: item.id,
    title: item.title,
    offerType: item.offerType,
    offerTypeIconId: resolveExistingOfferPickerTypeIconId(item.offerType),
    validUntilLabel,
    useRuleLabel,
    metaLine: `${validUntilLabel} · ${useRuleLabel}`,
  }
}

export function filterExistingOfferPickerItems(
  items: readonly CatalogOffersListItem[],
  searchQuery: string
): CatalogOffersListItem[] {
  const q = searchQuery.trim().toLowerCase()
  if (q.length === 0) {
    return [...items]
  }
  return items.filter((item) => {
    if (item.title.toLowerCase().includes(q)) {
      return true
    }
    return item.offerType.toLowerCase().includes(q)
  })
}
