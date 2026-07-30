import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CaptureArchivedPlacementItem,
  CapturePlacementQrType,
} from "@/types/dashboard"

export type CaptureArchiveSortId =
  | "recently-archived"
  | "oldest-archived"
  | "highest-qr-scans"
  | "highest-feedback"
  | "most-recent-activity"
  | "placement-name-az"

export type CaptureArchiveDatePresetId =
  | "any-time"
  | "today"
  | "last-7"
  | "last-30"
  | "this-month"
  | "previous-month"
  | "custom"

export type CaptureArchiveFilters = {
  locationIds: readonly number[]
  /** Empty = all types. */
  placementTypes: readonly CapturePlacementQrType[]
  archivedDate: {
    preset: CaptureArchiveDatePresetId
    /** Inclusive YYYY-MM-DD when preset is custom. */
    dateFrom?: string
    dateTo?: string
  }
  /** Empty = any archiver. */
  archivedByDisplayNames: readonly string[]
}

export type OperatorCaptureArchiveRow = {
  qrCodeId: number
  locationId: number
  locationName: string
  qrType: CapturePlacementQrType
  placementLabel: string
  linkName: string | null
  channel: CaptureArchivedPlacementItem["channel"]
  archivedOnText: string
  archivedByText: string
  qrScansText: string
  feedbackSubmittedText: string
  lastScanText: string
  canRestore: boolean
  canDuplicateAsNew: boolean
  qrLinkUrl: string
  internalDescription: string | null
}

export type CaptureArchiveListResult = {
  rows: OperatorCaptureArchiveRow[]
  /** True when totalCount is 0 with no search/filters. */
  isTrueEmpty: boolean
  /** True when totalCount is 0 but search/filters are active. */
  isNoMatch: boolean
  activeFilterCount: number
  totalCount: number
  page: number
  pageSize: number
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
}

export const CAPTURE_ARCHIVE_SORT_OPTIONS: readonly {
  id: CaptureArchiveSortId
  label: string
}[] = [
  { id: "recently-archived", label: "Recently archived" },
  { id: "oldest-archived", label: "Oldest archived" },
  { id: "highest-qr-scans", label: "Highest QR scans" },
  { id: "highest-feedback", label: "Highest feedback submitted" },
  { id: "most-recent-activity", label: "Most recent activity" },
  { id: "placement-name-az", label: "Placement name A–Z" },
] as const

export const CAPTURE_ARCHIVE_PLACEMENT_TYPE_OPTIONS: readonly {
  id: CapturePlacementQrType
  label: string
}[] = [
  { id: "CounterCard", label: "Counter card" },
  { id: "PackagingSticker", label: "Packaging sticker" },
  { id: "DeliveryInsert", label: "Delivery insert" },
  { id: "WindowSticker", label: "Window sticker" },
  { id: "SmartGuest", label: "Smart Guest" },
  { id: "DigitalGuestLink", label: "Digital guest link" },
] as const

const QR_TYPE_LABELS: Record<CapturePlacementQrType, string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
  DigitalGuestLink: "Digital guest link",
}

export const DEFAULT_CAPTURE_ARCHIVE_FILTERS: CaptureArchiveFilters = {
  locationIds: [],
  placementTypes: [],
  archivedDate: { preset: "any-time" },
  archivedByDisplayNames: [],
}

export function countActiveArchiveFilters(
  filters: CaptureArchiveFilters,
  options?: { showLocationFilter?: boolean }
): number {
  let count = 0
  if (
    (options?.showLocationFilter ?? true)
    && filters.locationIds.length > 0
  ) {
    count += 1
  }
  if (filters.placementTypes.length > 0) {
    count += 1
  }
  if (filters.archivedDate.preset === "custom") {
    if (
      filters.archivedDate.dateFrom != null
      && filters.archivedDate.dateFrom !== ""
      && filters.archivedDate.dateTo != null
      && filters.archivedDate.dateTo !== ""
    ) {
      count += 1
    }
  } else if (filters.archivedDate.preset !== "any-time") {
    count += 1
  }
  if (filters.archivedByDisplayNames.length > 0) {
    count += 1
  }
  return count
}

function placementLabel(fact: CaptureArchivedPlacementItem): string {
  if (
    fact.qrType === "DigitalGuestLink"
    && fact.linkName != null
    && fact.linkName.trim() !== ""
  ) {
    return fact.linkName.trim()
  }
  return QR_TYPE_LABELS[fact.qrType]
}

