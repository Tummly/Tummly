import type {
  DateAxisId,
  DateFilterValue,
  DatePresetId,
  FieldSchema,
  FieldValue,
  FilterSheetSchema,
  FilterSheetSession,
  OperatorFilterSelection,
} from "@/lib/operatorFilterSheet/types"

function emptyFieldValue(field: FieldSchema): FieldValue {
  switch (field.kind) {
    case "multi-select":
      return { kind: "multi-select", ids: [] }
    case "date":
      return { kind: "date", value: { kind: "none" } }
    case "location-scope":
      return { kind: "location-scope", value: { kind: "none" } }
  }
}

export function emptySelection(
  schema: FilterSheetSchema
): OperatorFilterSelection {
  const selection: OperatorFilterSelection = {}
  for (const field of schema.fields) {
    selection[field.id] = emptyFieldValue(field)
  }
  return selection
}

function cloneFieldValue(value: FieldValue): FieldValue {
  switch (value.kind) {
    case "multi-select":
      return { kind: "multi-select", ids: [...value.ids] }
    case "date":
      return { kind: "date", value: { ...value.value } }
    case "location-scope":
      return {
        kind: "location-scope",
        value:
          value.value.kind === "individual"
            ? { kind: "individual", locationIds: [...value.value.locationIds] }
            : { ...value.value },
      }
  }
}

export function cloneSelection(
  selection: OperatorFilterSelection
): OperatorFilterSelection {
  const next: OperatorFilterSelection = {}
  for (const [fieldId, value] of Object.entries(selection)) {
    next[fieldId] = cloneFieldValue(value)
  }
  return next
}

export function selectionsEqual(
  a: OperatorFilterSelection,
  b: OperatorFilterSelection
): boolean {
  return JSON.stringify(a) === JSON.stringify(b)
}

export function openSession(
  applied: OperatorFilterSelection
): FilterSheetSession {
  return {
    applied: cloneSelection(applied),
    pending: cloneSelection(applied),
    dateStep: null,
    dateDraftAxis: null,
    locationStep: null,
    activeDateFieldId: null,
    activeLocationFieldId: null,
  }
}

export function isApplyDirty(session: FilterSheetSession): boolean {
  return !selectionsEqual(session.applied, session.pending)
}

export function applyPending(
  session: FilterSheetSession
): OperatorFilterSelection {
  return cloneSelection(session.pending)
}

/** Commit pending → applied and clear step nav; keeps the sheet open. */
export function commitPending(
  session: FilterSheetSession
): FilterSheetSession {
  const applied = cloneSelection(session.pending)
  return {
    applied,
    pending: cloneSelection(applied),
    dateStep: null,
    dateDraftAxis: null,
    locationStep: null,
    activeDateFieldId: null,
    activeLocationFieldId: null,
  }
}

export function clearAllPending(
  schema: FilterSheetSchema,
  session: FilterSheetSession
): FilterSheetSession {
  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    locationStep: null,
    activeDateFieldId: null,
    activeLocationFieldId: null,
    pending: emptySelection(schema),
  }
}

function toggleInList<T extends string>(list: readonly T[], value: T): T[] {
  return list.includes(value)
    ? list.filter((item) => item !== value)
    : [...list, value]
}

export function toggleMultiSelect(
  session: FilterSheetSession,
  fieldId: string,
  id: string
): FilterSheetSession {
  const current = session.pending[fieldId]
  const ids = current?.kind === "multi-select" ? current.ids : []
  return {
    ...session,
    pending: {
      ...session.pending,
      [fieldId]: { kind: "multi-select", ids: toggleInList(ids, id) },
    },
  }
}

/** Shell picker change (e.g. location switch) while Location override active → clear it. */
export function clearLocationOverrideOnShellChange(
  selection: OperatorFilterSelection,
  fieldId: string
): OperatorFilterSelection {
  const current = selection[fieldId]
  if (current?.kind !== "location-scope" || current.value.kind === "none") {
    return selection
  }
  return {
    ...selection,
    [fieldId]: { kind: "location-scope", value: { kind: "none" } },
  }
}

export function beginLocationPick(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  const current = session.pending[fieldId]
  const isIndividual =
    current?.kind === "location-scope" && current.value.kind === "individual"
  return {
    ...session,
    locationStep: isIndividual ? "individual" : "mode",
    activeLocationFieldId: fieldId,
  }
}

export function backToLocationMode(
  session: FilterSheetSession
): FilterSheetSession {
  return { ...session, locationStep: "mode" }
}

export function setLocationAll(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  return {
    ...session,
    locationStep: null,
    activeLocationFieldId: null,
    pending: {
      ...session.pending,
      [fieldId]: { kind: "location-scope", value: { kind: "all" } },
    },
  }
}

