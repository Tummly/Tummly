/** Pure Home Needs attention projection (ticket 01). */

import {
  formatRelativeTime,
  parseApiInstantMs,
} from "@/lib/operatorHome/relativeTime"

export const HOME_NEEDS_ATTENTION_MAX_ROWS = 5

export type HomeNeedsAttentionMetaKind = "warning" | "ai"

export type HomeNeedsAttentionCtaKind =
  | "review-feedback"
  | "preview-campaign"
  | "retry-remaining"
  | "duplicate-as-draft"
  | "manage-offer"
  | "view-redemptions"

export type HomeNeedsAttentionCta = {
  kind: HomeNeedsAttentionCtaKind
  label: string
}

export type HomeNeedsAttentionFeedbackFact = {
  count: number
  newestSubmittedAt: string | null
}

export type HomeNeedsAttentionCampaignFact = {
  id: number
  name: string
  status: "failed" | "partially-sent"
  updatedAt: string
  /** Base64 SQL rowversion for Duplicate as Draft. */
  rowVersion: string
}

export type HomeNeedsAttentionOfferFact = {
  id: number
  title: string
  lifetimeClaims: number
  lifetimeRedeemed: number
  /** Warning vs AI chrome. AI when this Offer is an Offers AI attention signal. */
  attentionKind: HomeNeedsAttentionMetaKind
  openVoid: {
    requestedAt: string
    pendingCount: number
  } | null
  expiry: {
    windowEnteredAt: string
    daysUntilExpiry: number
  } | null
}

type HomeNeedsAttentionItemBase = {
  title: string
  body: string
  metaKind: HomeNeedsAttentionMetaKind
  metaLine: string
  ctas: readonly HomeNeedsAttentionCta[]
}

export type HomeNeedsAttentionFeedbackItem = HomeNeedsAttentionItemBase & {
  sourceKind: "feedback"
  id: "feedback"
}

export type HomeNeedsAttentionCampaignItem = HomeNeedsAttentionItemBase & {
  sourceKind: "campaign"
  id: string
  campaignId: number
  rowVersion: string
}

export type HomeNeedsAttentionOfferItem = HomeNeedsAttentionItemBase & {
  sourceKind: "offer"
  id: string
  offerId: number
}

export type HomeNeedsAttentionItem =
  | HomeNeedsAttentionFeedbackItem
  | HomeNeedsAttentionCampaignItem
  | HomeNeedsAttentionOfferItem

export type HomeNeedsAttentionProjection = {
  allRows: readonly HomeNeedsAttentionItem[]
  visibleRows: readonly HomeNeedsAttentionItem[]
  showViewAll: boolean
  isEmpty: boolean
}

const FEEDBACK_BODY = "Negative feedback is not Resolved."

function feedbackTitle(count: number): string {
  return count === 1
    ? "1 feedback item needs attention"
    : `${count} feedback items need attention`
}

function metaPrefix(kind: HomeNeedsAttentionMetaKind): string {
  return kind === "warning" ? "Warning" : "AI"
}

function buildMetaLine(input: {
  metaKind: HomeNeedsAttentionMetaKind
  at: string | null
  locationName: string
  nowMs: number
}): string {
  const relative =
    input.at != null && input.at.trim() !== ""
      ? formatRelativeTime(input.at, input.nowMs)
      : ""
  const parts = [
    metaPrefix(input.metaKind),
    relative,
    input.locationName,
  ].filter((part) => part !== "")
  return parts.join(" · ")
}

function mapFeedbackRow(input: {
  fact: HomeNeedsAttentionFeedbackFact
  locationName: string
  nowMs: number
}): HomeNeedsAttentionFeedbackItem | null {
  if (input.fact.count <= 0) {
    return null
  }

  return {
    sourceKind: "feedback",
    id: "feedback",
    title: feedbackTitle(input.fact.count),
    body: FEEDBACK_BODY,
    metaKind: "warning",
    metaLine: buildMetaLine({
      metaKind: "warning",
      at: input.fact.newestSubmittedAt,
      locationName: input.locationName,
      nowMs: input.nowMs,
    }),
    ctas: [{ kind: "review-feedback", label: "Review feedback" }],
  }
}

function campaignBody(status: HomeNeedsAttentionCampaignFact["status"]): string {
  switch (status) {
    case "failed":
      return "This campaign failed."
    case "partially-sent":
      return "This campaign was only partially sent."
  }
}

function campaignCtas(
  status: HomeNeedsAttentionCampaignFact["status"]
): readonly HomeNeedsAttentionCta[] {
  const preview: HomeNeedsAttentionCta = {
    kind: "preview-campaign",
    label: "Preview",
  }
  switch (status) {
    case "failed":
      return [
        preview,
        { kind: "duplicate-as-draft", label: "Duplicate as Draft" },
      ]
    case "partially-sent":
      return [preview, { kind: "retry-remaining", label: "Retry remaining" }]
  }
}

