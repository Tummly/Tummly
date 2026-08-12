/**
 * Location-wide Offers redemption log query — maps ticket 42 list API rows.
 */

import { format } from "date-fns"

import type { OperatorOffersRedemptionLogRow } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  OfferDetailsRedemptionListItemApi,
  OfferDetailsRedemptionsListResponse,
} from "@/types/operatorCampaigns"

function formatRedemptionLogDateTimeLabel(
  iso: string | null | undefined
): string {
  if (iso == null || iso.trim().length === 0) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  const parsed = new Date(iso)
  if (Number.isNaN(parsed.getTime())) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  return format(parsed, "d MMM yyyy, HH:mm")
}

export function mapLocationRedemptionLogRow(
  item: OfferDetailsRedemptionListItemApi
): OperatorOffersRedemptionLogRow {
  return {
    id: item.id,
    dateTimeText: formatRedemptionLogDateTimeLabel(item.dateTimeUtc),
    guestName: item.guestName,
    passReferenceText: item.passReferenceText,
    locationName: item.locationName,
    staffMemberText:
      item.staffMemberText?.trim()
      || OFFER_DETAILS_COPY.metricUnavailable,
    outcomeText: item.outcomeLabel,
    reasonText: item.reasonLabel ?? OFFER_DETAILS_COPY.metricUnavailable,
    offerVersionText: item.offerVersionLabel,
    offerTitle: item.offerTitle,
  }
}

export type LocationRedemptionLogFetch = (
  locationId: number
) => Promise<OfferDetailsRedemptionsListResponse>

export async function loadLocationRedemptionLogRows(
  locationId: number,
  options: { fetchRedemptions: LocationRedemptionLogFetch }
): Promise<readonly OperatorOffersRedemptionLogRow[]> {
  const response = await options.fetchRedemptions(locationId)
  if (!response.success) {
    throw new Error("Location redemption log list failed.")
  }
  return response.items.map(mapLocationRedemptionLogRow)
}
