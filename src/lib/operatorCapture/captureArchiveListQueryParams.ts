import { parseLocalDateKey } from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  CaptureArchiveFilters,
  CaptureArchiveSortId,
} from "@/lib/operatorCapture/buildCaptureArchive"

export const CAPTURE_ARCHIVE_PAGE_SIZE = 25

export type CaptureArchiveListQueryParams = {
  q?: string
  locationIds?: number[]
  qrTypes?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  archivedBy?: string[]
  sort: CaptureArchiveSortId
  page: number
  pageSize: number
  /** Minutes east of UTC; required when sending a non-any-time datePreset. */
  utcOffsetMinutes?: number
}

function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

/** Custom archived-date range → UTC bounds (Guests-aligned inclusive local days). */
function customRangeToUtcBounds(
  dateFrom: string,
  dateTo: string
): { from: string; to: string } {
  const from = parseLocalDateKey(dateFrom)
  const end = parseLocalDateKey(dateTo)
  const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

/**
 * Build GET /api/capture/placements/archived query params (ADR-0024).
 * Omits empty optional filters; sends utcOffsetMinutes for date presets.
 */
export function buildCaptureArchiveListQueryParams(input: {
  q: string
  filters: CaptureArchiveFilters
  sort: CaptureArchiveSortId
  page: number
  pageSize?: number
  now?: Date
}): CaptureArchiveListQueryParams {
  const now = input.now ?? new Date()
  const pageSize = input.pageSize ?? CAPTURE_ARCHIVE_PAGE_SIZE
  const params: CaptureArchiveListQueryParams = {
    sort: input.sort,
    page: input.page,
    pageSize,
  }

  const q = input.q.trim()
  if (q !== "") {
    params.q = q
  }

  if (input.filters.locationIds.length > 0) {
    params.locationIds = [...input.filters.locationIds]
  }

  if (input.filters.placementTypes.length > 0) {
    params.qrTypes = [...input.filters.placementTypes]
  }

  if (input.filters.archivedByDisplayNames.length > 0) {
    params.archivedBy = [...input.filters.archivedByDisplayNames]
  }

  const { archivedDate } = input.filters
  if (archivedDate.preset === "custom") {
    if (
      archivedDate.dateFrom != null
      && archivedDate.dateFrom !== ""
      && archivedDate.dateTo != null
      && archivedDate.dateTo !== ""
    ) {
      const bounds = customRangeToUtcBounds(
        archivedDate.dateFrom,
        archivedDate.dateTo
      )
      params.datePreset = "custom"
      params.dateFrom = bounds.from
      params.dateTo = bounds.to
    }
  } else if (archivedDate.preset !== "any-time") {
    params.datePreset = archivedDate.preset
    params.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  }

  return params
}
