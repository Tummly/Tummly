import { describe, expect, it } from "vitest"

import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet/dateLabels"
import {
  applyPending,
  beginDatePick,
  beginLocationIndividual,
  beginLocationPick,
  clearAllPending,
  clearDate,
  clearLocation,
  clearLocationOverrideOnShellChange,
  closeDatePick,
  emptySelection,
  isApplyDirty,
  openSession,
  pickDateAxis,
  pickDatePreset,
  setLocationAll,
  toggleLocationId,
  toggleMultiSelect,
} from "@/lib/operatorFilterSheet/session"
import {
  chipCount,
  projectChips,
  removeAppliedChip,
  removePendingChip,
} from "@/lib/operatorFilterSheet/chips"
import type { FilterSheetSchema } from "@/lib/operatorFilterSheet/types"

const SCHEMA_WITH_AXIS: FilterSheetSchema = {
  fields: [
    {
      id: "marketing",
      kind: "multi-select",
      label: "Marketing status",
      chipKind: "marketing",
      options: [
        { id: "eligible", label: "Eligible to contact" },
        { id: "not-opted-in", label: "Not opted in" },
      ],
    },
    {
      id: "location",
      kind: "location-scope",
      label: "Location",
      locations: [
        { id: "1", label: "Camden" },
        { id: "2", label: "Soho" },
      ],
    },
    {
      id: "date",
      kind: "date",
      label: "Date",
      hasAxis: true,
      axisLabels: {
        "first-captured": "First captured",
        "last-interaction": "Last interaction",
      },
      presetLabels: DATE_PRESET_LABELS,
    },
    {
      id: "tag",
      kind: "multi-select",
      label: "Tags",
      chipKind: "tag",
      options: [],
    },
  ],
}

const SCHEMA_NO_AXIS: FilterSheetSchema = {
  fields: [
    {
      id: "activityType",
      kind: "multi-select",
      label: "Activity type",
      chipKind: "activity-type",
      options: [
        { id: "note", label: "Note" },
        { id: "tag", label: "Tag" },
      ],
    },
    {
      id: "date",
      kind: "date",
      label: "Date",
      hasAxis: false,
      presetLabels: DATE_PRESET_LABELS,
    },
  ],
}

describe("operatorFilterSheet session — open/clear/apply/dirty", () => {
  it("opens a session with applied cloned into pending and not dirty", () => {
    const applied = emptySelection(SCHEMA_WITH_AXIS)
    const session = openSession(applied)

    expect(session.applied).toEqual(applied)
    expect(session.pending).toEqual(applied)
    expect(session.applied).not.toBe(applied)
    expect(isApplyDirty(session)).toBe(false)
  })

  it("becomes dirty once pending diverges, and clean again once applied", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "marketing", "eligible")

    expect(isApplyDirty(session)).toBe(true)

    const applied = applyPending(session)
    session = openSession(applied)

    expect(isApplyDirty(session)).toBe(false)
    expect(applied.marketing).toEqual({ kind: "multi-select", ids: ["eligible"] })
  })

  it("clearAllPending resets pending to empty and clears step nav", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "marketing", "eligible")
    session = beginLocationPick(session, "location")

    session = clearAllPending(SCHEMA_WITH_AXIS, session)

    expect(session.pending).toEqual(emptySelection(SCHEMA_WITH_AXIS))
    expect(session.locationStep).toBeNull()
    expect(session.dateStep).toBeNull()
  })
})

describe("operatorFilterSheet session — multi-select toggle", () => {
  it("toggles an id in and back out of a multi-select field", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))

    session = toggleMultiSelect(session, "marketing", "eligible")
    expect(session.pending.marketing).toEqual({
      kind: "multi-select",
      ids: ["eligible"],
    })

    session = toggleMultiSelect(session, "marketing", "not-opted-in")
    expect(session.pending.marketing).toEqual({
      kind: "multi-select",
      ids: ["eligible", "not-opted-in"],
    })

    session = toggleMultiSelect(session, "marketing", "eligible")
    expect(session.pending.marketing).toEqual({
      kind: "multi-select",
      ids: ["not-opted-in"],
    })
  })

  it("does not mutate the applied selection", () => {
    const applied = emptySelection(SCHEMA_WITH_AXIS)
    let session = openSession(applied)
    session = toggleMultiSelect(session, "marketing", "eligible")

    expect(applied.marketing).toEqual({ kind: "multi-select", ids: [] })
    expect(session.applied.marketing).toEqual({ kind: "multi-select", ids: [] })
  })
})

