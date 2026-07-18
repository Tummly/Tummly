const MINUTE_MS = 60_000
const HOUR_MS = 60 * MINUTE_MS
const DAY_MS = 24 * HOUR_MS

const HAS_TIMEZONE_RE = /(?:[zZ]|[+-]\d{2}:?\d{2})$/
const DATE_ONLY_RE = /^\d{4}-\d{2}-\d{2}$/

/**
 * Parse an API instant. SQL Server / System.Text.Json often emit UTC
 * DateTimes without a `Z` suffix; browsers treat those as local time.
 * Treat timezone-less datetimes as UTC so relative labels stay correct.
 */
export function parseApiInstantMs(iso: string): number {
  const trimmed = iso.trim()
  if (!trimmed) {
    return Number.NaN
  }

  if (HAS_TIMEZONE_RE.test(trimmed) || DATE_ONLY_RE.test(trimmed)) {
    return Date.parse(trimmed)
  }

  return Date.parse(`${trimmed}Z`)
}

/** Format an ISO timestamp as a short relative label for Latest activity. */
export function formatRelativeTime(
  iso: string,
  nowMs: number = Date.now()
): string {
  const thenMs = parseApiInstantMs(iso)
  if (Number.isNaN(thenMs)) {
    return ""
  }

  const deltaMs = Math.max(0, nowMs - thenMs)

  if (deltaMs < MINUTE_MS) {
    return "just now"
  }

  const minutes = Math.floor(deltaMs / MINUTE_MS)
  if (minutes < 60) {
    return minutes === 1 ? "1 minute ago" : `${minutes} minutes ago`
  }

  const hours = Math.floor(deltaMs / HOUR_MS)
  if (hours < 24) {
    return hours === 1 ? "1 hour ago" : `${hours} hours ago`
  }

  const days = Math.floor(deltaMs / DAY_MS)
  return days === 1 ? "1 day ago" : `${days} days ago`
}
