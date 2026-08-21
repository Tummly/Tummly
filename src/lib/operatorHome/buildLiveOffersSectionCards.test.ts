import { describe, expect, it } from "vitest"

import type {
  CampaignsListItem,
  CatalogOffersListItem,
} from "@/types/operatorCampaigns"

import {
  buildLiveOffersSectionCards,
  formatLiveMetricCount,
  formatLiveMetricOrDash,
  LIVE_OFFERS_METRIC_DASH,
} from "./buildLiveOffersSectionCards"

function campaign(
  overrides: Partial<CampaignsListItem> & Pick<CampaignsListItem, "id" | "name">
): CampaignsListItem {
  return {
    status: "scheduled",
    goalId: null,
    locationId: 1,
    locationName: "Test",
    channel: "email",
    audienceKey: null,
    offerStance: null,
    updatedAt: "2026-08-20T12:00:00.000Z",
    rowVersion: "AAAA",
    sendDate: null,
    delivery: null,
    engagement: null,
    redemptions: null,
    ...overrides,
  }
}

function offer(
  overrides: Partial<CatalogOffersListItem> &
    Pick<CatalogOffersListItem, "id" | "title">
): CatalogOffersListItem {
  return {
    locationId: 1,
    status: "active",
    offerType: "percentage-discount",
    validity: "custom-date",
    expiryDate: "2026-07-31",
    attachKinds: ["campaign"],
    createdAt: "2026-08-01T12:00:00.000Z",
    updatedAt: "2026-08-20T12:00:00.000Z",
    ...overrides,
  }
}

describe("formatLiveMetricOrDash", () => {
  it("uses em dash for null or blank", () => {
    expect(formatLiveMetricOrDash(null)).toBe(LIVE_OFFERS_METRIC_DASH)
    expect(formatLiveMetricOrDash("")).toBe(LIVE_OFFERS_METRIC_DASH)
    expect(formatLiveMetricOrDash("  ")).toBe(LIVE_OFFERS_METRIC_DASH)
  })

  it("keeps non-empty metric strings", () => {
    expect(formatLiveMetricOrDash("12%")).toBe("12%")
  })
})

describe("formatLiveMetricCount", () => {
  it("uses em dash for null", () => {
    expect(formatLiveMetricCount(null)).toBe(LIVE_OFFERS_METRIC_DASH)
    expect(formatLiveMetricCount(undefined)).toBe(LIVE_OFFERS_METRIC_DASH)
  })

  it("formats known counts", () => {
    expect(formatLiveMetricCount(0)).toBe("0")
    expect(formatLiveMetricCount(12)).toBe("12")
  })
})

describe("buildLiveOffersSectionCards", () => {
  it("returns empty cards when both lists are empty", () => {
    expect(
      buildLiveOffersSectionCards({
        campaigns: [],
        offers: [],
      })
    ).toEqual([])
  })

  it("picks newest scheduled or sending campaign and newest active offer", () => {
    const cards = buildLiveOffersSectionCards({
      campaigns: [
        campaign({
          id: 1,
          name: "Older",
          status: "scheduled",
          updatedAt: "2026-08-10T12:00:00.000Z",
        }),
        campaign({
          id: 2,
          name: "Newer sending",
          status: "sending",
          updatedAt: "2026-08-21T12:00:00.000Z",
          delivery: "80%",
          redemptions: "3",
        }),
        campaign({
          id: 3,
          name: "Paused ignored",
          status: "paused",
          updatedAt: "2026-08-22T12:00:00.000Z",
        }),
      ],
      offers: [
        offer({
          id: 10,
          title: "Older offer",
          updatedAt: "2026-08-10T12:00:00.000Z",
          lifetimeClaims: 1,
          lifetimeRedeemed: 0,
        }),
        offer({
          id: 11,
          title: "10% off your next visit",
          updatedAt: "2026-08-21T12:00:00.000Z",
          lifetimeClaims: 5,
          lifetimeRedeemed: 2,
          expiryDate: "2026-07-31",
        }),
      ],
    })

    expect(cards).toHaveLength(2)
    expect(cards[0]).toMatchObject({
      kind: "campaign",
      id: 2,
      title: "Newer sending",
      statusLabel: "Sending",
      rowVersion: "AAAA",
    })
    expect(cards[0]?.kind === "campaign" && cards[0].metricParts).toEqual([
      `Sent to ${LIVE_OFFERS_METRIC_DASH} guests`,
      "80% delivered",
      "3 offer claims",
    ])
    expect(cards[1]).toMatchObject({
      kind: "offer",
      id: 11,
      title: "10% off your next visit",
      statusLabel: "Active",
    })
    expect(cards[1]?.kind === "offer" && cards[1].metricParts).toEqual([
      "5 claims",
      "2 redemptions",
      "Expires 31 Jul 2026",
    ])
  })

  it("fills with two campaigns when no offers qualify", () => {
    const cards = buildLiveOffersSectionCards({
      campaigns: [
        campaign({
          id: 1,
          name: "A",
          status: "scheduled",
          updatedAt: "2026-08-21T12:00:00.000Z",
        }),
        campaign({
          id: 2,
          name: "B",
          status: "sending",
          updatedAt: "2026-08-20T12:00:00.000Z",
        }),
      ],
      offers: [],
    })

    expect(cards.map((card) => card.id)).toEqual([1, 2])
    expect(cards.every((card) => card.kind === "campaign")).toBe(true)
  })

  it("fills with two offers when no campaigns qualify", () => {
    const cards = buildLiveOffersSectionCards({
      campaigns: [
        campaign({
          id: 9,
          name: "Draft",
          status: "draft",
          updatedAt: "2026-08-22T12:00:00.000Z",
        }),
      ],
      offers: [
        offer({
          id: 1,
          title: "First",
          updatedAt: "2026-08-21T12:00:00.000Z",
        }),
        offer({
          id: 2,
          title: "Second",
          updatedAt: "2026-08-20T12:00:00.000Z",
        }),
      ],
    })

    expect(cards.map((card) => card.id)).toEqual([1, 2])
    expect(cards.every((card) => card.kind === "offer")).toBe(true)
  })

  it("prefers one of each when both exist even if a second campaign is newer", () => {
    const cards = buildLiveOffersSectionCards({
      campaigns: [
        campaign({
          id: 1,
          name: "Newest campaign",
          status: "sending",
          updatedAt: "2026-08-22T12:00:00.000Z",
        }),
        campaign({
          id: 2,
          name: "Older campaign",
          status: "scheduled",
          updatedAt: "2026-08-21T12:00:00.000Z",
        }),
      ],
      offers: [
        offer({
          id: 5,
          title: "Only offer",
          updatedAt: "2026-08-01T12:00:00.000Z",
        }),
      ],
    })

    expect(cards).toHaveLength(2)
    expect(cards[0]?.kind).toBe("campaign")
    expect(cards[0]?.id).toBe(1)
    expect(cards[1]?.kind).toBe("offer")
    expect(cards[1]?.id).toBe(5)
  })
})
