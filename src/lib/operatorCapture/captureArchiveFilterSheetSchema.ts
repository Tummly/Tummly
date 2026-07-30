import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type {
  FilterSheetSchema,
  SchemaOption,
} from "@/lib/operatorFilterSheet"
import { CAPTURE_ARCHIVE_PLACEMENT_TYPE_OPTIONS } from "@/lib/operatorCapture/buildCaptureArchive"

/** Archive Filters — Location (multi only), Placement type, Archived date, Archived by. */
export function captureArchiveFilterSheetSchema(catalog: {
  locations?: readonly SchemaOption[]
  archivers?: readonly SchemaOption[]
  showLocationFilter?: boolean
}): FilterSheetSchema {
  const fields: FilterSheetSchema["fields"] = []

  if (catalog.showLocationFilter) {
    fields.push({
      id: "location",
      kind: "location-scope",
      label: "Location",
      locations: catalog.locations ?? [],
    })
  }

  fields.push(
    {
      id: "placementType",
      kind: "multi-select",
      label: "Placement type",
      chipKind: "placementType",
      options: CAPTURE_ARCHIVE_PLACEMENT_TYPE_OPTIONS.map((option) => ({
        id: option.id,
        label: option.label,
      })),
    },
    {
      id: "archivedDate",
      kind: "date",
      label: "Archived date",
      hasAxis: false,
      presetLabels: DATE_PRESET_LABELS,
    },
    {
      id: "archivedBy",
      kind: "multi-select",
      label: "Archived by",
      chipKind: "archivedBy",
      options: catalog.archivers ?? [],
    }
  )

  return { fields }
}
