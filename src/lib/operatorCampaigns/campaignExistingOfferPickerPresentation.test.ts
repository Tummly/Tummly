import { describe, expect, it } from "vitest"

import {
  CAMPAIGN_EXISTING_OFFER_PICKER_COPY,
  filterExistingOfferPickerItems,
  formatExistingOfferPickerValidUntil,
  mapCatalogOfferToExistingPickerCard,
  resolveExistingOfferPickerTypeIconId,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"

function sampleItem(
  overrides: Partial<CatalogOffersListItem> = {}
): CatalogOffersListItem {
  return {
    id: 1,
    locationId: 42,
    title: "10% off next visit",
    status: "active",
    offerType: "percentage_discount",
    validity: "30_days_after_issue",
    expiryDate: null,
    attachKinds: [],
    createdAt: "2026-08-09T00:00:00Z",
    updatedAt: "2026-08-09T00:00:00Z",
    ...overrides,
  }
}

describe("campaignExistingOfferPickerPresentation", () => {
  it("uses Existing-offer search copy and enables View details", () => {
    expect(CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchPlaceholder).toBe(
      "Search offers"
    )
    expect(CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchPlaceholder).not.toMatch(
      /campaigns|audiences/i
    )
    expect(CAMPAIGN_EXISTING_OFFER_PICKER_COPY.viewDetailsEnabled).toBe(true)
    expect(CAMPAIGN_EXISTING_OFFER_PICKER_COPY.viewDetailsLabel).toBe(
      "View details"
    )
  })

  it("maps Valid until + fixed Use rule for picker cards", () => {
    const card = mapCatalogOfferToExistingPickerCard(
      sampleItem({
        validity: "choose_expiry_date",
        expiryDate: "2026-12-31",
      })
    )
    expect(card.validUntilLabel).toBe("Valid until: 2026-12-31")
    expect(card.useRuleLabel).toBe(
      CAMPAIGN_EXISTING_OFFER_PICKER_COPY.useRule
    )
    expect(card.metaLine).toBe(
      "Valid until: 2026-12-31 · Use rule: Single use per guest"
    )
    expect(formatExistingOfferPickerValidUntil("14_days_after_issue", null)).toBe(
      "Valid until: 14 days after issue"
    )
  })

  it("resolves known catalog type icons and filters by title / type", () => {
    expect(resolveExistingOfferPickerTypeIconId("replacement_item")).toBe(
      "replacement_item"
    )
    expect(resolveExistingOfferPickerTypeIconId("mystery")).toBe("unknown")

    const items = [
      sampleItem({ id: 1, title: "Brunch deal", offerType: "free_item" }),
      sampleItem({
        id: 2,
        title: "Dinner percent",
        offerType: "percentage_discount",
      }),
    ]
    expect(filterExistingOfferPickerItems(items, "brunch").map((i) => i.id)).toEqual(
      [1]
    )
    expect(
      filterExistingOfferPickerItems(items, "percentage").map((i) => i.id)
    ).toEqual([2])
  })
})
