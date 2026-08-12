import { describe, expect, it } from "vitest"

import {
  buildOffersNeedsAttentionOverview,
  type OffersNeedsAttentionFact,
} from "@/lib/operatorOffers/buildOffersNeedsAttentionOverview"

function warningFact(
  overrides: Partial<OffersNeedsAttentionFact> & { id: string }
): OffersNeedsAttentionFact {
  return {
    kind: "warning",
    title: "Offers expire this week",
    body: "Review expiring catalog offers.",
    metaParts: ["Just now", "Camden"],
    ctaKind: "review-expiring",
    ctaLabel: "Review expiring offers",
    ...overrides,
  }
}

function aiFact(
  overrides: Partial<OffersNeedsAttentionFact> & { id: string }
): OffersNeedsAttentionFact {
  return {
    kind: "ai",
    title: "AI tip",
    body: "Portfolio suggestion.",
    metaParts: ["Just now", "Camden"],
    ctaKind: "ai-primary",
    ctaLabel: "Review",
    ...overrides,
  }
}

describe("buildOffersNeedsAttentionOverview", () => {
  it("returns empty when no facts", () => {
    expect(
      buildOffersNeedsAttentionOverview({ warnings: [], ai: [] })
    ).toEqual({
      rows: [],
      showViewAll: false,
      isEmpty: true,
    })
  })

  it("sorts warnings before AI and builds Warning/AI meta lines", () => {
    const result = buildOffersNeedsAttentionOverview({
      warnings: [warningFact({ id: "w1" })],
      ai: [aiFact({ id: "a1" })],
    })

    expect(result.isEmpty).toBe(false)
    expect(result.showViewAll).toBe(false)
    expect(result.rows).toHaveLength(2)
    expect(result.rows[0]).toMatchObject({
      id: "w1",
      kind: "warning",
      metaLine: "Warning · Just now · Camden",
      ctaKind: "review-expiring",
      ctaLabel: "Review expiring offers",
    })
    expect(result.rows[1]).toMatchObject({
      id: "a1",
      kind: "ai",
      metaLine: "AI · Just now · Camden",
      ctaKind: "ai-primary",
    })
  })

  it("caps overview at 5 rows and sets showViewAll when more qualify", () => {
    const warnings = Array.from({ length: 4 }, (_, i) =>
      warningFact({ id: `w${i}`, title: `Warning ${i}` })
    )
    const ai = Array.from({ length: 3 }, (_, i) =>
      aiFact({ id: `a${i}`, title: `AI ${i}` })
    )

    const result = buildOffersNeedsAttentionOverview({ warnings, ai })

    expect(result.rows).toHaveLength(5)
    expect(result.showViewAll).toBe(true)
    expect(result.rows.map((row) => row.id)).toEqual([
      "w0",
      "w1",
      "w2",
      "w3",
      "a0",
    ])
  })

  it("preserves single-offer void CTA with offerId", () => {
    const result = buildOffersNeedsAttentionOverview({
      warnings: [
        warningFact({
          id: "void-1",
          title: "Open void request",
          body: "“Lunch deal” has 1 pending void request.",
          ctaKind: "review-void-offer",
          ctaLabel: "Review void request",
          offerId: 99,
        }),
      ],
      ai: [],
    })

    expect(result.rows[0]).toMatchObject({
      ctaKind: "review-void-offer",
      offerId: 99,
      ctaLabel: "Review void request",
    })
  })

  it("preserves aggregate void CTA without offerId", () => {
    const result = buildOffersNeedsAttentionOverview({
      warnings: [
        warningFact({
          id: "void-agg",
          title: "Open void requests",
          body: "3 offers have pending void requests.",
          ctaKind: "review-void-aggregate",
          ctaLabel: "Review void requests",
        }),
      ],
      ai: [],
    })

    expect(result.rows[0]).toMatchObject({
      ctaKind: "review-void-aggregate",
      offerId: undefined,
    })
  })
})
