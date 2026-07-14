const MINUTE_MS = 60_000
const HOUR_MS = 60 * MINUTE_MS
const DAY_MS = 24 * HOUR_MS

/** Format an ISO timestamp as a short relative label for Latest activity. */
export function formatRelativeTime(
  iso: string,
  nowMs: number = Date.now()
): string {
  const thenMs = new Date(iso).getTime()
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