export function beginLocationIndividual(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  const current = session.pending[fieldId]
  const existing =
    current?.kind === "location-scope" && current.value.kind === "individual"
      ? current.value.locationIds
      : []
  return {
    ...session,
    locationStep: "individual",
    activeLocationFieldId: fieldId,
    pending: {
      ...session.pending,
      [fieldId]: {
        kind: "location-scope",
        value: { kind: "individual", locationIds: [...existing] },
      },
    },
  }
}

export function clearLocation(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  return {
    ...session,
    locationStep: null,
    activeLocationFieldId: null,
    pending: {
      ...session.pending,
      [fieldId]: { kind: "location-scope", value: { kind: "none" } },
    },
  }
}

export function toggleLocationId(
  session: FilterSheetSession,
  fieldId: string,
  locationId: string
): FilterSheetSession {
  const current = session.pending[fieldId]
  if (current?.kind !== "location-scope" || current.value.kind !== "individual") {
    return beginLocationIndividual(session, fieldId)
  }
  const nextIds = toggleInList(current.value.locationIds, locationId)
  if (nextIds.length === 0) {
    return clearLocation(session, fieldId)
  }
  return {
    ...session,
    pending: {
      ...session.pending,
      [fieldId]: {
        kind: "location-scope",
        value: { kind: "individual", locationIds: nextIds },
      },
    },
  }
}

type DateFieldRef = { id: string; hasAxis: boolean }

function currentDateValue(
  session: FilterSheetSession,
  fieldId: string
): DateFilterValue {
  const current = session.pending[fieldId]
  return current?.kind === "date" ? current.value : { kind: "none" }
}

export function beginDatePick(
  session: FilterSheetSession,
  field: DateFieldRef
): FilterSheetSession {
  if (!field.hasAxis) {
    return {
      ...session,
      dateStep: "preset",
      dateDraftAxis: null,
      activeDateFieldId: field.id,
    }
  }
  const value = currentDateValue(session, field.id)
  const draftAxis = value.kind === "none" ? null : value.axis ?? null
  return {
    ...session,
    dateStep: draftAxis == null ? "axis" : "preset",
    dateDraftAxis: draftAxis,
    activeDateFieldId: field.id,
  }
}

export function closeDatePick(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  const value = currentDateValue(session, fieldId)
  const keepAxis = value.kind !== "none"
  return {
    ...session,
    dateStep: null,
    dateDraftAxis: keepAxis ? session.dateDraftAxis : null,
    activeDateFieldId: null,
  }
}

export function changeDateAxis(
  session: FilterSheetSession
): FilterSheetSession {
  return { ...session, dateStep: "axis", dateDraftAxis: null }
}

export function pickDateAxis(
  session: FilterSheetSession,
  fieldId: string,
  axis: DateAxisId
): FilterSheetSession {
  return {
    ...session,
    dateStep: "preset",
    dateDraftAxis: axis,
    activeDateFieldId: fieldId,
  }
}

export function pickDatePreset(
  session: FilterSheetSession,
  field: DateFieldRef,
  preset: DatePresetId,
  customRange?: { dateFrom: string; dateTo: string }
): FilterSheetSession {
  const value = currentDateValue(session, field.id)
  const axis =
    session.dateDraftAxis ?? (value.kind === "none" ? null : value.axis ?? null)

  if (preset === "any-time" || (field.hasAxis && axis == null)) {
    return {
      ...session,
      dateStep: null,
      dateDraftAxis: null,
      activeDateFieldId: null,
      pending: {
        ...session.pending,
        [field.id]: { kind: "date", value: { kind: "none" } },
      },
    }
  }

  if (preset === "custom") {
    const range = customRange ?? {
      dateFrom: "2026-07-01",
      dateTo: "2026-07-15",
    }
    return {
      ...session,
      dateStep: null,
      dateDraftAxis: null,
      activeDateFieldId: null,
      pending: {
        ...session.pending,
        [field.id]: {
          kind: "date",
          value: {
            kind: "custom",
            axis: axis ?? undefined,
            dateFrom: range.dateFrom,
            dateTo: range.dateTo,
          },
        },
      },
    }
  }

  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    activeDateFieldId: null,
    pending: {
      ...session.pending,
      [field.id]: {
        kind: "date",
        value: { kind: "preset", axis: axis ?? undefined, preset },
      },
    },
  }
}

export function clearDate(
  session: FilterSheetSession,
  fieldId: string
): FilterSheetSession {
  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    activeDateFieldId: null,
    pending: {
      ...session.pending,
      [fieldId]: { kind: "date", value: { kind: "none" } },
    },
  }
}
