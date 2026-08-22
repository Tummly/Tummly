import { describe, expect, it } from "vitest"

import {
  buildExpiringOffersWarningFact,
  buildOpenVoidWarningFacts,
  selectExpiringOffersForOverview,
  type OffersNeedsAttentionExpiringOffer,
  type OffersNeedsAttentionOpenVoidOffer,
} from "@/lib/operatorOffers/buildOffersNeedsAttentionFacts"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"

const OVERVIEW_NOW_MS = Date.parse("2026-08-22T12:00:00.000Z")

function catalogItem(
  overrides: Partial<CatalogOffersListItem> & { id: number; title: string }
): CatalogOffersListItem {
  return {
    locationId: 7,
    status: "active",
    offerType: "percentage_discount",
    validity: "14_days_after_issue",
    expiryDate: null,
    attachKinds: ["campaign"],
    lifetimeClaims: 0,
    lifetimeRedeemed: 0,
    createdAt: "2026-08-01T10:00:00.000Z",
    updatedAt: "2026-08-01T10:00:00.000Z",
    ...overrides,
  }
}

describe("buildExpiringOffersWarningFact", () => {
  it("returns null when no expiring offers", () => {
    expect(
      buildExpiringOffersWarningFact({
        offers: [],
        locationName: "Camden",
      })
    ).toBeNull()
  })

  it("builds aggregate expiring warning with review-expiring CTA", () => {
    const offers: OffersNeedsAttentionExpiringOffer[] = [
      {
        id: 1,
        title: "10% off next order",
        lifetimeClaims: 23,
        lifetimeRedeemed: 9,
      },
      {
        id: 2,
        title: "Free dessert",
        lifetimeClaims: 4,
        lifetimeRedeemed: 1,
      },
      {
        id: 3,
        title: "Lunch deal",
        lifetimeClaims: 0,
        lifetimeRedeemed: 0,
      },
    ]

    expect(
      buildExpiringOffersWarningFact({
        offers,
        locationName: "Manchester",
        relativeTimeLabel: "1 hour ago",
      })
    ).toEqual({
      id: "warning-expiring",
      kind: "warning",
      title: "3 offers expire this week",
      body: "“10% off next order” has 23 claims and 9 redemptions before expiry.",
      metaParts: ["1 hour ago", "Manchester"],
      ctaKind: "review-expiring",
      ctaLabel: "Review expiring offers",
    })
  })

  it("uses singular copy for one expiring offer", () => {
    const fact = buildExpiringOffersWarningFact({
      offers: [
        {
          id: 7,
          title: "Free coffee",
          lifetimeClaims: 2,
          lifetimeRedeemed: 1,
        },
      ],
      locationName: "Camden",
    })

    expect(fact?.title).toBe("1 offer expires this week")
    expect(fact?.body).toBe(
      "“Free coffee” has 2 claims and 1 redemption before expiry."
    )
  })
})

describe("buildOpenVoidWarningFacts", () => {
  const VOID_NOW_MS = Date.parse("2026-08-22T12:00:00.000Z")

  it("returns empty when no open voids", () => {
    expect(
      buildOpenVoidWarningFacts({
        offers: [],
        locationName: "Camden",
        nowMs: VOID_NOW_MS,
      })
    ).toEqual([])
  })

  it("builds single-offer void warning with relative clock from newest pending request", () => {
    const offers: OffersNeedsAttentionOpenVoidOffer[] = [
      {
        offerId: 42,
        offerTitle: "Lunch deal",
        pendingCount: 2,
        newestPendingRequestedAtUtc: "2026-08-22T11:00:00.000Z",
      },
    ]

    expect(
      buildOpenVoidWarningFacts({
        offers,
        locationName: "Camden",
        nowMs: VOID_NOW_MS,
      })
    ).toEqual([
      {
        id: "warning-void-42",
        kind: "warning",
        title: "Open void request",
        body: "“Lunch deal” has 2 pending void requests.",
        metaParts: ["1 hour ago", "Camden"],
        ctaKind: "review-void-offer",
        ctaLabel: "Review void request",
        offerId: 42,
      },
    ])
  })

  it("uses the newest pending requested-at across offers for aggregate void clock", () => {
    const offers: OffersNeedsAttentionOpenVoidOffer[] = [
      {
        offerId: 1,
        offerTitle: "A",
        pendingCount: 1,
        newestPendingRequestedAtUtc: "2026-08-21T10:00:00.000Z",
      },
      {
        offerId: 2,
        offerTitle: "B",
        pendingCount: 3,
        newestPendingRequestedAtUtc: "2026-08-22T11:30:00.000Z",
      },
    ]

    expect(
      buildOpenVoidWarningFacts({
        offers,
        locationName: "Camden",
        nowMs: VOID_NOW_MS,
      })
    ).toEqual([
      {
        id: "warning-void-aggregate",
        kind: "warning",
        title: "Open void requests",
        body: "2 offers have pending void requests.",
        metaParts: ["30 minutes ago", "Camden"],
        ctaKind: "review-void-aggregate",
        ctaLabel: "Review void requests",
      },
    ])
  })

  it("omits a relative clock when no pending requested-at is available", () => {
    const offers: OffersNeedsAttentionOpenVoidOffer[] = [
      {
        offerId: 9,
        offerTitle: "Lunch deal",
        pendingCount: 1,
        newestPendingRequestedAtUtc: null,
      },
    ]

    expect(
      buildOpenVoidWarningFacts({
        offers,
        locationName: "Camden",
        nowMs: VOID_NOW_MS,
      })
    ).toEqual([
      {
        id: "warning-void-9",
        kind: "warning",
        title: "Open void request",
        body: "“Lunch deal” has 1 pending void request.",
        metaParts: ["Camden"],
        ctaKind: "review-void-offer",
        ctaLabel: "Review void request",
        offerId: 9,
      },
    ])
  })
})