describe("operatorFilterSheet session — date with and without axis", () => {
  it("requires an axis pick before a preset commits, for axis fields", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    const field = { id: "date", hasAxis: true }

    session = beginDatePick(session, field)
    expect(session.dateStep).toBe("axis")

    session = pickDateAxis(session, "date", "first-captured")
    expect(session.dateStep).toBe("preset")

    session = pickDatePreset(session, field, "last-7")
    expect(session.pending.date).toEqual({
      kind: "date",
      value: { kind: "preset", axis: "first-captured", preset: "last-7" },
    })
    expect(session.dateStep).toBeNull()
  })

  it("clears to none when a preset is picked without an axis on axis fields", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    const field = { id: "date", hasAxis: true }

    session = pickDatePreset(session, field, "last-7")

    expect(session.pending.date).toEqual({ kind: "date", value: { kind: "none" } })
  })

  it("skips the axis step entirely for fields without an axis", () => {
    let session = openSession(emptySelection(SCHEMA_NO_AXIS))
    const field = { id: "date", hasAxis: false }

    session = beginDatePick(session, field)
    expect(session.dateStep).toBe("preset")

    session = pickDatePreset(session, field, "today")
    expect(session.pending.date).toEqual({
      kind: "date",
      value: { kind: "preset", axis: undefined, preset: "today" },
    })
  })

  it("supports a custom range and clearing back to none", () => {
    let session = openSession(emptySelection(SCHEMA_NO_AXIS))
    const field = { id: "date", hasAxis: false }

    session = pickDatePreset(session, field, "custom", {
      dateFrom: "2026-07-01",
      dateTo: "2026-07-10",
    })
    expect(session.pending.date).toEqual({
      kind: "date",
      value: {
        kind: "custom",
        axis: undefined,
        dateFrom: "2026-07-01",
        dateTo: "2026-07-10",
      },
    })

    session = clearDate(session, "date")
    expect(session.pending.date).toEqual({ kind: "date", value: { kind: "none" } })
  })

  it("closeDatePick resets the step but keeps a committed axis", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    const field = { id: "date", hasAxis: true }

    session = beginDatePick(session, field)
    session = pickDateAxis(session, "date", "last-interaction")
    session = pickDatePreset(session, field, "today")

    session = beginDatePick(session, field)
    expect(session.dateStep).toBe("preset")
    session = closeDatePick(session, "date")

    expect(session.dateStep).toBeNull()
    expect(session.dateDraftAxis).toBe("last-interaction")
  })
})

describe("operatorFilterSheet session — location-scope steps", () => {
  it("sets All permitted locations and clears step nav", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = setLocationAll(session, "location")

    expect(session.pending.location).toEqual({
      kind: "location-scope",
      value: { kind: "all" },
    })
    expect(session.locationStep).toBeNull()
  })

  it("begins individual mode, toggles ids, and clears when the last id is removed", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = beginLocationIndividual(session, "location")
    expect(session.locationStep).toBe("individual")

    session = toggleLocationId(session, "location", "1")
    session = toggleLocationId(session, "location", "2")
    expect(session.pending.location).toEqual({
      kind: "location-scope",
      value: { kind: "individual", locationIds: ["1", "2"] },
    })

    session = toggleLocationId(session, "location", "1")
    session = toggleLocationId(session, "location", "2")
    expect(session.pending.location).toEqual({
      kind: "location-scope",
      value: { kind: "none" },
    })
  })

  it("toggling a location id while not in individual mode begins individual mode first", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = setLocationAll(session, "location")

    session = toggleLocationId(session, "location", "1")

    expect(session.pending.location).toEqual({
      kind: "location-scope",
      value: { kind: "individual", locationIds: [] },
    })
    expect(session.locationStep).toBe("individual")
  })

  it("beginLocationPick resumes on the individual step when already individual", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = beginLocationIndividual(session, "location")
    session = toggleLocationId(session, "location", "1")
    session = { ...session, locationStep: null }

    session = beginLocationPick(session, "location")

    expect(session.locationStep).toBe("individual")
  })

  it("clearLocation resets to none", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = setLocationAll(session, "location")
    session = clearLocation(session, "location")

    expect(session.pending.location).toEqual({
      kind: "location-scope",
      value: { kind: "none" },
    })
  })

  it("clearLocationOverrideOnShellChange only clears an active override", () => {
    const untouched = emptySelection(SCHEMA_WITH_AXIS)
    expect(clearLocationOverrideOnShellChange(untouched, "location")).toBe(
      untouched
    )

    const applied = applyPending(setLocationAll(openSession(untouched), "location"))
    const cleared = clearLocationOverrideOnShellChange(applied, "location")
    expect(cleared.location).toEqual({
      kind: "location-scope",
      value: { kind: "none" },
    })
  })
})

