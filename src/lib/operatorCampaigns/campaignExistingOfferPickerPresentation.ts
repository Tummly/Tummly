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

/**
 * Figma `5338:64710` — Existing offer picker panel on Offer selection.
 * Fill `#f5f5f5` / `--op-color-gray-60` light, `#171717` / `--op-color-gray-1000`
 * dark. Idle border `--op-divider` (`#e5e5e5` light / `#262626` dark).
 */
export const EXISTING_OFFER_PICKER_PANEL_CLASS =
  "flex w-full flex-col gap-[18px] rounded-[4px] border border-op-divider bg-op-color-gray-60 p-5 dark:bg-[var(--op-color-gray-1000)]"

/**
 * Figma Offer card — `#fff` / `--op-shell-chrome` light, `#141414` dark;
 * 24px padding and 36px gap between header and actions.
 * Idle border matches other wizard cards (`--op-divider`).
 */
export const EXISTING_OFFER_PICKER_CARD_CLASS =
  "flex w-full flex-col gap-9 overflow-clip rounded-[4px] border border-op-divider bg-op-shell-chrome p-6"

export const EXISTING_OFFER_PICKER_ICON_WELL_CLASS =
  "flex size-[47px] shrink-0 items-center justify-center rounded-[2.6px] bg-op-background-secondary"

export const EXISTING_OFFER_PICKER_CARD_TITLE_CLASS =
  "m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-op-text-primary"

export const EXISTING_OFFER_PICKER_CARD_META_CLASS =
  "m-0 flex flex-wrap items-start gap-2 text-xs font-normal leading-normal text-[var(--op-color-gray-550)]"

export const EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

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