function formatArchivedOn(iso: string | null | undefined): string {
  if (iso == null || iso === "") {
    return "—"
  }
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return "—"
  }
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(date)
}

export function formatCaptureArchivePageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount <= 0) {
    return "Showing 0 of 0 archived placements"
  }

  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} archived placements`
}

export function resolveCaptureArchiveEmptyState(input: {
  totalCount: number
  searchQuery: string
  filters: CaptureArchiveFilters
  showLocationFilter?: boolean
}): { isTrueEmpty: boolean; isNoMatch: boolean } {
  if (input.totalCount > 0) {
    return { isTrueEmpty: false, isNoMatch: false }
  }

  const hasSearch = input.searchQuery.trim() !== ""
  const activeFilterCount = countActiveArchiveFilters(input.filters, {
    showLocationFilter: input.showLocationFilter,
  })
  const hasQuerySignals = hasSearch || activeFilterCount > 0

  return {
    isTrueEmpty: !hasQuerySignals,
    isNoMatch: hasQuerySignals,
  }
}

/** Map a server page of archived placements to table display rows. */
export function mapCaptureArchiveRows(
  placements: readonly CaptureArchivedPlacementItem[],
  nowMs: number = Date.now()
): OperatorCaptureArchiveRow[] {
  return placements.map((fact) => {
    const lastScanText =
      fact.lastScanAt == null || fact.lastScanAt === ""
        ? "—"
        : formatRelativeTime(fact.lastScanAt, nowMs) || "—"

    return {
      qrCodeId: fact.qrCodeId,
      locationId: fact.locationId,
      locationName: fact.locationName,
      qrType: fact.qrType,
      placementLabel: placementLabel(fact),
      linkName: fact.linkName ?? null,
      channel: fact.channel ?? null,
      archivedOnText: formatArchivedOn(fact.archivedAt),
      archivedByText: fact.archivedByDisplayName?.trim() || "—",
      qrScansText: `${fact.qrScans} scans`,
      feedbackSubmittedText: `${fact.feedbackSubmitted} feedback submissions`,
      lastScanText,
      canRestore: fact.canRestore,
      canDuplicateAsNew: fact.qrType === "DigitalGuestLink",
      qrLinkUrl: fact.qrLinkUrl,
      internalDescription: fact.internalDescription ?? null,
    } satisfies OperatorCaptureArchiveRow
  })
}

export type BuildCaptureArchiveListInput = {
  placements: readonly CaptureArchivedPlacementItem[]
  totalCount: number
  page: number
  pageSize: number
  searchQuery: string
  filters: CaptureArchiveFilters
  nowMs?: number
  showLocationFilter?: boolean
}

/** Project Archive table view-model from a server page + query chrome. */
export function buildCaptureArchiveList(
  input: BuildCaptureArchiveListInput
): CaptureArchiveListResult {
  const nowMs = input.nowMs ?? Date.now()
  const activeFilterCount = countActiveArchiveFilters(input.filters, {
    showLocationFilter: input.showLocationFilter,
  })
  const empty = resolveCaptureArchiveEmptyState({
    totalCount: input.totalCount,
    searchQuery: input.searchQuery,
    filters: input.filters,
    showLocationFilter: input.showLocationFilter,
  })
  const maxPage = Math.max(1, Math.ceil(input.totalCount / input.pageSize))

  return {
    rows: mapCaptureArchiveRows(input.placements, nowMs),
    isTrueEmpty: empty.isTrueEmpty,
    isNoMatch: empty.isNoMatch,
    activeFilterCount,
    totalCount: input.totalCount,
    page: input.page,
    pageSize: input.pageSize,
    pageRangeLabel: formatCaptureArchivePageRangeLabel(
      input.page,
      input.pageSize,
      input.totalCount
    ),
    canGoPrevious: input.page > 1,
    canGoNext: input.page < maxPage && input.totalCount > 0,
  }
}

/** Prefill Link name for Duplicate as new (digital only). */
export function duplicateDigitalGuestLinkName(linkName: string): string {
  const trimmed = linkName.trim()
  if (trimmed === "") {
    return "(copy)"
  }
  return `${trimmed} (copy)`
}
