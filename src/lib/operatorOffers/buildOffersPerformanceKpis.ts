export type OffersPerformanceFacts = {
  /** Snapshot count of stored Active catalog offers — ignores date window. */
  activeOffers: number
  /** Account-wide plan cap when entitlements are available. */
  activeOffersCap?: number
  offersIssued: number
  claims: number
  redemptions: number
}

export type OperatorOffersKpiId =
  | "active-offers"
  | "offers-issued"
  | "claims"
  | "redemptions"
  | "claim-to-redemption-rate"

export type OperatorOffersKpi = {
  id: OperatorOffersKpiId
  label: string
  primaryText: string
  helperText: string
}

/** Redemptions ÷ Claims; 0 Claims → em dash (not 0%). */
export function formatClaimToRedemptionRate(
  claims: number,
  redemptions: number
): string {
  if (claims === 0) {
    return "—"
  }
  return `${Math.round((redemptions / claims) * 100)}%`
}

/** Build Offers Performance KPI cells from snapshot + window counts. */
export function buildOffersPerformanceKpis(
  facts: OffersPerformanceFacts
): OperatorOffersKpi[] {
  const activeOffersPrimary =
    facts.activeOffersCap != null && facts.activeOffersCap > 0
      ? `${facts.activeOffers} of ${facts.activeOffersCap}`
      : String(facts.activeOffers)

  return [
    {
      id: "active-offers",
      label: "Active offers",
      primaryText: activeOffersPrimary,
      helperText:
        facts.activeOffersCap != null && facts.activeOffersCap > 0
          ? "Account-wide active offers compared with your plan limit."
          : "Offers currently available for valid issuance or redemption.",
    },
    {
      id: "offers-issued",
      label: "Offers issued",
      primaryText: String(facts.offersIssued),
      helperText: "Guest-specific passes issued during the selected period.",
    },
    {
      id: "claims",
      label: "Claims",
      primaryText: String(facts.claims),
      helperText:
        "Issued offers activated or opened by guests during this period.",
    },
    {
      id: "redemptions",
      label: "Redemptions",
      primaryText: String(facts.redemptions),
      helperText:
        "Successful staff-confirmed redemptions during this period.",
    },
    {
      id: "claim-to-redemption-rate",
      label: "Claim-to-redemption rate",
      primaryText: formatClaimToRedemptionRate(
        facts.claims,
        facts.redemptions
      ),
      helperText: "Share of claims in this period that staff redeemed.",
    },
  ]
}
