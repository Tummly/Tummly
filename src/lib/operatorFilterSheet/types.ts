/** Operator Filter Sheet kernel types — session owns applied/pending, dirty Apply,
 * chips, and date/location step nav. Surfaces (Guests, Activity, Feedbacks) supply a
 * `FilterSheetSchema` describing their fields; the kernel stays domain-agnostic.
 */

export type DateAxisId = "first-captured" | "last-interaction"

export type DatePresetId =
  | "any-time"
  | "today"
  | "last-7"
  | "last-30"
  | "this-month"
  | "previous-month"
  | "custom"

export type DateFilterValue =
  | { kind: "none" }
  | {
      kind: "preset"
      axis?: DateAxisId
      preset: Exclude<DatePresetId, "any-time" | "custom">
    }
  | {
      kind: "custom"
      axis?: DateAxisId
      dateFrom: string
      dateTo: string
    }

export type LocationOverride =
  | { kind: "none" }
  | { kind: "all" }
  | { kind: "individual"; locationIds: string[] }

export type FieldValue =
  | { kind: "multi-select"; ids: string[] }
  | { kind: "date"; value: DateFilterValue }
  | { kind: "location-scope"; value: LocationOverride }

export type OperatorFilterSelection = Record<string, FieldValue>

export type FilterChip = {
  id: string
  /** Stable chip kind used by removeAppliedChip/removePendingChip switches. */
  kind: string
  fieldId: string
  label: string
  value: string
}

export type FilterSheetSession = {
  applied: OperatorFilterSelection
  pending: OperatorFilterSelection
  dateStep: "axis" | "preset" | null
  dateDraftAxis: DateAxisId | null
  locationStep: "mode" | "individual" | null
  /** Which date field id is being edited when dateStep != null. */
  activeDateFieldId: string | null
  activeLocationFieldId: string | null
}

export type SchemaOption = {
  id: string
  label: string
}

export type MultiSelectFieldSchema = {
  id: string
  kind: "multi-select"
  label: string
  /** Chip id/kind prefix, e.g. "marketing" → chip id `marketing:eligible`. */
  chipKind: string
  options: readonly SchemaOption[]
}

export type DateFieldSchema = {
  id: string
  kind: "date"
  label: string
  hasAxis: boolean
  axisLabels?: Record<DateAxisId, string>
  presetLabels: Record<Exclude<DatePresetId, "any-time" | "custom">, string>
}

export type LocationScopeFieldSchema = {
  id: string
  kind: "location-scope"
  label: string
  locations: readonly SchemaOption[]
}

export type FieldSchema =
  | MultiSelectFieldSchema
  | DateFieldSchema
  | LocationScopeFieldSchema

export type FilterSheetSchema = {
  fields: readonly FieldSchema[]
}

/** Per-field label resolvers for dynamic options (guest tags, locations) not baked into schema.options. */
export type ChipLabelResolvers = Partial<Record<string, (id: string) => string>>
