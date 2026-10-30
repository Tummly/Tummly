/**
 * Client re-check of the public trust contract (CONTRACT §2, §3, §8, §9).
 * The API already filters; this module fails closed again: a bad item is
 * omitted, a bad CTA is dropped, and nothing is downgraded to a weaker status.
 */

export type PublicTrustStatus = "Verified" | "Roadmap" | "UnderReview"

export type PublicTrustCtaType = "ExternalProof" | "InternalPage" | "CookieSettings"

export type PublicTrustCta = {
  type: PublicTrustCtaType
  label: string
  href: string | null
}

export type PublicTrustIndicator = {
  itemNumber: number
  title: string
  status: PublicTrustStatus
  statusLabel: string
  iconKey: string | null
  imageUrl: string | null
  altText: string
  tooltip: string
  cta: PublicTrustCta | null
}

const CTA_LABEL_MAX = 60
const UNDER_REVIEW_LABEL = "Under review"
const CTA_TYPES: readonly PublicTrustCtaType[] = [
  "ExternalProof",
  "InternalPage",
  "CookieSettings",
]

type RawRecord = { [key: string]: unknown }

function isRecord(value: unknown): value is RawRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value)
}

function trimmedString(value: unknown): string {
  return typeof value === "string" ? value.trim() : ""
}

function optionalString(value: unknown): string | null {
  const text = trimmedString(value)
  return text.length > 0 ? text : null
}

/** True for whitespace, C0 controls and DEL. */
function hasControlOrSpace(text: string): boolean {
  for (const ch of text) {
    if (ch.charCodeAt(0) <= 0x20 || ch.charCodeAt(0) === 0x7f || /\s/.test(ch)) return true
  }
  return false
}

/** One leading `/`, not `//`, no backslash, no whitespace or control characters. */
function isInternalPath(href: string): boolean {
  return (
    href.startsWith("/")
    && !href.startsWith("//")
    && !href.includes("\\")
    && !hasControlOrSpace(href)
  )
}

function isHttps(href: string): boolean {
  if (hasControlOrSpace(href)) return false
  try {
    return new URL(href).protocol === "https:"
  } catch {
    return false
  }
}

function normalizeCta(raw: unknown, status: PublicTrustStatus): PublicTrustCta | null {
  if (status !== "Verified" || !isRecord(raw)) return null

  const type = raw.type
  if (typeof type !== "string" || !(CTA_TYPES as readonly string[]).includes(type)) {
    return null
  }
  const ctaType = type as PublicTrustCtaType

  const label = trimmedString(raw.label)
  if (label.length === 0 || label.length > CTA_LABEL_MAX) return null

  const href = optionalString(raw.href)

  if (ctaType === "ExternalProof") {
    return href !== null && isHttps(href) ? { type: ctaType, label, href } : null
  }
  if (ctaType === "InternalPage") {
    return href !== null && isInternalPath(href) ? { type: ctaType, label, href } : null
  }
  return href === null ? { type: ctaType, label, href: null } : null
}

/** Keeps an https URL or a single-slash path; anything else (and every Roadmap item) gets null. */
function normalizeImageUrl(raw: unknown, status: PublicTrustStatus): string | null {
  if (status === "Roadmap") return null
  const href = optionalString(raw)
  if (href === null) return null
  return isHttps(href) || isInternalPath(href) ? href : null
}

/** Returns the public item, or `null` when the row must be omitted. */
function normalizeItem(raw: unknown): PublicTrustIndicator | null {
  if (!isRecord(raw)) return null

  const itemNumber = raw.itemNumber
  if (typeof itemNumber !== "number" || !Number.isInteger(itemNumber) || itemNumber <= 0) {
    return null
  }

  const title = trimmedString(raw.title)
  if (title.length === 0) return null

  const rawStatus = raw.status
  if (rawStatus !== "Verified" && rawStatus !== "Roadmap" && rawStatus !== "UnderReview") {
    return null
  }
  const status: PublicTrustStatus = rawStatus

  let statusLabel: string
  if (status === "UnderReview") {
    statusLabel = UNDER_REVIEW_LABEL
  } else {
    statusLabel = trimmedString(raw.statusLabel)
    if (statusLabel.length === 0) return null
  }

  return {
    itemNumber,
    title,
    status,
    statusLabel,
    iconKey: optionalString(raw.iconKey),
    imageUrl: normalizeImageUrl(raw.imageUrl, status),
    altText: trimmedString(raw.altText),
    tooltip: trimmedString(raw.tooltip),
    cta: normalizeCta(raw.cta, status),
  }
}

/**
 * Normalizes the public endpoint payload. Sorted by `itemNumber`; when two
 * rows share a number, the first one in the payload is kept.
 */
export function normalizePublicTrustIndicators(raw: unknown): PublicTrustIndicator[] {
  if (!Array.isArray(raw)) return []

  const seen = new Set<number>()
  const items: PublicTrustIndicator[] = []
  for (const element of raw) {
    const item = normalizeItem(element)
    if (item === null || seen.has(item.itemNumber)) continue
    seen.add(item.itemNumber)
    items.push(item)
  }

  return items.sort((a, b) => a.itemNumber - b.itemNumber)
}
