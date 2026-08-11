import type { OffersNeedsAttentionFact } from "@/lib/operatorOffers/buildOffersNeedsAttentionOverview"

export type OffersNeedsAttentionExpiringOffer = {
  id: number
  title: string
  lifetimeClaims: number
  lifetimeRedeemed: number
}

export type OffersNeedsAttentionOpenVoidOffer = {
  offerId: number
  offerTitle: string
  pendingCount: number
}

function quoteTitle(title: string): string {
  return `“${title}”`
}

function claimRedeemCopy(claims: number, redeemed: number): string {
  const claimsLabel = claims === 1 ? "1 claim" : `${claims} claims`
  const redeemedLabel =
    redeemed === 1 ? "1 redemption" : `${redeemed} redemptions`
  return `${claimsLabel} and ${redeemedLabel}`
}

/** One overview warning for catalog offers expiring within 7 venue days. */
export function buildExpiringOffersWarningFact(input: {
  offers: readonly OffersNeedsAttentionExpiringOffer[]
  locationName: string
  relativeTimeLabel?: string
}): OffersNeedsAttentionFact | null {
  if (input.offers.length === 0) {
    return null
  }

  const count = input.offers.length
  const lead = input.offers[0]!
  const title =
    count === 1
      ? "1 offer expires this week"
      : `${count} offers expire this week`
  const body = `${quoteTitle(lead.title)} has ${claimRedeemCopy(lead.lifetimeClaims, lead.lifetimeRedeemed)} before expiry.`
  const metaParts = [
    ...(input.relativeTimeLabel != null && input.relativeTimeLabel !== ""
      ? [input.relativeTimeLabel]
      : []),
    input.locationName,
  ]

  return {
    id: "warning-expiring",
    kind: "warning",
    title,
    body,
    metaParts,
    ctaKind: "review-expiring",
    ctaLabel: "Review expiring offers",
  }
}

/**
 * Open Void request warnings: one offer → Details Void tab CTA;
 * multiple offers → Needs attention list (void scope) CTA.
 */
export function buildOpenVoidWarningFacts(input: {
  offers: readonly OffersNeedsAttentionOpenVoidOffer[]
  locationName: string
  relativeTimeLabel?: string
}): OffersNeedsAttentionFact[] {
  if (input.offers.length === 0) {
    return []
  }

  const metaParts = [
    ...(input.relativeTimeLabel != null && input.relativeTimeLabel !== ""
      ? [input.relativeTimeLabel]
      : []),
    input.locationName,
  ]

  if (input.offers.length === 1) {
    const only = input.offers[0]!
    const pendingLabel =
      only.pendingCount === 1
        ? "1 pending void request"
        : `${only.pendingCount} pending void requests`
    return [
      {
        id: `warning-void-${only.offerId}`,
        kind: "warning",
        title: "Open void request",
        body: `${quoteTitle(only.offerTitle)} has ${pendingLabel}.`,
        metaParts,
        ctaKind: "review-void-offer",
        ctaLabel: "Review void request",
        offerId: only.offerId,
      },
    ]
  }

  return [
    {
      id: "warning-void-aggregate",
      kind: "warning",
      title: "Open void requests",
      body: `${input.offers.length} offers have pending void requests.`,
      metaParts,
      ctaKind: "review-void-aggregate",
      ctaLabel: "Review void requests",
    },
  ]
}
