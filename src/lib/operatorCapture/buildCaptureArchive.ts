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
  /** True when there are zero archived codes before search/filters. */
  isTrueEmpty: boolean
  /** True when filters/search yield zero rows but archive has data. */
  isNoMatch: boolean
  activeFilterCount: number
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

function startOfUtcDay(ms: number): number {
  const d = new Date(ms)
  return Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate())
}

function matchesArchivedDate(
  archivedAt: string | null | undefined,
  filter: CaptureArchiveFilters["archivedDate"],
  nowMs: number
): boolean {
  if (filter.preset === "any-time") {
    return true
  }
  if (archivedAt == null || archivedAt === "") {
    return false
  }
  const archivedMs = new Date(archivedAt).getTime()
  if (Number.isNaN(archivedMs)) {
    return false
  }

  const todayStart = startOfUtcDay(nowMs)
  if (filter.preset === "today") {
    return archivedMs >= todayStart && archivedMs < todayStart + 24 * 60 * 60 * 1000
  }
  if (filter.preset === "last-7") {
    return archivedMs >= todayStart - 6 * 24 * 60 * 60 * 1000
  }
  if (filter.preset === "last-30") {
    return archivedMs >= todayStart - 29 * 24 * 60 * 60 * 1000
  }
  if (filter.preset === "this-month") {
    const now = new Date(nowMs)
    const monthStart = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1)
    return archivedMs >= monthStart
  }
  if (filter.preset === "previous-month") {
    const now = new Date(nowMs)
    const thisMonthStart = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1)
    const previousMonthStart = Date.UTC(
      now.getUTCFullYear(),
      now.getUTCMonth() - 1,
      1
    )
    return archivedMs >= previousMonthStart && archivedMs < thisMonthStart
  }
  if (filter.preset === "custom") {
    if (filter.dateFrom == null || filter.dateTo == null) {
      return true
    }
    const fromMs = new Date(`${filter.dateFrom}T00:00:00.000Z`).getTime()
    const toMs = new Date(`${filter.dateTo}T23:59:59.999Z`).getTime()
    return archivedMs >= fromMs && archivedMs <= toMs
  }
  return true
}

function matchesSearch(
  fact: CaptureArchivedPlacementItem,
  searchQuery: string
): boolean {
  const q = searchQuery.trim().toLowerCase()
  if (q === "") {
    return true
  }
  const haystack = [
    placementLabel(fact),
    fact.locationName,
    fact.linkName ?? "",
    fact.archivedByDisplayName ?? "",
    QR_TYPE_LABELS[fact.qrType],
  ]
    .join(" ")
    .toLowerCase()
  return haystack.includes(q)
}

function sortRows(
  rows: OperatorCaptureArchiveRow[],
  factsById: Map<number, CaptureArchivedPlacementItem>,
  sort: CaptureArchiveSortId
): OperatorCaptureArchiveRow[] {
  const sorted = [...rows]
  sorted.sort((a, b) => {
    const fa = factsById.get(a.qrCodeId)
    const fb = factsById.get(b.qrCodeId)
    if (fa == null || fb == null) {
      return 0
    }
    switch (sort) {
      case "oldest-archived": {
        const ta = fa.archivedAt ? new Date(fa.archivedAt).getTime() : 0
        const tb = fb.archivedAt ? new Date(fb.archivedAt).getTime() : 0
        return ta - tb || a.qrCodeId - b.qrCodeId
      }
      case "highest-qr-scans":
        return fb.qrScans - fa.qrScans || a.placementLabel.localeCompare(b.placementLabel)
      case "highest-feedback":
        return (
          fb.feedbackSubmitted - fa.feedbackSubmitted
          || a.placementLabel.localeCompare(b.placementLabel)
        )
      case "most-recent-activity": {
        const ta = fa.lastScanAt ? new Date(fa.lastScanAt).getTime() : 0
        const tb = fb.lastScanAt ? new Date(fb.lastScanAt).getTime() : 0
        return tb - ta || a.placementLabel.localeCompare(b.placementLabel)
      }
      case "placement-name-az":
        return a.placementLabel.localeCompare(b.placementLabel) || a.qrCodeId - b.qrCodeId
      case "recently-archived":
      default: {
        const ta = fa.archivedAt ? new Date(fa.archivedAt).getTime() : 0
        const tb = fb.archivedAt ? new Date(fb.archivedAt).getTime() : 0
        return tb - ta || b.qrCodeId - a.qrCodeId
      }
    }
  })
  return sorted
}

export type BuildCaptureArchiveListInput = {
  facts: readonly CaptureArchivedPlacementItem[]
  searchQuery: string
  filters: CaptureArchiveFilters
  sort: CaptureArchiveSortId
  nowMs?: number
  showLocationFilter?: boolean
}

/** Build Archive table rows from account-wide archived facts + filters/sort. */
export function buildCaptureArchiveList(
  input: BuildCaptureArchiveListInput
): CaptureArchiveListResult {
  const nowMs = input.nowMs ?? Date.now()
  const isTrueEmpty = input.facts.length === 0
  const activeFilterCount = countActiveArchiveFilters(input.filters, {
    showLocationFilter: input.showLocationFilter,
  })

  const filteredFacts = input.facts.filter((fact) => {
    if (!matchesSearch(fact, input.searchQuery)) {
      return false
    }
    if (
      input.filters.locationIds.length > 0
      && !input.filters.locationIds.includes(fact.locationId)
    ) {
      return false
    }
    if (
      input.filters.placementTypes.length > 0
      && !input.filters.placementTypes.includes(fact.qrType)
    ) {
      return false
    }
    if (
      !matchesArchivedDate(fact.archivedAt, input.filters.archivedDate, nowMs)
    ) {
      return false
    }
    if (
      input.filters.archivedByDisplayNames.length > 0
      && (fact.archivedByDisplayName == null
        || !input.filters.archivedByDisplayNames.includes(
          fact.archivedByDisplayName
        ))
    ) {
      return false
    }
    return true
  })

  const factsById = new Map(filteredFacts.map((f) => [f.qrCodeId, f]))

  const rows = filteredFacts.map((fact) => {
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

  const sortedRows = sortRows(rows, factsById, input.sort)

  return {
    rows: sortedRows,
    isTrueEmpty,
    isNoMatch: !isTrueEmpty && sortedRows.length === 0,
    activeFilterCount,
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
