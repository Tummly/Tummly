import { formatCatalogOfferExpiryLabel } from "@/lib/operatorFeedback/guestPreviewPresentation"
import type {
  CampaignsListItem,
  CatalogOffersListItem,
} from "@/types/operatorCampaigns"

export const LIVE_OFFERS_METRIC_DASH = "—"

const LIVE_CAMPAIGN_STATUSES = new Set(["scheduled", "sending"])

export type OperatorHomeLiveOfferCoupon = {
  title: string
  description: string
  expiryLabel: string
}

export type OperatorHomeLiveCampaignCard = {
  kind: "campaign"
  id: number
  title: string
  status: string
  statusLabel: string
  rowVersion: string
  metricParts: string[]
  channel: string | null
  /** Guest message for left preview chrome — filled after optional draft fetch. */
  messageSubject: string | null
  messageBody: string | null
  /** Attached catalog offer coupon — filled after optional draft + offer join. */
  offerCoupon: OperatorHomeLiveOfferCoupon | null
}

export type OperatorHomeLiveOfferCard = {
  kind: "offer"
  id: number
  title: string
  status: string
  statusLabel: string
  metricParts: string[]
  description: string | null
  validity: string
  expiryDate: string | null
  expiryLabel: string
}

export type OperatorHomeLiveCard =
  | OperatorHomeLiveCampaignCard
  | OperatorHomeLiveOfferCard

export function formatLiveMetricOrDash(
  value: string | null | undefined
): string {
  if (value == null || value.trim().length === 0) {
    return LIVE_OFFERS_METRIC_DASH
  }
  return value.trim()
}

export function formatLiveMetricCount(
  value: number | null | undefined
): string {
  if (value == null) {
    return LIVE_OFFERS_METRIC_DASH
  }
  return String(value)
}

function statusLabelForWireStatus(status: string): string {
  if (status === "partially-sent") {
    return "Partially sent"
  }
  if (status.length === 0) {
    return LIVE_OFFERS_METRIC_DASH
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

function sortByUpdatedAtDesc<T extends { updatedAt: string }>(
  items: readonly T[]
): T[] {
  return [...items].sort(
    (a, b) =>
      new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  )
}

function mapCampaignCard(item: CampaignsListItem): OperatorHomeLiveCampaignCard {
  const delivery = formatLiveMetricOrDash(item.delivery)
  const claims = formatLiveMetricOrDash(item.redemptions)
  return {
    kind: "campaign",
    id: item.id,
    title: item.name,
    status: item.status,
    statusLabel: statusLabelForWireStatus(item.status),
    rowVersion: item.rowVersion,
    channel: item.channel,
    messageSubject: null,
    messageBody: null,
    offerCoupon: null,
    // List wire has no guest-count field — only delivery + redemptions.
    metricParts: [
      delivery === LIVE_OFFERS_METRIC_DASH
        ? `${LIVE_OFFERS_METRIC_DASH} delivered`
        : delivery.toLowerCase().includes("delivered")
          ? delivery
          : `${delivery} delivered`,
      claims === LIVE_OFFERS_METRIC_DASH
        ? `${LIVE_OFFERS_METRIC_DASH} offer claims`
        : claims.toLowerCase().includes("claim")
          ? claims
          : `${claims} offer claims`,
    ],
  }
}

function mapOfferCard(item: CatalogOffersListItem): OperatorHomeLiveOfferCard {
  const expiryLabel = formatCatalogOfferExpiryLabel(
    item.validity,
    item.expiryDate
  )
  return {
    kind: "offer",
    id: item.id,
    title: item.title,
    status: item.status,
    statusLabel: statusLabelForWireStatus(item.status),
    description: item.description ?? null,
    validity: item.validity,
    expiryDate: item.expiryDate,
    expiryLabel,
    metricParts: [
      `${formatLiveMetricCount(item.lifetimeClaims)} claims`,
      `${formatLiveMetricCount(item.lifetimeRedeemed)} redemptions`,
      expiryLabel,
    ],
  }
}

/**
 * Join the attached catalog offer onto a live campaign card after draft fetch.
 */
export function attachLiveCampaignOffer(
  card: OperatorHomeLiveCampaignCard,
  offer: Pick<
    CatalogOffersListItem,
    "title" | "description" | "validity" | "expiryDate"
  > | null
): OperatorHomeLiveCampaignCard {
  if (offer == null) {
    return card
  }
  const expiryLabel = formatCatalogOfferExpiryLabel(
    offer.validity,
    offer.expiryDate
  )
  return {
    ...card,
    offerCoupon: {
      title: offer.title,
      description: offer.description?.trim() ?? "",
      expiryLabel,
    },
    metricParts: [
      ...card.metricParts.filter((part) => !part.startsWith("Expires")),
      expiryLabel,
    ],
  }
}

/**
 * Prefer one campaign + one offer (cap 2). Fill with the other type when only
 * one kind qualifies. Newest by updatedAt within each type.
 */
export function buildLiveOffersSectionCards(input: {
  campaigns: readonly CampaignsListItem[]
  offers: readonly CatalogOffersListItem[]
}): OperatorHomeLiveCard[] {
  const campaigns = sortByUpdatedAtDesc(
    input.campaigns.filter((item) => LIVE_CAMPAIGN_STATUSES.has(item.status))
  )
  const offers = sortByUpdatedAtDesc(input.offers)

  const newestCampaign = campaigns[0] ?? null
  const newestOffer = offers[0] ?? null

  if (newestCampaign != null && newestOffer != null) {
    return [mapCampaignCard(newestCampaign), mapOfferCard(newestOffer)]
  }

  if (newestCampaign != null) {
    return campaigns.slice(0, 2).map(mapCampaignCard)
  }

  if (newestOffer != null) {
    return offers.slice(0, 2).map(mapOfferCard)
  }

  return []
}
