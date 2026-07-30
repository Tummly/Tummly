import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CaptureLocationItem,
  CaptureLocationStatus,
  CaptureLocationsSortId,
} from "@/types/dashboard"

export type OperatorCaptureLocationPerformanceRow = {
  locationId: number
  locationName: string
  status: CaptureLocationStatus
  activePlacementsText: string
  qrScansText: string
  feedbackSubmittedText: string
  submissionRateText: string
  marketingOptInsText: string
  offerClaimsText: string
  lastActivityText: string
}

export type CaptureLocationPerformanceEmptyKind =
  | "no-locations"
  | "no-results"
  | "load-error"

export const OPERATOR_CAPTURE_LOCATION_SORT_LABELS: Record<
  CaptureLocationsSortId,
  string
> = {
  "highest-qr-scans": "Highest Guest form opens",
  "highest-submission-rate": "Highest submission rate",
  "highest-marketing-opt-ins": "Highest marketing opt-ins",
  "highest-offer-claims": "Highest offer claims",
  "most-active-placements": "Most active placements",
  "most-recent-activity": "Most recent activity",
  "location-name-az": "Location name A–Z",
}

export const OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID: CaptureLocationsSortId =
  "highest-qr-scans"

export const OPERATOR_CAPTURE_LOCATION_PAGE_SIZE = 20

export function formatCaptureLocationSubmissionRate(
  feedbackSubmitted: number,
  qrScans: number
): string {
  if (qrScans <= 0) {
    return "—"
  }

  const percent = Math.round((feedbackSubmitted / qrScans) * 100)
  return `${percent}%`
}

export function formatCaptureLocationPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount <= 0) {
    return "Showing 0 of 0 locations"
  }

  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} locations`
}

/** Build Location performance table rows; client derives submission rate. */
export function buildCaptureLocationPerformanceRows(
  items: readonly CaptureLocationItem[],
  nowMs: number = Date.now()
): OperatorCaptureLocationPerformanceRow[] {
  return items.map((item) => {
    const lastActivityText =
      item.lastActivityAt == null || item.lastActivityAt === ""
        ? "—"
        : formatRelativeTime(item.lastActivityAt, nowMs) || "—"

    return {
      locationId: item.locationId,
      locationName: item.locationName,
      status: item.status,
      activePlacementsText: `${item.activePlacementsCount} placements`,
      qrScansText: `${item.qrScans} opens`,
      feedbackSubmittedText: `${item.feedbackSubmitted} feedback`,
      submissionRateText: formatCaptureLocationSubmissionRate(
        item.feedbackSubmitted,
        item.qrScans
      ),
      marketingOptInsText: `${item.marketingOptIns} opt-ins`,
      offerClaimsText: `${item.offerClaims} claims`,
      lastActivityText,
    }
  })
}
