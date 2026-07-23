import { parseLocalDateKey } from "@/lib/operatorHome/homePerformanceDateRange"

export function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

export function customRangeToUtcBounds(
  dateFrom: string,
  dateTo: string
): { from: string; to: string } {
  const from = parseLocalDateKey(dateFrom)
  const end = parseLocalDateKey(dateTo)
  const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}
