const LONDON_TIME_ZONE = "Europe/London"

const LONDON_DATE_TIME_FORMAT = new Intl.DateTimeFormat("en-GB", {
  timeZone: LONDON_TIME_ZONE,
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
})

const LONDON_WALL_CLOCK_FORMAT = new Intl.DateTimeFormat("en-GB", {
  timeZone: LONDON_TIME_ZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hourCycle: "h23",
})

/** Display date for admin screens: `28 Oct 2026, 09:00` in Europe/London. */
export function formatLondonDateTime(isoUtc: string): string {
  const date = new Date(isoUtc)
  if (Number.isNaN(date.getTime())) {
    return isoUtc
  }
  return LONDON_DATE_TIME_FORMAT.format(date)
}

/** Offset of London from UTC, in milliseconds, at the given instant. */
function londonOffsetMs(utcMs: number): number {
  const parts = Object.fromEntries(
    LONDON_WALL_CLOCK_FORMAT.formatToParts(new Date(utcMs)).map((part) => [
      part.type,
      part.value,
    ])
  )
  const wallClockAsUtc = Date.UTC(
    Number(parts.year),
    Number(parts.month) - 1,
    Number(parts.day),
    Number(parts.hour),
    Number(parts.minute),
    Number(parts.second)
  )
  return wallClockAsUtc - Math.floor(utcMs / 1000) * 1000
}

const DAY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/

/**
 * UTC instant for the start (00:00:00.000) or end (23:59:59.999) of a London
 * calendar day given as `YYYY-MM-DD`. Returns null for an invalid date.
 */
export function londonDayBoundaryUtcIso(
  day: string,
  boundary: "start" | "end"
): string | null {
  const match = DAY_PATTERN.exec(day)
  if (!match) {
    return null
  }

  const year = Number(match[1])
  const month = Number(match[2])
  const dayOfMonth = Number(match[3])
  const isEnd = boundary === "end"
  const wallClockMs = isEnd
    ? Date.UTC(year, month - 1, dayOfMonth, 23, 59, 59, 999)
    : Date.UTC(year, month - 1, dayOfMonth, 0, 0, 0, 0)

  const probe = new Date(wallClockMs)
  if (
    probe.getUTCFullYear() !== year ||
    probe.getUTCMonth() !== month - 1 ||
    probe.getUTCDate() !== dayOfMonth
  ) {
    return null
  }

  // Two passes: the London offset can differ at the instant it converts to.
  const firstPass = wallClockMs - londonOffsetMs(wallClockMs)
  return new Date(wallClockMs - londonOffsetMs(firstPass)).toISOString()
}
