import { describe, expect, it } from "vitest"

import {
  buildExpiringOffersWarningFact,
  buildOpenVoidWarningFacts,
  type OffersNeedsAttentionExpiringOffer,
  type OffersNeedsAttentionOpenVoidOffer,
} from "@/lib/operatorOffers/buildOffersNeedsAttentionFacts"

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
  it("returns empty when no open voids", () => {
    expect(
      buildOpenVoidWarningFacts({
        offers: [],
        locationName: "Camden",
      })
    ).toEqual([])
  })

  it("builds single-offer void warning with Details CTA", () => {
    const offers: OffersNeedsAttentionOpenVoidOffer[] = [
      { offerId: 42, offerTitle: "Lunch deal", pendingCount: 2 },
    ]

    expect(
      buildOpenVoidWarningFacts({
        offers,
        locationName: "Camden",
        relativeTimeLabel: "Just now",
      })
    ).toEqual([
      {
        id: "warning-void-42",
        kind: "warning",
        title: "Open void request",
        body: "“Lunch deal” has 2 pending void requests.",
        metaParts: ["Just now", "Camden"],
        ctaKind: "review-void-offer",
        ctaLabel: "Review void request",
        offerId: 42,
      },
    ])
  })

  it("builds aggregate void warning when multiple offers", () => {
    const offers: OffersNeedsAttentionOpenVoidOffer[] = [
      { offerId: 1, offerTitle: "A", pendingCount: 1 },
      { offerId: 2, offerTitle: "B", pendingCount: 3 },
    ]

    expect(
      buildOpenVoidWarningFacts({
        offers,
        locationName: "Camden",
      })
    ).toEqual([
      {
        id: "warning-void-aggregate",
        kind: "warning",
        title: "Open void requests",
        body: "2 offers have pending void requests.",
        metaParts: ["Camden"],
        ctaKind: "review-void-aggregate",
        ctaLabel: "Review void requests",
      },
    ])
  })
})
