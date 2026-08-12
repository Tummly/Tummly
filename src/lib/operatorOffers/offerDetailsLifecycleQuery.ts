/**
 * Offer Details lifecycle list queries — Claims + Redemptions tabs (ticket 40).
 */

import { format } from "date-fns"

import type {
  OfferDetailsClaimRow,
  OfferDetailsRedemptionRow,
} from "@/lib/operatorOffers/createOfferDetailsPageModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  OfferDetailsClaimListItemApi,
  OfferDetailsClaimsListResponse,
  OfferDetailsRedemptionListItemApi,
  OfferDetailsRedemptionsListResponse,
} from "@/types/operatorCampaigns"

function formatOfferDetailsDateLabel(iso: string | null | undefined): string {
  if (iso == null || iso.trim().length === 0) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  const parsed = new Date(iso)
  if (Number.isNaN(parsed.getTime())) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  return format(parsed, "d MMM yyyy")
}

function formatOfferDetailsDateTimeLabel(iso: string | null | undefined): string {
  if (iso == null || iso.trim().length === 0) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  const parsed = new Date(iso)
  if (Number.isNaN(parsed.getTime())) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  return format(parsed, "d MMM yyyy, HH:mm")
}

export function mapOfferDetailsClaimListItem(
  item: OfferDetailsClaimListItemApi
): OfferDetailsClaimRow {
  return {
    id: item.id,
    guestName: item.guestName,
    guestId: item.guestId == null ? null : String(item.guestId),
    claimCode: item.claimCode,
    claimedText: formatOfferDetailsDateLabel(
      item.claimedAtUtc ?? item.issuedAtUtc
    ),
    sourceText: item.sourceLabel,
    locationName: item.locationName,
    expiryText: formatOfferDetailsDateLabel(item.expiryAtUtc),
    statusText: item.statusLabel,
    actions: [],
  }
}

export function mapOfferDetailsRedemptionListItem(
  item: OfferDetailsRedemptionListItemApi
): OfferDetailsRedemptionRow {
  return {
    id: item.id,
    dateTimeText: formatOfferDetailsDateTimeLabel(item.dateTimeUtc),
    guestName: item.guestName,
    guestId: item.guestId == null ? null : String(item.guestId),
    passReferenceText: item.passReferenceText,
    locationName: item.locationName,
    staffMemberText:
      item.staffMemberText?.trim()
      || OFFER_DETAILS_COPY.metricUnavailable,
    outcomeText: item.outcomeLabel,
    reasonText: item.reasonLabel ?? OFFER_DETAILS_COPY.metricUnavailable,
    offerVersionText: item.offerVersionLabel,
    passId: item.passId,
    passCodeMasked: item.passCodeMasked,
    expiresText: formatOfferDetailsDateLabel(item.expiresAtUtc),
    linkedCampaignText: item.linkedCampaignText ?? undefined,
    offerTitle: item.offerTitle,
    actions: [],
  }
}

export type OfferDetailsClaimsFetch = (
  offerId: number
) => Promise<OfferDetailsClaimsListResponse>

export type OfferDetailsRedemptionsFetch = (
  offerId: number
) => Promise<OfferDetailsRedemptionsListResponse>

export async function loadOfferDetailsClaims(
  offerId: number,
  options: { fetchClaims: OfferDetailsClaimsFetch }
): Promise<readonly OfferDetailsClaimRow[]> {
  const response = await options.fetchClaims(offerId)
  if (!response.success) {
    throw new Error("Offer claims list failed.")
  }
  return response.items.map(mapOfferDetailsClaimListItem)
}

export async function loadOfferDetailsRedemptions(
  offerId: number,
  options: { fetchRedemptions: OfferDetailsRedemptionsFetch }
): Promise<readonly OfferDetailsRedemptionRow[]> {
  const response = await options.fetchRedemptions(offerId)
  if (!response.success) {
    throw new Error("Offer redemptions list failed.")
  }
  return response.items.map(mapOfferDetailsRedemptionListItem)
}
