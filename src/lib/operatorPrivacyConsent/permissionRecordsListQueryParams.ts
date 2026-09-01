import {
  getDateValue,
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  parseLocalDateKey,
} from "@/lib/operatorHome/homePerformanceDateRange"

export const PERMISSION_RECORDS_PAGE_SIZE = 25

export type PermissionRecordsListQueryParams = {
  q?: string
  permission?: string[]
  currentState?: string[]
  location?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  sort?: "recent-activity" | "oldest-first"
  page?: number
  pageSize?: number
  utcOffsetMinutes?: number
}

export type PermissionRecordsListApiRow = {
  id: string
  locationGuestId: number
  locationId: number
  guestName: string
  permissionId: string
  permissionLabel: string
  currentState: "granted" | "withdrawn"
  locationLabel: string
  sourceLabel: string
  recordedAt: string
}

export type PermissionRecordsListResponse = {
  success: boolean
  rows: PermissionRecordsListApiRow[]
  totalCount: number
  page: number
  pageSize: number
}

function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

function customRangeToUtcBounds(
  dateFrom: string,
  dateTo: string
): { from: string; to: string } {
  const from = parseLocalDateKey(dateFrom)
  const end = parseLocalDateKey(dateTo)
  const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

export function buildPermissionRecordsListQueryParams(input: {
  searchQuery: string
  page: number
  pageSize?: number
  applied?: OperatorFilterSelection | null
  now?: Date
}): PermissionRecordsListQueryParams {
  const selection = input.applied ?? {}
  const now = input.now ?? new Date()
  const q = input.searchQuery.trim()
  const permission = getMultiSelectIds(selection, "permission")
  const currentState = getMultiSelectIds(selection, "currentState").filter(
    (id) => id === "granted" || id === "withdrawn"
  )
  const location = getMultiSelectIds(selection, "location")

  const params: PermissionRecordsListQueryParams = {
    sort: "recent-activity",
    page: input.page,
    pageSize: input.pageSize ?? PERMISSION_RECORDS_PAGE_SIZE,
  }

  if (q.length > 0) {
    params.q = q
  }
  if (permission.length > 0) {
    params.permission = permission
  }
  if (currentState.length > 0) {
    params.currentState = currentState
  }
  if (location.length > 0) {
    params.location = location
  }

  const date = getDateValue(selection, "date")
  if (date.kind === "preset") {
    params.datePreset = date.preset
    params.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  } else if (date.kind === "custom") {
    const bounds = customRangeToUtcBounds(date.dateFrom, date.dateTo)
    params.dateFrom = bounds.from
    params.dateTo = bounds.to
  }

  return params
}
