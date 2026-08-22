import type { OffersNeedsAttentionFact } from "@/lib/operatorOffers/buildOffersNeedsAttentionOverview"
import type { OpenVoidAttentionOffer } from "@/lib/operatorOffers/voidRequestAdapters"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"
import {
  formatRelativeTime,
  parseApiInstantMs,
} from "@/lib/operatorHome/relativeTime"

const DAY_MS = 24 * 60 * 60 * 1000
const EXPIRY_WINDOW_DAYS = 7
const CHOOSE_EXPIRY_DATE = "choose_expiry_date"

export type OffersNeedsAttentionExpiringOffer = {
  id: number
  title: string
  lifetimeClaims: number
  lifetimeRedeemed: number
}

export type ExpiringOffersOverviewSelection = {
  offers: OffersNeedsAttentionExpiringOffer[]
  leadWindowEnteredAt: string | null
}

export type OffersNeedsAttentionOpenVoidOffer = OpenVoidAttentionOffer

function venueLocalDateKey(nowMs: number, utcOffsetMinutes: number): string {
  const shifted = new Date(nowMs + utcOffsetMinutes * 60_000)
  const year = shifted.getUTCFullYear()
  const month = String(shifted.getUTCMonth() + 1).padStart(2, "0")
  const day = String(shifted.getUTCDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

function dateKeyMs(dateKey: string): number {
  return Date.parse(`${dateKey}T00:00:00.000Z`)
}

function addDaysToDateKey(dateKey: string, days: number): string {
  return new Date(dateKeyMs(dateKey) + days * DAY_MS).toISOString().slice(0, 10)
}

function venueLocalDayStartIso(
  dateKey: string,
  utcOffsetMinutes: number
): string {
  return new Date(dateKeyMs(dateKey) - utcOffsetMinutes * 60_000).toISOString()
}

function daysUntilExpiry(expiryKey: string, todayKey: string): number {
  return Math.round((dateKeyMs(expiryKey) - dateKeyMs(todayKey)) / DAY_MS)
}

function isSevenDayRuleOffer(
  item: CatalogOffersListItem,
  todayKey: string
): string | null {
  if (item.validity !== CHOOSE_EXPIRY_DATE) {
    return null
  }
  const raw = item.expiryDate?.trim() ?? ""
  if (raw === "") {
    return null
  }
  const expiryKey = raw.slice(0, 10)
  const days = daysUntilExpiry(expiryKey, todayKey)
  if (days < 0 || days > EXPIRY_WINDOW_DAYS) {
    return null
  }
  return expiryKey
}

/**
 * 7-day-rule set for the Offers expiry overview. Callers must not pass the
 * full Needs attention list into the expiry copy builder.
 */
export function selectExpiringOffersForOverview(input: {
  items: readonly CatalogOffersListItem[]
  nowMs: number
  utcOffsetMinutes: number
}): ExpiringOffersOverviewSelection {
  const todayKey = venueLocalDateKey(input.nowMs, input.utcOffsetMinutes)
  const matched: {
    offer: OffersNeedsAttentionExpiringOffer
    expiryKey: string
  }[] = []

  for (const item of input.items) {
    const expiryKey = isSevenDayRuleOffer(item, todayKey)
    if (expiryKey == null) {
      continue
    }
    matched.push({
      expiryKey,
      offer: {
        id: item.id,
        title: item.title,
        lifetimeClaims: item.lifetimeClaims ?? 0,
        lifetimeRedeemed: item.lifetimeRedeemed ?? 0,
      },
    })
  }

  matched.sort((a, b) => {
    if (a.expiryKey !== b.expiryKey) {
      return a.expiryKey < b.expiryKey ? -1 : 1
    }
    return a.offer.id - b.offer.id
  })

  const offers = matched.map((entry) => entry.offer)
  const lead = matched[0]
  return {
    offers,
    leadWindowEnteredAt:
      lead == null
        ? null
        : venueLocalDayStartIso(
            addDaysToDateKey(lead.expiryKey, -EXPIRY_WINDOW_DAYS),
            input.utcOffsetMinutes
          ),
  }
}

function quoteTitle(title: string): string {
  return `“${title}”`
}

function voidRelativeTimeLabel(
  offers: readonly OffersNeedsAttentionOpenVoidOffer[],
  nowMs: number
): string | undefined {
  let newestRaw = ""
  let newestMs = Number.NaN
  for (const offer of offers) {
    const raw = offer.newestPendingRequestedAtUtc?.trim() ?? ""
    if (raw === "") {
      continue
    }
    const ms = parseApiInstantMs(raw)
    if (Number.isNaN(ms)) {
      continue
    }
    if (Number.isNaN(newestMs) || ms > newestMs) {
      newestMs = ms
      newestRaw = raw
    }
  }
  if (newestRaw === "") {
    return undefined
  }
  const label = formatRelativeTime(newestRaw, nowMs)
  return label !== "" ? label : undefined
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
  nowMs: number
}): OffersNeedsAttentionFact[] {
  if (input.offers.length === 0) {
    return []
  }

  const relativeTimeLabel = voidRelativeTimeLabel(input.offers, input.nowMs)
  const metaParts = [
    ...(relativeTimeLabel != null ? [relativeTimeLabel] : []),
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
