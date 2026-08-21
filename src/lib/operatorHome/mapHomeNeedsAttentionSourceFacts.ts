import type {
  HomeNeedsAttentionCampaignFact,
  HomeNeedsAttentionFeedbackFact,
  HomeNeedsAttentionOfferFact,
} from "@/lib/operatorHome/buildHomeNeedsAttention"
import type {
  CampaignsListItem,
  CatalogOffersListItem,
  OpenVoidAttentionOfferApi,
} from "@/types/operatorCampaigns"

const DAY_MS = 24 * 60 * 60 * 1000
const EXPIRY_WINDOW_DAYS = 7

export type HomeNeedsAttentionFeedbackSource = {
  count: number
  newestSubmittedAt: string | null
}

function venueLocalDateKey(nowMs: number, utcOffsetMinutes: number): string {
  const shifted = new Date(nowMs + utcOffsetMinutes * 60_000)
  const year = shifted.getUTCFullYear()
  const month = String(shifted.getUTCMonth() + 1).padStart(2, "0")
  const day = String(shifted.getUTCDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

function dateKeyToUtcMs(dateKey: string): number {
  return Date.parse(`${dateKey}T00:00:00.000Z`)
}

function addDaysToDateKey(dateKey: string, days: number): string {
  const ms = dateKeyToUtcMs(dateKey) + days * DAY_MS
  return new Date(ms).toISOString().slice(0, 10)
}

function mapExpiry(input: {
  expiryDate: string | null
  nowMs: number
  utcOffsetMinutes: number
}): HomeNeedsAttentionOfferFact["expiry"] {
  if (input.expiryDate == null || input.expiryDate.trim() === "") {
    return null
  }

  const expiryKey = input.expiryDate.trim().slice(0, 10)
  const todayKey = venueLocalDateKey(input.nowMs, input.utcOffsetMinutes)
  const daysUntilExpiry = Math.round(
    (dateKeyToUtcMs(expiryKey) - dateKeyToUtcMs(todayKey)) / DAY_MS
  )

  return {
    windowEnteredAt: addDaysToDateKey(expiryKey, -EXPIRY_WINDOW_DAYS),
    daysUntilExpiry,
  }
}

function mapCampaign(
  item: CampaignsListItem
): HomeNeedsAttentionCampaignFact | null {
  if (item.status !== "failed" && item.status !== "partially-sent") {
    return null
  }

  return {
    id: item.id,
    name: item.name,
    status: item.status,
    updatedAt: item.updatedAt,
    rowVersion: item.rowVersion,
  }
}

function mapOfferFact(input: {
  id: number
  title: string
  lifetimeClaims: number
  lifetimeRedeemed: number
  expiryDate: string | null
  openVoid: HomeNeedsAttentionOfferFact["openVoid"]
  nowMs: number
  utcOffsetMinutes: number
}): HomeNeedsAttentionOfferFact {
  return {
    id: input.id,
    title: input.title,
    lifetimeClaims: input.lifetimeClaims,
    lifetimeRedeemed: input.lifetimeRedeemed,
    attentionKind: "warning",
    openVoid: input.openVoid,
    expiry: mapExpiry({
      expiryDate: input.expiryDate,
      nowMs: input.nowMs,
      utcOffsetMinutes: input.utcOffsetMinutes,
    }),
  }
}

/**
 * Map source-queue reads onto assembler facts. Membership stays on the
 * Offers / Campaigns / Feedback APIs — this does not invent extra rows.
 */
export function mapHomeNeedsAttentionSourceFacts(input: {
  feedback: HomeNeedsAttentionFeedbackSource
  campaigns: readonly CampaignsListItem[]
  offers: readonly CatalogOffersListItem[]
  openVoids: readonly OpenVoidAttentionOfferApi[]
  nowMs?: number
  utcOffsetMinutes?: number
}): {
  feedback: HomeNeedsAttentionFeedbackFact
  campaigns: HomeNeedsAttentionCampaignFact[]
  offers: HomeNeedsAttentionOfferFact[]
} {
  const nowMs = input.nowMs ?? Date.now()
  const utcOffsetMinutes =
    input.utcOffsetMinutes ?? -new Date(nowMs).getTimezoneOffset()
  const voidByOfferId = new Map(
    input.openVoids.map((item) => [item.offerId, item] as const)
  )
  const offers: HomeNeedsAttentionOfferFact[] = []
  const seenOfferIds = new Set<number>()

  for (const item of input.offers) {
    seenOfferIds.add(item.id)
    const openVoid = voidByOfferId.get(item.id)
    offers.push(
      mapOfferFact({
        id: item.id,
        title: item.title,
        lifetimeClaims: item.lifetimeClaims ?? 0,
        lifetimeRedeemed: item.lifetimeRedeemed ?? 0,
        expiryDate: item.expiryDate,
        openVoid:
          openVoid == null
            ? null
            : {
                requestedAt: "",
                pendingCount: openVoid.pendingCount,
              },
        nowMs,
        utcOffsetMinutes,
      })
    )
  }

  for (const openVoid of input.openVoids) {
    if (seenOfferIds.has(openVoid.offerId)) {
      continue
    }
    offers.push(
      mapOfferFact({
        id: openVoid.offerId,
        title: openVoid.offerTitle,
        lifetimeClaims: 0,
        lifetimeRedeemed: 0,
        expiryDate: null,
        openVoid: {
          requestedAt: "",
          pendingCount: openVoid.pendingCount,
        },
        nowMs,
        utcOffsetMinutes,
      })
    )
  }

  return {
    feedback: {
      count: input.feedback.count,
      newestSubmittedAt: input.feedback.newestSubmittedAt,
    },
    campaigns: input.campaigns.flatMap((item) => {
      const mapped = mapCampaign(item)
      return mapped == null ? [] : [mapped]
    }),
    offers,
  }
}
