import { describe, expect, it } from "vitest"

import { buildHomeNeedsAttention } from "./buildHomeNeedsAttention"

describe("buildHomeNeedsAttention", () => {
  it("returns empty when no source facts qualify", () => {
    expect(
      buildHomeNeedsAttention({
        locationName: "Manchester",
        feedback: { count: 0, newestSubmittedAt: null },
        campaigns: [],
        offers: [],
      })
    ).toEqual({
      allRows: [],
      visibleRows: [],
      showViewAll: false,
      isEmpty: true,
    })
  })

  it("builds one Feedback aggregate whose title uses the count", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      feedback: {
        count: 3,
        newestSubmittedAt: "2026-08-21T11:48:00.000Z",
      },
      campaigns: [],
      offers: [],
    })

    expect(result.isEmpty).toBe(false)
    expect(result.showViewAll).toBe(false)
    expect(result.visibleRows).toHaveLength(1)
    expect(result.allRows).toEqual(result.visibleRows)
    expect(result.visibleRows[0]).toMatchObject({
      sourceKind: "feedback",
      id: "feedback",
      title: "3 feedback items need attention",
      body: "Negative feedback is not Resolved.",
      metaKind: "warning",
      metaLine: "Warning · 12 minutes ago · Manchester",
      ctas: [{ kind: "review-feedback", label: "Review feedback" }],
    })
  })

  it("builds one named Campaign row for a Failed Campaign", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      campaigns: [
        {
          id: 41,
          name: "Weekend SMS blast",
          status: "failed",
          updatedAt: "2026-08-21T11:00:00.000Z",
          rowVersion: "rv-41",
        },
      ],
      offers: [],
    })

    expect(result.isEmpty).toBe(false)
    expect(result.visibleRows).toHaveLength(1)
    expect(result.visibleRows[0]).toMatchObject({
      sourceKind: "campaign",
      id: "campaign-41",
      campaignId: 41,
      title: "Weekend SMS blast",
      body: "This campaign failed.",
      metaKind: "warning",
      metaLine: "Warning · 1 hour ago · Manchester",
      rowVersion: "rv-41",
      ctas: [
        { kind: "preview-campaign", label: "Preview" },
        { kind: "duplicate-as-draft", label: "Duplicate as Draft" },
      ],
    })
  })

  it("builds one named Offer row per Offer and sorts newest meta first", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      offers: [
        {
          id: 10,
          title: "Lunch deal",
          lifetimeClaims: 4,
          lifetimeRedeemed: 1,
          attentionKind: "warning",
          openVoid: null,
          expiry: {
            windowEnteredAt: "2026-08-21T09:00:00.000Z",
            daysUntilExpiry: 5,
          },
        },
        {
          id: 11,
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

    expect(result.visibleRows).toHaveLength(2)
    expect(result.visibleRows.map((row) => row.id)).toEqual([
      "offer-11",
      "offer-10",
    ])
    expect(result.visibleRows[0]).toMatchObject({
      sourceKind: "offer",
      offerId: 11,
      title: "Offer expires in 2 days",
      body: "“10% off next order” has 23 claims and 9 redemptions before expiry.",
      metaKind: "warning",
      metaLine: "Warning · 1 hour ago · Manchester",
      ctas: [
        { kind: "manage-offer", label: "Manage offer" },
        { kind: "view-redemptions", label: "View redemptions" },
      ],
    })
    expect(result.visibleRows[1]).toMatchObject({
      offerId: 10,
      title: "Offer expires in 5 days",
      metaLine: "Warning · 3 hours ago · Manchester",
    })
  })

  it("emits one Offer row and lets Void request win expiry on the same Offer", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      offers: [
        {
          id: 22,
          title: "Lunch deal",
          lifetimeClaims: 4,
          lifetimeRedeemed: 1,
          attentionKind: "warning",
          openVoid: {
            requestedAt: "2026-08-21T11:48:00.000Z",
            pendingCount: 1,
          },
          expiry: {
            windowEnteredAt: "2026-08-21T09:00:00.000Z",
            daysUntilExpiry: 2,
          },
        },
      ],
    })

    expect(result.visibleRows).toHaveLength(1)
    expect(result.visibleRows[0]).toMatchObject({
      sourceKind: "offer",
      id: "offer-22",
      offerId: 22,
      title: "Open void request",
      body: "“Lunch deal” has 1 pending void request.",
      metaKind: "warning",
      metaLine: "Warning · 12 minutes ago · Manchester",
      ctas: [
        { kind: "manage-offer", label: "Manage offer" },
        { kind: "view-redemptions", label: "View redemptions" },
      ],
    })
  })

  it("caps visibleRows at 5 and sets showViewAll when more qualify", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const offers = Array.from({ length: 6 }, (_, index) => ({
      id: index + 1,
      title: `Offer ${index + 1}`,
      lifetimeClaims: 2,
      lifetimeRedeemed: 1,
      attentionKind: "warning" as const,
      openVoid: null,
      expiry: {
        windowEnteredAt: `2026-08-21T11:0${index}:00.000Z`,
        daysUntilExpiry: 2,
      },
    }))

    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      offers,
    })

    expect(result.allRows).toHaveLength(6)
    expect(result.visibleRows).toHaveLength(5)
    expect(result.showViewAll).toBe(true)
    expect(result.isEmpty).toBe(false)
    expect(result.visibleRows.map((row) => row.id)).toEqual([
      "offer-6",
      "offer-5",
      "offer-4",
      "offer-3",
      "offer-2",
    ])
    expect(result.allRows[5]?.id).toBe("offer-1")
  })

  it("keeps kind order Feedback then Campaigns then Offers even when Offers are newer", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      feedback: {
        count: 1,
        newestSubmittedAt: "2026-08-20T12:00:00.000Z",
      },
      campaigns: [
        {
          id: 7,
          name: "Retry lunch",
          status: "partially-sent",
          updatedAt: "2026-08-21T10:00:00.000Z",
          rowVersion: "rv-7",
        },
      ],
      offers: [
        {
          id: 90,
          title: "10% off next order",
          lifetimeClaims: 23,
          lifetimeRedeemed: 9,
          attentionKind: "ai",
          openVoid: null,
          expiry: {
            windowEnteredAt: "2026-08-21T11:50:00.000Z",
            daysUntilExpiry: 2,
          },
        },
      ],
    })

    expect(result.visibleRows.map((row) => row.sourceKind)).toEqual([
      "feedback",
      "campaign",
      "offer",
    ])
    expect(result.visibleRows[0]).toMatchObject({
      title: "1 feedback item needs attention",
    })
    expect(result.visibleRows[1]).toMatchObject({
      campaignId: 7,
      ctas: [
        { kind: "preview-campaign", label: "Preview" },
        { kind: "retry-remaining", label: "Retry remaining" },
      ],
    })
    expect(result.visibleRows[2]).toMatchObject({
      offerId: 90,
      metaKind: "ai",
      metaLine: "AI · 10 minutes ago · Manchester",
    })
  })

  it("emits one named Offer row for an Offers AI attention member with no Void or expiry", () => {
    const nowMs = Date.parse("2026-08-21T12:00:00.000Z")
    const result = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs,
      offers: [
        {
          id: 33,
          title: "Lunch deal",
          lifetimeClaims: 4,
          lifetimeRedeemed: 1,
          attentionKind: "ai",
          openVoid: null,
          expiry: null,
        },
      ],
    })

    expect(result.visibleRows).toHaveLength(1)
    expect(result.visibleRows[0]).toMatchObject({
      sourceKind: "offer",
      id: "offer-33",
      offerId: 33,
      title: "Lunch deal",
      body: "“Lunch deal” has 4 claims and 1 redemption.",
      metaKind: "ai",
      metaLine: "AI · Manchester",
      ctas: [
        { kind: "manage-offer", label: "Manage offer" },
        { kind: "view-redemptions", label: "View redemptions" },
      ],
    })
  })
})