function mapCampaignRow(input: {
  fact: HomeNeedsAttentionCampaignFact
  locationName: string
  nowMs: number
}): HomeNeedsAttentionCampaignItem {
  return {
    sourceKind: "campaign",
    id: `campaign-${input.fact.id}`,
    campaignId: input.fact.id,
    rowVersion: input.fact.rowVersion,
    title: input.fact.name,
    body: campaignBody(input.fact.status),
    metaKind: "warning",
    metaLine: buildMetaLine({
      metaKind: "warning",
      at: input.fact.updatedAt,
      locationName: input.locationName,
      nowMs: input.nowMs,
    }),
    ctas: campaignCtas(input.fact.status),
  }
}

function sortByMetaTimeDesc<T>(
  items: readonly T[],
  at: (item: T) => string
): T[] {
  return [...items].sort((a, b) => {
    const aMs = parseApiInstantMs(at(a))
    const bMs = parseApiInstantMs(at(b))
    const aTime = Number.isNaN(aMs) ? 0 : aMs
    const bTime = Number.isNaN(bMs) ? 0 : bMs
    return bTime - aTime
  })
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

function expiryTitle(daysUntilExpiry: number): string {
  if (daysUntilExpiry <= 0) {
    return "Offer expires today"
  }
  if (daysUntilExpiry === 1) {
    return "Offer expires in 1 day"
  }
  return `Offer expires in ${daysUntilExpiry} days`
}

function voidBody(title: string, pendingCount: number): string {
  const pendingLabel =
    pendingCount === 1
      ? "1 pending void request"
      : `${pendingCount} pending void requests`
  return `${quoteTitle(title)} has ${pendingLabel}.`
}

function offerMetaAt(fact: HomeNeedsAttentionOfferFact): string {
  if (fact.openVoid != null) {
    return fact.openVoid.requestedAt
  }
  return fact.expiry?.windowEnteredAt ?? ""
}

function mapOfferRow(input: {
  fact: HomeNeedsAttentionOfferFact
  locationName: string
  nowMs: number
}): HomeNeedsAttentionOfferItem {
  const openVoid = input.fact.openVoid
  const expiry = input.fact.expiry
  const claimsBody = `${quoteTitle(input.fact.title)} has ${claimRedeemCopy(input.fact.lifetimeClaims, input.fact.lifetimeRedeemed)}`

  let title: string
  let body: string
  if (openVoid != null) {
    title = "Open void request"
    body = voidBody(input.fact.title, openVoid.pendingCount)
  } else if (expiry != null) {
    title = expiryTitle(expiry.daysUntilExpiry)
    body = `${claimsBody} before expiry.`
  } else {
    title = input.fact.title
    body = `${claimsBody}.`
  }

  return {
    sourceKind: "offer",
    id: `offer-${input.fact.id}`,
    offerId: input.fact.id,
    title,
    body,
    metaKind: input.fact.attentionKind,
    metaLine: buildMetaLine({
      metaKind: input.fact.attentionKind,
      at: offerMetaAt(input.fact),
      locationName: input.locationName,
      nowMs: input.nowMs,
    }),
    ctas: [
      { kind: "manage-offer", label: "Manage offer" },
      { kind: "view-redemptions", label: "View redemptions" },
    ],
  }
}

function toProjection(
  allRows: readonly HomeNeedsAttentionItem[]
): HomeNeedsAttentionProjection {
  const visibleRows = allRows.slice(0, HOME_NEEDS_ATTENTION_MAX_ROWS)
  return {
    allRows,
    visibleRows,
    showViewAll: allRows.length > HOME_NEEDS_ATTENTION_MAX_ROWS,
    isEmpty: allRows.length === 0,
  }
}

export function buildHomeNeedsAttention(input: {
  locationName: string
  feedback?: HomeNeedsAttentionFeedbackFact | null
  campaigns?: readonly HomeNeedsAttentionCampaignFact[]
  offers?: readonly HomeNeedsAttentionOfferFact[]
  nowMs?: number
}): HomeNeedsAttentionProjection {
  const nowMs = input.nowMs ?? Date.now()
  const rows: HomeNeedsAttentionItem[] = []

  if (input.feedback != null) {
    const feedbackRow = mapFeedbackRow({
      fact: input.feedback,
      locationName: input.locationName,
      nowMs,
    })
    if (feedbackRow != null) {
      rows.push(feedbackRow)
    }
  }

  const campaigns = sortByMetaTimeDesc(input.campaigns ?? [], (item) => item.updatedAt)
  for (const fact of campaigns) {
    rows.push(
      mapCampaignRow({
        fact,
        locationName: input.locationName,
        nowMs,
      })
    )
  }

  const offers = sortByMetaTimeDesc(input.offers ?? [], offerMetaAt)
  for (const fact of offers) {
    rows.push(
      mapOfferRow({
        fact,
        locationName: input.locationName,
        nowMs,
      })
    )
  }

  return toProjection(rows)
}
