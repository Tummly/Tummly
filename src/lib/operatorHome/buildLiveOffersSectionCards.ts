import type {
  CampaignsListItem,
  CatalogOffersListItem,
} from "@/types/operatorCampaigns"

export const LIVE_OFFERS_METRIC_DASH = "—"

const LIVE_CAMPAIGN_STATUSES = new Set(["scheduled", "sending"])

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
}

export type OperatorHomeLiveOfferCard = {
  kind: "offer"
  id: number
  title: string
  status: string
  statusLabel: string
  metricParts: string[]
  description: string | null
  expiryDate: string | null
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

function formatOfferExpiryLabel(expiryDate: string | null): string {
  if (expiryDate == null || expiryDate.trim().length === 0) {
    return `Expires ${LIVE_OFFERS_METRIC_DASH}`
  }
  const parsed = new Date(`${expiryDate}T00:00:00.000Z`)
  if (Number.isNaN(parsed.getTime())) {
    return `Expires ${LIVE_OFFERS_METRIC_DASH}`
  }
  const formatted = new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(parsed)
  return `Expires ${formatted}`
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
  return {
    kind: "offer",
    id: item.id,
    title: item.title,
    status: item.status,
    statusLabel: statusLabelForWireStatus(item.status),
    description: item.description ?? null,
    expiryDate: item.expiryDate,
    metricParts: [
      `${formatLiveMetricCount(item.lifetimeClaims)} claims`,
      `${formatLiveMetricCount(item.lifetimeRedeemed)} redemptions`,
      formatOfferExpiryLabel(item.expiryDate),
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
