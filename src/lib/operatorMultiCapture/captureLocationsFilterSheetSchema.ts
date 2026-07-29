import type {
  FilterSheetSchema,
  SchemaOption,
} from "@/lib/operatorFilterSheet"

export type CaptureLocationStatusOptionId = "Active" | "Paused"

export const CAPTURE_LOCATION_STATUS_LABELS: Record<
  CaptureLocationStatusOptionId,
  string
> = {
  Active: "Active",
  Paused: "Paused",
}

/** Location performance Filters — Location multi-select + Status only. */
export function captureLocationsFilterSheetSchema(
  catalog: {
    locations?: readonly SchemaOption[]
  } = {}
): FilterSheetSchema {
  return {
    fields: [
      {
        id: "location",
        kind: "location-scope",
        label: "Location",
        locations: catalog.locations ?? [],
      },
      {
        id: "status",
        kind: "multi-select",
        label: "Status",
        chipKind: "status",
        options: Object.entries(CAPTURE_LOCATION_STATUS_LABELS).map(
          ([id, label]) => ({ id, label })
        ),
      },
    ],
  }
}
