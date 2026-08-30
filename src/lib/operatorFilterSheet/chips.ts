import { cloneSelection } from "@/lib/operatorFilterSheet/session"
import type {
  ChipLabelResolvers,
  DateFieldSchema,
  DateFilterValue,
  FieldSchema,
  FilterChip,
  FilterSheetSchema,
  FilterSheetSession,
  MultiSelectFieldSchema,
  OperatorFilterSelection,
} from "@/lib/operatorFilterSheet/types"

function optionLabel(
  field: MultiSelectFieldSchema,
  id: string,
  resolve?: (id: string) => string
): string {
  const found = field.options.find((option) => option.id === id)
  if (found != null) {
    return found.label
  }
  return resolve?.(id) ?? id
}

function dateChipLabel(
  field: DateFieldSchema,
  value: DateFilterValue
): string | null {
  if (value.kind === "none") {
    return null
  }
  const axisLabel =
    field.hasAxis && value.axis != null
      ? field.axisLabels?.[value.axis] ?? null
      : null

  if (value.kind === "custom") {
    const range = `${value.dateFrom}–${value.dateTo}`
    return axisLabel != null ? `${axisLabel} · ${range}` : range
  }

  const presetLabel = field.presetLabels[value.preset] ?? value.preset
  return axisLabel != null ? `${axisLabel} · ${presetLabel}` : presetLabel
}

/** FE-only chips, projected from field defs + optional resolvers for dynamic options. */
export function projectChips(
  schema: FilterSheetSchema,
  selection: OperatorFilterSelection,
  resolvers: ChipLabelResolvers = {}
): FilterChip[] {
  const chips: FilterChip[] = []

  for (const field of schema.fields) {
    const value = selection[field.id]
    if (value == null) {
      continue
    }

    if (field.kind === "multi-select" && value.kind === "multi-select") {
      for (const id of value.ids) {
        chips.push({
          id: `${field.chipKind}:${id}`,
          kind: field.chipKind,
          fieldId: field.id,
          label: optionLabel(field, id, resolvers[field.id]),
          value: id,
        })
      }
      continue
    }

    if (field.kind === "location-scope" && value.kind === "location-scope") {
      if (value.value.kind === "all") {
        chips.push({
          id: `${field.id}:all`,
          kind: "location-all",
          fieldId: field.id,
          label: "All permitted locations",
          value: "all",
        })
      } else if (value.value.kind === "individual") {
        const resolve = resolvers[field.id]
        for (const locationId of value.value.locationIds) {
          const found = field.locations.find((l) => l.id === locationId)
          chips.push({
            id: `${field.id}:${locationId}`,
            kind: "location-id",
            fieldId: field.id,
            label: found?.label ?? resolve?.(locationId) ?? locationId,
            value: locationId,
          })
        }
      }
      continue
    }

    if (field.kind === "date" && value.kind === "date") {
      const label = dateChipLabel(field, value.value)
      if (label != null) {
        chips.push({
          id: field.id,
          kind: "date",
          fieldId: field.id,
          label,
          value: field.id,
        })
      }
    }
  }

  return chips
}

export function chipCount(
  schema: FilterSheetSchema,
  selection: OperatorFilterSelection
): number {
  return projectChips(schema, selection).length
}

function findField(
  schema: FilterSheetSchema,
  fieldId: string
): FieldSchema | undefined {
  return schema.fields.find((field) => field.id === fieldId)
}

export function removeAppliedChip(
  schema: FilterSheetSchema,
  selection: OperatorFilterSelection,
  chip: FilterChip
): OperatorFilterSelection {
  const next = cloneSelection(selection)
  const field = findField(schema, chip.fieldId)
  const value = next[chip.fieldId]
  if (field == null || value == null) {
    return next
  }

  if (field.kind === "multi-select" && value.kind === "multi-select") {
    next[chip.fieldId] = {
      kind: "multi-select",
      ids: value.ids.filter((id) => id !== chip.value),
    }
    return next
  }

  if (field.kind === "location-scope" && value.kind === "location-scope") {
    if (chip.kind === "location-all") {
      next[chip.fieldId] = { kind: "location-scope", value: { kind: "none" } }
    } else if (chip.kind === "location-id" && value.value.kind === "individual") {
      const locationIds = value.value.locationIds.filter(
        (id) => id !== chip.value
      )
      next[chip.fieldId] = {
        kind: "location-scope",
        value:
          locationIds.length === 0
            ? { kind: "none" }
            : { kind: "individual", locationIds },
      }
    }
    return next
  }

  if (field.kind === "date") {
    next[chip.fieldId] = { kind: "date", value: { kind: "none" } }
  }

  return next
}

export function removePendingChip(
  schema: FilterSheetSchema,
  session: FilterSheetSession,
  chip: FilterChip
): FilterSheetSession {
  const pending = removeAppliedChip(schema, session.pending, chip)
  const isDateChip = chip.kind === "date"
  const isLocationChip = chip.kind === "location-all" || chip.kind === "location-id"
  return {
    ...session,
    pending,
    dateStep: isDateChip ? null : session.dateStep,
    dateDraftAxis: isDateChip ? null : session.dateDraftAxis,
    activeDateFieldId: isDateChip ? null : session.activeDateFieldId,
    locationStep: isLocationChip ? null : session.locationStep,
    activeLocationFieldId: isLocationChip
      ? null
      : session.activeLocationFieldId,
  }
}
