/**
 * Offer Details lifecycle list queries — Claims / Redemptions / Campaigns / Void (tickets 40–41).
 */

import { format } from "date-fns"

import type {
  OfferDetailsClaimRow,
  OfferDetailsIssuanceSourceRow,
  OfferDetailsLinkedCampaignRow,
  OfferDetailsRedemptionRow,
  OfferDetailsVoidRequestRow,
} from "@/lib/operatorOffers/createOfferDetailsPageModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  VoidRequestCorrectionId,
  VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"
import type {
  OfferDetailsClaimListItemApi,
  OfferDetailsClaimsListResponse,
  OfferDetailsIssuanceSourceListItemApi,
  OfferDetailsIssuanceSourcesListResponse,
  OfferDetailsLinkedCampaignListItemApi,
  OfferDetailsLinkedCampaignsListResponse,
  OfferDetailsRedemptionListItemApi,
  OfferDetailsRedemptionsListResponse,
  OfferDetailsVoidRequestListItemApi,
  OfferDetailsVoidRequestsListResponse,
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

export function mapOfferDetailsLinkedCampaignListItem(
  item: OfferDetailsLinkedCampaignListItemApi
): OfferDetailsLinkedCampaignRow {
  return {
    id: item.id,
    campaignName: item.campaignName,
    statusText: item.statusLabel,
    locationName: item.locationName,
    channelText: item.channelLabel,
    audienceText: item.audienceLabel,
    offerVersionText: item.offerVersionLabel,
    passesIssuedText: item.passesIssued,
    claimsText: item.claims,
    redemptionsText: item.redemptions,
    sendDateText: item.sendDateLabel,
  }
}

export function mapOfferDetailsIssuanceSourceListItem(
  item: OfferDetailsIssuanceSourceListItemApi
): OfferDetailsIssuanceSourceRow {
  return {
    id: item.id,
    sourceText: item.sourceLabel,
    pathText: item.pathLabel,
    passesIssuedText: item.passesIssued,
    lastIssuedText: item.lastIssuedLabel,
  }
}

export function mapOfferDetailsVoidRequestListItem(
  item: OfferDetailsVoidRequestListItemApi
): OfferDetailsVoidRequestRow {
  return {
    id: item.requestId,
    dateTimeText:
      item.requestedAtText.trim().length > 0
        ? item.requestedAtText
        : formatOfferDetailsDateTimeLabel(item.requestedAtUtc),
    requestedByText: item.requestedByText,
    guestName: item.guestName,
    offerPassText: item.offerPassText,
    reasonText: item.reasonText,
    locationName: item.locationName,
    currentStateText: item.currentStateText,
    requestedCorrectionText: item.correctionText,
    statusText: item.statusLabel,
    passId: item.passId,
    passCodeMasked: item.passCodeMasked,
    expiresText: item.expiresText,
    linkedCampaignText: item.linkedCampaignText,
    offerTitle: item.offerTitle,
    explanation: item.explanation,
    reasonId: item.reasonId as VoidRequestReasonId,
    correctionId: item.correctionId as VoidRequestCorrectionId,
    actions: [],
  }
}

export type OfferDetailsClaimsFetch = (
  offerId: number
) => Promise<OfferDetailsClaimsListResponse>

export type OfferDetailsRedemptionsFetch = (
  offerId: number
) => Promise<OfferDetailsRedemptionsListResponse>

export type OfferDetailsLinkedCampaignsFetch = (
  offerId: number
) => Promise<OfferDetailsLinkedCampaignsListResponse>

export type OfferDetailsIssuanceSourcesFetch = (
  offerId: number
) => Promise<OfferDetailsIssuanceSourcesListResponse>

export type OfferDetailsVoidRequestsFetch = (
  offerId: number
) => Promise<OfferDetailsVoidRequestsListResponse>

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

export async function loadOfferDetailsLinkedCampaigns(
  offerId: number,
  options: { fetchLinkedCampaigns: OfferDetailsLinkedCampaignsFetch }
): Promise<readonly OfferDetailsLinkedCampaignRow[]> {
  const response = await options.fetchLinkedCampaigns(offerId)
  if (!response.success) {
    throw new Error("Offer linked campaigns list failed.")
  }
  return response.items.map(mapOfferDetailsLinkedCampaignListItem)
}

export async function loadOfferDetailsIssuanceSources(
  offerId: number,
  options: { fetchIssuanceSources: OfferDetailsIssuanceSourcesFetch }
): Promise<readonly OfferDetailsIssuanceSourceRow[]> {
  const response = await options.fetchIssuanceSources(offerId)
  if (!response.success) {
    throw new Error("Offer issuance sources list failed.")
  }
  return response.items.map(mapOfferDetailsIssuanceSourceListItem)
}

export async function loadOfferDetailsVoidRequests(
  offerId: number,
  options: { fetchVoidRequests: OfferDetailsVoidRequestsFetch }
): Promise<readonly OfferDetailsVoidRequestRow[]> {
  const response = await options.fetchVoidRequests(offerId)
  if (!response.success) {
    throw new Error("Offer void requests list failed.")
  }
  return response.items.map(mapOfferDetailsVoidRequestListItem)
}
