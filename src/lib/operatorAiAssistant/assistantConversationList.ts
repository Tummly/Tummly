import { formatRelativeTime, parseApiInstantMs } from "@/lib/operatorHome/relativeTime"

export type OperatorAiAssistantListItem = {
  id: string
  title: string
  ownedLocationName: string
  lastActivityAt: string
  isArchived: boolean
}

export type OperatorAiAssistantRecentGroupId =
  | "today"
  | "yesterday"
  | "previous7"
  | "older"

export type OperatorAiAssistantRecentGroup = {
  id: OperatorAiAssistantRecentGroupId
  label: string
  rows: OperatorAiAssistantListItem[]
}

export const RECENT_GROUP_LABELS: Record<
  OperatorAiAssistantRecentGroupId,
  string
> = {
  today: "Today",
  yesterday: "Yesterday",
  previous7: "Previous 7 days",
  older: "Older",
}

const DAY_MS = 24 * 60 * 60 * 1000

export function startOfLocalDay(nowMs: number): number {
  const date = new Date(nowMs)
  date.setHours(0, 0, 0, 0)
  return date.getTime()
}

export function filterConversationsByTitle(
  rows: readonly OperatorAiAssistantListItem[],
  query: string
): OperatorAiAssistantListItem[] {
  const needle = query.trim().toLowerCase()
  if (needle.length === 0) {
    return [...rows]
  }
  return rows.filter((row) => row.title.toLowerCase().includes(needle))
}

export function sortNewestFirst(
  rows: readonly OperatorAiAssistantListItem[]
): OperatorAiAssistantListItem[] {
  return [...rows].sort((left, right) => {
    const rightMs = parseApiInstantMs(right.lastActivityAt)
    const leftMs = parseApiInstantMs(left.lastActivityAt)
    if (rightMs !== leftMs) {
      return rightMs - leftMs
    }
    return right.id.localeCompare(left.id)
  })
}

export function recentGroupIdForActivity(
  lastActivityAt: string,
  nowMs: number
): OperatorAiAssistantRecentGroupId {
  const activityMs = parseApiInstantMs(lastActivityAt)
  if (Number.isNaN(activityMs)) {
    return "older"
  }

  const startToday = startOfLocalDay(nowMs)
  const startYesterday = startToday - DAY_MS
  const startPrevious7 = startToday - 7 * DAY_MS

  if (activityMs >= startToday) {
    return "today"
  }
  if (activityMs >= startYesterday) {
    return "yesterday"
  }
  if (activityMs >= startPrevious7) {
    return "previous7"
  }
  return "older"
}

export function groupRecentConversations(
  rows: readonly OperatorAiAssistantListItem[],
  nowMs: number
): OperatorAiAssistantRecentGroup[] {
  const buckets: Record<
    OperatorAiAssistantRecentGroupId,
    OperatorAiAssistantListItem[]
  > = {
    today: [],
    yesterday: [],
    previous7: [],
    older: [],
  }

  for (const row of sortNewestFirst(rows)) {
    buckets[recentGroupIdForActivity(row.lastActivityAt, nowMs)].push(row)
  }

  const order: OperatorAiAssistantRecentGroupId[] = [
    "today",
    "yesterday",
    "previous7",
    "older",
  ]

  return order
    .filter((id) => buckets[id].length > 0)
    .map((id) => ({
      id,
      label: RECENT_GROUP_LABELS[id],
      rows: buckets[id],
    }))
}

export function formatConversationListMeta(
  ownedLocationName: string,
  lastActivityAt: string,
  nowMs: number
): string {
  const relative = formatRelativeTime(lastActivityAt, nowMs)
  if (relative.length === 0) {
    return ownedLocationName
  }
  return `${ownedLocationName} · ${relative}`
}
