/** Applied / pending Activity tab Filters (Activity type · Date on OccurredAt). */

import {
  DATE_PRESET_LABELS,
  type DatePresetId,
} from "@/lib/operatorGuests/guestsFilterSelection"
import { OPERATOR_GUEST_ACTIVITY_TYPE_LABELS } from "@/lib/operatorGuestProfile/guestProfilePresentation"

export type ActivityTypeId = keyof typeof OPERATOR_GUEST_ACTIVITY_TYPE_LABELS

export type ActivityDateFilter =
  | { kind: "none" }
  | {
      kind: "preset"
      preset: Exclude<DatePresetId, "any-time" | "custom">
    }
  | {
      kind: "custom"
      dateFrom: string
      dateTo: string
    }

export type GuestActivityFilterSelection = {
  activityTypes: ActivityTypeId[]
  date: ActivityDateFilter
}

export type ActivityFilterChipKind = "activity-type" | "date"

export type ActivityFilterChip = {
  id: string
  kind: ActivityFilterChipKind
  label: string
  value: string
}

export type ActivityFiltersPanelSession = {
  applied: GuestActivityFilterSelection
  pending: GuestActivityFilterSelection
  dateStep: "preset" | null
}

export function emptyActivitySelection(): GuestActivityFilterSelection {
  return {
    activityTypes: [],
    date: { kind: "none" },
  }
}

export function cloneActivitySelection(
  selection: GuestActivityFilterSelection
): GuestActivityFilterSelection {
  return {
    activityTypes: [...selection.activityTypes],
    date: { ...selection.date },
  }
}

export function activitySelectionsEqual(
  a: GuestActivityFilterSelection,
  b: GuestActivityFilterSelection
): boolean {
  return JSON.stringify(a) === JSON.stringify(b)
}

export function isActivityFiltersApplyDirty(
  session: ActivityFiltersPanelSession
): boolean {
  return !activitySelectionsEqual(session.applied, session.pending)
}

export function openActivityFiltersSession(
  applied: GuestActivityFilterSelection
): ActivityFiltersPanelSession {
  return {
    applied: cloneActivitySelection(applied),
    pending: cloneActivitySelection(applied),
    dateStep: null,
  }
}

function toggleInList<T extends string>(list: T[], value: T): T[] {
  return list.includes(value)
    ? list.filter((item) => item !== value)
    : [...list, value]
}

export function toggleActivityType(
  session: ActivityFiltersPanelSession,
  id: ActivityTypeId
): ActivityFiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      activityTypes: toggleInList(session.pending.activityTypes, id),
    },
  }
}

export function beginActivityDatePick(
  session: ActivityFiltersPanelSession
): ActivityFiltersPanelSession {
  return {
    ...session,
    dateStep: "preset",
  }
}

export function pickActivityDatePreset(
  session: ActivityFiltersPanelSession,
  preset: DatePresetId,
  customRange?: { dateFrom: string; dateTo: string }
): ActivityFiltersPanelSession {
  if (preset === "any-time") {
    return {
      ...session,
      dateStep: null,
      pending: {
        ...session.pending,
        date: { kind: "none" },
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
      pending: {
        ...session.pending,
        date: {
          kind: "custom",
          dateFrom: range.dateFrom,
          dateTo: range.dateTo,
        },
      },
    }
  }

  return {
    ...session,
    dateStep: null,
    pending: {
      ...session.pending,
      date: { kind: "preset", preset },
    },
  }
}

export function clearActivityDate(
  session: ActivityFiltersPanelSession
): ActivityFiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    pending: {
      ...session.pending,
      date: { kind: "none" },
    },
  }
}

export function clearAllActivityPending(
  session: ActivityFiltersPanelSession
): ActivityFiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    pending: emptyActivitySelection(),
  }
}

export function applyPendingActivityFilters(
  session: ActivityFiltersPanelSession
): GuestActivityFilterSelection {
  return cloneActivitySelection(session.pending)
}

function dateChipLabel(date: ActivityDateFilter): string | null {
  if (date.kind === "none") {
    return null
  }
  if (date.kind === "custom") {
    return `${date.dateFrom}–${date.dateTo}`
  }
  return DATE_PRESET_LABELS[date.preset]
}

export function projectActivityFilterChips(
  selection: GuestActivityFilterSelection
): ActivityFilterChip[] {
  const chips: ActivityFilterChip[] = []

  for (const id of selection.activityTypes) {
    chips.push({
      id: `activity-type:${id}`,
      kind: "activity-type",
      label: OPERATOR_GUEST_ACTIVITY_TYPE_LABELS[id],
      value: id,
    })
  }

  const dateLabel = dateChipLabel(selection.date)
  if (dateLabel != null) {
    chips.push({
      id: "date",
      kind: "date",
      label: dateLabel,
      value: "date",
    })
  }

  return chips
}

export function activityFilterChipCount(
  selection: GuestActivityFilterSelection
): number {
  return projectActivityFilterChips(selection).length
}

export function hasActiveActivityFilters(
  filters: GuestActivityFilterSelection
): boolean {
  return activityFilterChipCount(filters) > 0
}

export function removeActivityFilterChip(
  selection: GuestActivityFilterSelection,
  chip: ActivityFilterChip
): GuestActivityFilterSelection {
  if (chip.kind === "activity-type") {
    return {
      ...selection,
      activityTypes: selection.activityTypes.filter(
        (id) => id !== chip.value
      ) as ActivityTypeId[],
    }
  }
  return {
    ...selection,
    date: { kind: "none" },
  }
}

export function removePendingActivityFilterChip(
  session: ActivityFiltersPanelSession,
  chip: ActivityFilterChip
): ActivityFiltersPanelSession {
  return {
    ...session,
    pending: removeActivityFilterChip(session.pending, chip),
  }
}

export const ACTIVITY_TYPE_OPTIONS = Object.entries(
  OPERATOR_GUEST_ACTIVITY_TYPE_LABELS
) as Array<[ActivityTypeId, string]>

export { DATE_PRESET_LABELS }
