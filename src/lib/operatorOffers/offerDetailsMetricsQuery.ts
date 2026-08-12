/**
 * Offer Details Overview metrics query — date window + API map for KPIs (ticket 35).
 */

import { parseOfferDetailsLocalDateKey } from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  OfferDetailsDateRange,
  OfferDetailsOverviewMetrics,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import type { OfferMetricsResponse } from "@/types/operatorCampaigns"

/**
 * Resolve Offer Details Overview bounds in the operator's local timezone.
 * Callers send the returned instants as UTC ISO strings to GET /offers/{id}/metrics ([from, to)).
 *
 * - Last 7 days: start of local day 6 days before today → now
 * - Last 30 days: start of local day 29 days before today → now
 * - Last 90 days: start of local day 89 days before today → now
 * - Custom: start of start date → start of day after end date
 */
export function resolveOfferDetailsWindow(
  range: OfferDetailsDateRange,
  now: Date = new Date()
): { from: Date; to: Date } {
  if (range.kind === "custom") {
    const from = parseOfferDetailsLocalDateKey(range.startDate)
    const end = parseOfferDetailsLocalDateKey(range.endDate)
    const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
    return { from, to }
  }

  if (range.presetId === "last7") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 6),
      to: now,
    }
  }

  if (range.presetId === "last30") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 29),
      to: now,
    }
  }

  return {
    from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 89),
    to: now,
  }
}

export function mapOfferMetricsResponse(
  response: OfferMetricsResponse
): OfferDetailsOverviewMetrics {
  return {
    claims: response.claims,
    redemptions: response.redemptions,
    expiredUnused: response.expiredUnused,
    failedAttempts: response.failedAttempts,
  }
}

export type OfferDetailsMetricsFetch = (
  offerId: number,
  params: { from: string; to: string }
) => Promise<OfferMetricsResponse>

/**
 * Load Overview KPI counts for one Offer and date window via GET /offers/{id}/metrics.
 */
export async function loadOfferDetailsOverviewMetrics(
  offerId: number,
  range: OfferDetailsDateRange,
  options: {
    fetchMetrics: OfferDetailsMetricsFetch
    now?: Date
  }
): Promise<OfferDetailsOverviewMetrics> {
  const window = resolveOfferDetailsWindow(range, options.now ?? new Date())
  const response = await options.fetchMetrics(offerId, {
    from: window.from.toISOString(),
    to: window.to.toISOString(),
  })
  if (!response.success) {
    throw new Error("Offer metrics get failed.")
  }
  return mapOfferMetricsResponse(response)
}
