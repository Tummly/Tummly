import {
  emptySelection,
  getDateValue,
  getLocationOverride,
  getMultiSelectIds,
  type DateFilterValue,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  type CaptureArchiveDatePresetId,
  type CaptureArchiveFilters,
} from "@/lib/operatorCapture/buildCaptureArchive"
import type { CapturePlacementQrType } from "@/types/dashboard"
import { captureArchiveFilterSheetSchema } from "@/lib/operatorCapture/captureArchiveFilterSheetSchema"

function dateValueToArchive(
  date: DateFilterValue
): CaptureArchiveFilters["archivedDate"] {
  if (date.kind === "none") {
    return { preset: "any-time" }
  }
  if (date.kind === "custom") {
    return {
      preset: "custom",
      dateFrom: date.dateFrom,
      dateTo: date.dateTo,
    }
  }
  return { preset: date.preset as CaptureArchiveDatePresetId }
}

function archiveDateToValue(
  archivedDate: CaptureArchiveFilters["archivedDate"]
): DateFilterValue {
  if (archivedDate.preset === "any-time") {
    return { kind: "none" }
  }
  if (archivedDate.preset === "custom") {
    return {
      kind: "custom",
      dateFrom: archivedDate.dateFrom ?? "",
      dateTo: archivedDate.dateTo ?? "",
    }
  }
  return { kind: "preset", preset: archivedDate.preset }
}

/** Map Filter Sheet selection → Archive list filters. */
export function archiveFiltersFromSelection(
  selection: OperatorFilterSelection,
  options?: { showLocationFilter?: boolean }
): CaptureArchiveFilters {
  const showLocationFilter = options?.showLocationFilter ?? true
  const location = showLocationFilter
    ? getLocationOverride(selection, "location")
    : { kind: "none" as const }
  const locationIds =
    location.kind === "individual"
      ? location.locationIds
          .map((id) => Number(id))
          .filter((id) => Number.isFinite(id))
      : []

  const placementTypes = getMultiSelectIds(
    selection,
    "placementType"
  ) as CapturePlacementQrType[]

  return {
    locationIds,
    placementTypes,
    archivedDate: dateValueToArchive(getDateValue(selection, "archivedDate")),
    archivedByDisplayNames: getMultiSelectIds(selection, "archivedBy"),
  }
}

/** Map Archive list filters → Filter Sheet selection (for openSession). */
export function selectionFromArchiveFilters(
  filters: CaptureArchiveFilters,
  options: {
    showLocationFilter: boolean
    locations: readonly { id: number; label: string }[]
    archivers: readonly string[]
  }
): OperatorFilterSelection {
  const schema = captureArchiveFilterSheetSchema({
    showLocationFilter: options.showLocationFilter,
    locations: options.locations.map((location) => ({
      id: String(location.id),
      label: location.label,
    })),
    archivers: options.archivers.map((name) => ({ id: name, label: name })),
  })
  const selection = emptySelection(schema)

  if (options.showLocationFilter) {
    selection.location =
      filters.locationIds.length === 0
        ? { kind: "location-scope", value: { kind: "none" } }
        : {
            kind: "location-scope",
            value: {
              kind: "individual",
              locationIds: filters.locationIds.map(String),
            },
          }
  }

  selection.placementType = {
    kind: "multi-select",
    ids: [...filters.placementTypes],
  }
  selection.archivedDate = {
    kind: "date",
    value: archiveDateToValue(filters.archivedDate),
  }
  selection.archivedBy = {
    kind: "multi-select",
    ids: [...filters.archivedByDisplayNames],
  }

  return selection
}

export function emptyArchiveFilterSelection(options: {
  showLocationFilter: boolean
  locations: readonly { id: number; label: string }[]
  archivers: readonly string[]
}): OperatorFilterSelection {
  return selectionFromArchiveFilters(DEFAULT_CAPTURE_ARCHIVE_FILTERS, options)
}
