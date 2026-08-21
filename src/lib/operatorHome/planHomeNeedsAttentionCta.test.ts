import { describe, expect, it } from "vitest"

import { buildHomeNeedsAttention } from "./buildHomeNeedsAttention"
import { planHomeNeedsAttentionCta } from "./planHomeNeedsAttentionCta"

const locationId = 7

describe("planHomeNeedsAttentionCta", () => {
  it("opens Feedback inbox Needs attention for this location", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs: Date.parse("2026-08-21T12:00:00.000Z"),
      feedback: {
        count: 3,
        newestSubmittedAt: "2026-08-21T11:48:00.000Z",
      },
    })
    const row = projection.visibleRows[0]
    expect(row?.sourceKind).toBe("feedback")
    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "review-feedback",
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/feedback?location=7",
      feedbackInbox: { tab: "needs-attention" },
    })
  })

  it("opens Campaign Detail for Preview and Campaigns for Retry remaining", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs: Date.parse("2026-08-21T12:00:00.000Z"),
      campaigns: [
        {
          id: 41,
          name: "Weekend SMS blast",
          status: "partially-sent",
          updatedAt: "2026-08-21T11:00:00.000Z",
          rowVersion: "rv-41",
        },
      ],
    })
    const row = projection.visibleRows[0]
    expect(row?.sourceKind).toBe("campaign")

    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "preview-campaign",
        mode: "multi",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/multi-dashboard/campaigns/41?location=7",
    })
    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "retry-remaining",
        mode: "multi",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/multi-dashboard/campaigns?location=7",
    })
  })

  it("plans Duplicate as Draft as a create-then-open-draft action", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs: Date.parse("2026-08-21T12:00:00.000Z"),
      campaigns: [
        {
          id: 41,
          name: "Weekend SMS blast",
          status: "failed",
          updatedAt: "2026-08-21T11:00:00.000Z",
          rowVersion: "rv-41",
        },
      ],
    })
    const row = projection.visibleRows[0]
    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "duplicate-as-draft",
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "duplicate-as-draft",
      campaignId: 41,
      campaignsPath: "/single-dashboard/campaigns?location=7",
    })
  })

  it("opens Offer Details and the redemptions tab for Offer CTAs", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs: Date.parse("2026-08-21T12:00:00.000Z"),
      offers: [
        {
          id: 88,
          title: "10% off next order",
          lifetimeClaims: 23,
          lifetimeRedeemed: 9,
          attentionKind: "warning",
          openVoid: null,
          expiry: {
            windowEnteredAt: "2026-08-21T11:00:00.000Z",
            daysUntilExpiry: 2,
          },
        },
      ],
    })
    const row = projection.visibleRows[0]
    expect(row?.sourceKind).toBe("offer")

    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "manage-offer",
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/offers/88?location=7",
    })
    expect(
      planHomeNeedsAttentionCta({
        item: row!,
        ctaKind: "view-redemptions",
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/offers/88?location=7&tab=redemptions",
    })
  })
})
