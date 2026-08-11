/** Pure Offers Needs attention overview builder (ticket 33 / ticket 07). */

export type OffersNeedsAttentionRowKind = "warning" | "ai"

export type OffersNeedsAttentionCtaKind =
  | "review-expiring"
  | "review-void-offer"
  | "review-void-aggregate"
  | "ai-primary"

export type OffersNeedsAttentionFact = {
  id: string
  kind: OffersNeedsAttentionRowKind
  title: string
  body: string
  /** Segments after the Warning/AI prefix (e.g. relative time, venue). */
  metaParts: readonly string[]
  ctaKind: OffersNeedsAttentionCtaKind
  ctaLabel: string
  offerId?: number
}

export type OffersNeedsAttentionOverviewRow = {
  id: string
  kind: OffersNeedsAttentionRowKind
  title: string
  body: string
  metaLine: string
  ctaKind: OffersNeedsAttentionCtaKind
  ctaLabel: string
  offerId?: number
}

export type OffersNeedsAttentionOverview = {
  rows: OffersNeedsAttentionOverviewRow[]
  showViewAll: boolean
  isEmpty: boolean
}

export const OFFERS_NEEDS_ATTENTION_OVERVIEW_MAX_ROWS = 5

function metaPrefix(kind: OffersNeedsAttentionRowKind): string {
  return kind === "warning" ? "Warning" : "AI"
}

function toRow(fact: OffersNeedsAttentionFact): OffersNeedsAttentionOverviewRow {
  const parts = [metaPrefix(fact.kind), ...fact.metaParts.filter((p) => p !== "")]
  return {
    id: fact.id,
    kind: fact.kind,
    title: fact.title,
    body: fact.body,
    metaLine: parts.join(" · "),
    ctaKind: fact.ctaKind,
    ctaLabel: fact.ctaLabel,
    offerId: fact.offerId,
  }
}

/**
 * Build main-page Needs attention overview from rule + AI facts.
 * Warnings sort before AI; cap at 5; overflow → showViewAll.
 */
export function buildOffersNeedsAttentionOverview(input: {
  warnings: readonly OffersNeedsAttentionFact[]
  ai?: readonly OffersNeedsAttentionFact[]
}): OffersNeedsAttentionOverview {
  const ordered = [...input.warnings, ...(input.ai ?? [])]
  const capped = ordered.slice(0, OFFERS_NEEDS_ATTENTION_OVERVIEW_MAX_ROWS)
  const rows = capped.map(toRow)
  return {
    rows,
    showViewAll: ordered.length > OFFERS_NEEDS_ATTENTION_OVERVIEW_MAX_ROWS,
    isEmpty: rows.length === 0,
  }
}
