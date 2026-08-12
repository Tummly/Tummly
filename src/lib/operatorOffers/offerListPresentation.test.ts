import { describe, expect, it } from "vitest"

import {
  buildOfferRowActions,
  formatOfferAttachSubline,
  formatOfferControlsLabel,
  formatOfferRedemptionRate,
  formatOfferValidityLabel,
  mapCatalogOfferListItemToTableRow,
} from "@/lib/operatorOffers/offerListPresentation"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"

function listItem(
  overrides: Partial<CatalogOffersListItem> & { id: number; title: string }
): CatalogOffersListItem {
  return {
    locationId: 42,
    status: "active",
    offerType: "percentage_discount",
    validity: "14_days_after_issue",
    expiryDate: null,
    attachKinds: [],
    lifetimeClaims: 0,
    lifetimeRedeemed: 0,
    createdAt: "2026-08-01T10:00:00.000Z",
    updatedAt: "2026-08-01T10:00:00.000Z",
    ...overrides,
  }
}

describe("buildOfferRowActions", () => {
  it("gates Draft actions", () => {
    expect(buildOfferRowActions("draft").map((a) => a.id)).toEqual([
      "view",
      "edit",
      "duplicate",
      "archive",
    ])
  })

  it("gates Active actions", () => {
    expect(buildOfferRowActions("active").map((a) => a.id)).toEqual([
      "view",
      "edit",
      "pause",
      "duplicate",
      "archive",
    ])
  })

  it("gates Paused actions with Resume", () => {
    expect(buildOfferRowActions("paused").map((a) => a.id)).toEqual([
      "view",
      "edit",
      "resume",
      "duplicate",
      "archive",
    ])
  })

  it("gates Expired without Edit", () => {
    expect(buildOfferRowActions("expired").map((a) => a.id)).toEqual([
      "view",
      "duplicate",
      "archive",
    ])
  })

  it("gates Archived to View + Duplicate only", () => {
    expect(buildOfferRowActions("archived").map((a) => a.id)).toEqual([
      "view",
      "duplicate",
    ])
  })
})

describe("offer list presentation labels", () => {
  it("maps attach kinds to human subline labels", () => {
    expect(
      formatOfferAttachSubline([
        "campaign",
        "recovery",
        "guest-form-thank-you",
        "manual",
      ])
    ).toBe("Campaign, Recovery, Guest form thank-you, Manual")
    expect(formatOfferAttachSubline([])).toBeNull()
  })

  it("shows — for redemption rate when claims are 0", () => {
    expect(formatOfferRedemptionRate(0, 0)).toBe("—")
  })

  it("computes lifetime redemption rate", () => {
    expect(formatOfferRedemptionRate(10, 4)).toBe("40%")
  })

  it("formats validity and controls without inventing use-rule", () => {
    expect(formatOfferValidityLabel("14_days_after_issue", null)).toBe(
      "14 days after issue"
    )
    expect(formatOfferControlsLabel("14_days_after_issue", null)).toBe(
      "Unique code · 14-day expiry"
    )
    expect(formatOfferControlsLabel("choose_expiry_date", "2026-09-01")).toBe(
      "Unique code · Ends 2026-09-01"
    )
  })

  it("maps a list item into a table row", () => {
    const row = mapCatalogOfferListItemToTableRow(
      listItem({
        id: 7,
        title: "10% off next visit",
        attachKinds: ["campaign"],
        lifetimeClaims: 20,
        lifetimeRedeemed: 5,
      })
    )
    expect(row).toMatchObject({
      id: 7,
      title: "10% off next visit",
      attachSubline: "Campaign",
      statusLabel: "Active",
      claimsLabel: "20",
      redeemedLabel: "5",
      redemptionRateLabel: "25%",
      controlsLabel: "Unique code · 14-day expiry",
    })
  })
})