describe("selectExpiringOffersForOverview", () => {
  it("omits Void-only and N-days-after-issue Offers from the expiry set", () => {
    const selected = selectExpiringOffersForOverview({
      items: [
        catalogItem({
          id: 11,
          title: "Void only lunch",
          validity: "14_days_after_issue",
          expiryDate: null,
        }),
        catalogItem({
          id: 12,
          title: "Seven days after issue",
          validity: "7_days_after_issue",
          expiryDate: null,
        }),
      ],
      nowMs: OVERVIEW_NOW_MS,
      utcOffsetMinutes: 0,
    })

    expect(selected).toEqual({
      offers: [],
      leadWindowEnteredAt: null,
    })
  })

  it("keeps dual-rule Offers that still have a catalog end date in the 7-day window", () => {
    const selected = selectExpiringOffersForOverview({
      items: [
        catalogItem({
          id: 21,
          title: "Void only",
          validity: "30_days_after_issue",
          expiryDate: null,
        }),
        catalogItem({
          id: 22,
          title: "Dual rule dessert",
          validity: "choose_expiry_date",
          expiryDate: "2026-08-25",
          lifetimeClaims: 4,
          lifetimeRedeemed: 1,
        }),
        catalogItem({
          id: 23,
          title: "Fixed date eight days out",
          validity: "choose_expiry_date",
          expiryDate: "2026-08-30",
        }),
      ],
      nowMs: OVERVIEW_NOW_MS,
      utcOffsetMinutes: 0,
    })

    expect(selected.offers).toEqual([
      {
        id: 22,
        title: "Dual rule dessert",
        lifetimeClaims: 4,
        lifetimeRedeemed: 1,
      },
    ])
    expect(selected.leadWindowEnteredAt).toBe("2026-08-18T00:00:00.000Z")
  })

  it("picks the soonest venue-local end date, then the lower catalog Offer id", () => {
    const selected = selectExpiringOffersForOverview({
      items: [
        catalogItem({
          id: 40,
          title: "Later end",
          validity: "choose_expiry_date",
          expiryDate: "2026-08-28",
          lifetimeClaims: 9,
          lifetimeRedeemed: 2,
        }),
        catalogItem({
          id: 31,
          title: "Same-day higher id",
          validity: "choose_expiry_date",
          expiryDate: "2026-08-24",
          lifetimeClaims: 1,
          lifetimeRedeemed: 0,
        }),
        catalogItem({
          id: 30,
          title: "Same-day lead",
          validity: "choose_expiry_date",
          expiryDate: "2026-08-24",
          lifetimeClaims: 8,
          lifetimeRedeemed: 3,
        }),
      ],
      nowMs: OVERVIEW_NOW_MS,
      utcOffsetMinutes: 0,
    })

    expect(selected.offers.map((offer) => offer.id)).toEqual([30, 31, 40])
    expect(selected.offers[0]).toEqual({
      id: 30,
      title: "Same-day lead",
      lifetimeClaims: 8,
      lifetimeRedeemed: 3,
    })
    expect(selected.leadWindowEnteredAt).toBe("2026-08-17T00:00:00.000Z")
  })
})