describe("operatorFilterSheet chips — project/remove (pending + applied)", () => {
  it("projects one chip per multi-select id and a compound date chip", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "marketing", "eligible")
    session = toggleMultiSelect(session, "marketing", "not-opted-in")
    const field = { id: "date", hasAxis: true }
    session = beginDatePick(session, field)
    session = pickDateAxis(session, "date", "first-captured")
    session = pickDatePreset(session, field, "last-7")

    const chips = projectChips(SCHEMA_WITH_AXIS, session.pending)

    expect(chips.map((c) => c.id)).toEqual([
      "marketing:eligible",
      "marketing:not-opted-in",
      "date",
    ])
    expect(chips.find((c) => c.id === "date")?.label).toBe(
      "First captured · Last 7 days"
    )
  })

  it("projects location-all and location-id chips with resolvers", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = beginLocationIndividual(session, "location")
    session = toggleLocationId(session, "location", "9")

    const chips = projectChips(SCHEMA_WITH_AXIS, session.pending, {
      location: (id) => `Location #${id}`,
    })

    expect(chips).toEqual([
      {
        id: "location:9",
        kind: "location-id",
        fieldId: "location",
        label: "Location #9",
        value: "9",
      },
    ])
  })

  it("falls back to a resolver for dynamic multi-select options like tags", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "tag", "abc")

    const chips = projectChips(SCHEMA_WITH_AXIS, session.pending, {
      tag: (id) => `Tag ${id}`,
    })

    expect(chips).toEqual([
      { id: "tag:abc", kind: "tag", fieldId: "tag", label: "Tag abc", value: "abc" },
    ])
  })

  it("chipCount matches projectChips length", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "marketing", "eligible")
    session = setLocationAll(session, "location")

    expect(chipCount(SCHEMA_WITH_AXIS, session.pending)).toBe(2)
  })

  it("removeAppliedChip removes from an applied selection by chip", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = toggleMultiSelect(session, "marketing", "eligible")
    session = toggleMultiSelect(session, "marketing", "not-opted-in")
    const applied = applyPending(session)

    const next = removeAppliedChip(SCHEMA_WITH_AXIS, applied, {
      id: "marketing:eligible",
      kind: "marketing",
      fieldId: "marketing",
      label: "Eligible to contact",
      value: "eligible",
    })

    expect(next.marketing).toEqual({
      kind: "multi-select",
      ids: ["not-opted-in"],
    })
    // Original untouched.
    expect(applied.marketing).toEqual({
      kind: "multi-select",
      ids: ["eligible", "not-opted-in"],
    })
  })

  it("removePendingChip removes from pending and resets date/location step nav", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    const field = { id: "date", hasAxis: true }
    session = beginDatePick(session, field)
    session = pickDateAxis(session, "date", "first-captured")
    session = pickDatePreset(session, field, "today")

    session = removePendingChip(SCHEMA_WITH_AXIS, session, {
      id: "date",
      kind: "date",
      fieldId: "date",
      label: "First captured · Today",
      value: "date",
    })

    expect(session.pending.date).toEqual({ kind: "date", value: { kind: "none" } })
    expect(session.dateStep).toBeNull()
    expect(session.dateDraftAxis).toBeNull()
  })

  it("removing the last location-id chip collapses the override to none", () => {
    let session = openSession(emptySelection(SCHEMA_WITH_AXIS))
    session = beginLocationIndividual(session, "location")
    session = toggleLocationId(session, "location", "9")
    const applied = applyPending(session)

    const next = removeAppliedChip(SCHEMA_WITH_AXIS, applied, {
      id: "location:9",
      kind: "location-id",
      fieldId: "location",
      label: "Location #9",
      value: "9",
    })

    expect(next.location).toEqual({
      kind: "location-scope",
      value: { kind: "none" },
    })
  })
})
