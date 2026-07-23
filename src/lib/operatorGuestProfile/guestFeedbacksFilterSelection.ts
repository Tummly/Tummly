/** Applied / pending Feedbacks tab Filters (Classification · Issue tags · Date). */

import type { DetectedTagKey } from "@/lib/operatorHome/detectedTags"
import { DETECTED_TAG_LABELS } from "@/lib/operatorHome/detectedTags"
import {
  DATE_PRESET_LABELS,
  SENTIMENT_LABELS,
  type DatePresetId,
  type SentimentOptionId,
} from "@/lib/operatorGuests/guestsFilterSelection"

export type FeedbacksDateFilter =
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

export type GuestFeedbacksFilterSelection = {
  sentiment: SentimentOptionId[]
  detectedTags: DetectedTagKey[]
  date: FeedbacksDateFilter
}

export type FeedbacksFilterChipKind = "sentiment" | "detected-tag" | "date"

export type FeedbacksFilterChip = {
  id: string
  kind: FeedbacksFilterChipKind
  label: string
  value: string
}

export type FeedbacksFiltersPanelSession = {
  applied: GuestFeedbacksFilterSelection
  pending: GuestFeedbacksFilterSelection
  dateStep: "preset" | null
}

export function emptyFeedbacksSelection(): GuestFeedbacksFilterSelection {
  return {
    sentiment: [],
    detectedTags: [],
    date: { kind: "none" },
  }
}

export function cloneFeedbacksSelection(
  selection: GuestFeedbacksFilterSelection
): GuestFeedbacksFilterSelection {
  return {
    sentiment: [...selection.sentiment],
    detectedTags: [...selection.detectedTags],
    date: { ...selection.date },
  }
}

export function feedbacksSelectionsEqual(
  a: GuestFeedbacksFilterSelection,
  b: GuestFeedbacksFilterSelection
): boolean {
  return JSON.stringify(a) === JSON.stringify(b)
}

export function isFeedbacksFiltersApplyDirty(
  session: FeedbacksFiltersPanelSession
): boolean {
  return !feedbacksSelectionsEqual(session.applied, session.pending)
}

export function openFeedbacksFiltersSession(
  applied: GuestFeedbacksFilterSelection
): FeedbacksFiltersPanelSession {
  return {
    applied: cloneFeedbacksSelection(applied),
    pending: cloneFeedbacksSelection(applied),
    dateStep: null,
  }
}

function toggleInList<T extends string>(list: T[], value: T): T[] {
  return list.includes(value)
    ? list.filter((item) => item !== value)
    : [...list, value]
}

export function toggleFeedbacksSentiment(
  session: FeedbacksFiltersPanelSession,
  id: SentimentOptionId
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      sentiment: toggleInList(session.pending.sentiment, id),
    },
  }
}

export function toggleFeedbacksDetectedTag(
  session: FeedbacksFiltersPanelSession,
  tag: DetectedTagKey
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      detectedTags: toggleInList(session.pending.detectedTags, tag),
    },
  }
}

export function beginFeedbacksDatePick(
  session: FeedbacksFiltersPanelSession
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    dateStep: "preset",
  }
}

export function pickFeedbacksDatePreset(
  session: FeedbacksFiltersPanelSession,
  preset: DatePresetId,
  customRange?: { dateFrom: string; dateTo: string }
): FeedbacksFiltersPanelSession {
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

export function clearFeedbacksDate(
  session: FeedbacksFiltersPanelSession
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    pending: {
      ...session.pending,
      date: { kind: "none" },
    },
  }
}

export function clearAllFeedbacksPending(
  session: FeedbacksFiltersPanelSession
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    pending: emptyFeedbacksSelection(),
  }
}

export function applyPendingFeedbacksFilters(
  session: FeedbacksFiltersPanelSession
): GuestFeedbacksFilterSelection {
  return cloneFeedbacksSelection(session.pending)
}

function dateChipLabel(date: FeedbacksDateFilter): string | null {
  if (date.kind === "none") {
    return null
  }
  if (date.kind === "custom") {
    return `${date.dateFrom}–${date.dateTo}`
  }
  return DATE_PRESET_LABELS[date.preset]
}

export function projectFeedbacksFilterChips(
  selection: GuestFeedbacksFilterSelection
): FeedbacksFilterChip[] {
  const chips: FeedbacksFilterChip[] = []

  for (const id of selection.sentiment) {
    chips.push({
      id: `sentiment:${id}`,
      kind: "sentiment",
      label: SENTIMENT_LABELS[id],
      value: id,
    })
  }

  for (const tag of selection.detectedTags) {
    chips.push({
      id: `detected-tag:${tag}`,
      kind: "detected-tag",
      label: DETECTED_TAG_LABELS[tag],
      value: tag,
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

export function feedbacksFilterChipCount(
  selection: GuestFeedbacksFilterSelection
): number {
  return projectFeedbacksFilterChips(selection).length
}

export function hasActiveFeedbacksQuery(
  searchQuery: string,
  filters: GuestFeedbacksFilterSelection
): boolean {
  if (searchQuery.trim().length > 0) {
    return true
  }
  return feedbacksFilterChipCount(filters) > 0
}

export function removeFeedbacksFilterChip(
  selection: GuestFeedbacksFilterSelection,
  chip: FeedbacksFilterChip
): GuestFeedbacksFilterSelection {
  if (chip.kind === "sentiment") {
    return {
      ...selection,
      sentiment: selection.sentiment.filter((id) => id !== chip.value),
    }
  }
  if (chip.kind === "detected-tag") {
    return {
      ...selection,
      detectedTags: selection.detectedTags.filter(
        (tag) => tag !== chip.value
      ) as DetectedTagKey[],
    }
  }
  return {
    ...selection,
    date: { kind: "none" },
  }
}

export function removePendingFeedbacksFilterChip(
  session: FeedbacksFiltersPanelSession,
  chip: FeedbacksFilterChip
): FeedbacksFiltersPanelSession {
  return {
    ...session,
    pending: removeFeedbacksFilterChip(session.pending, chip),
  }
}

export const FEEDBACKS_DETECTED_TAG_OPTIONS = Object.entries(
  DETECTED_TAG_LABELS
) as Array<[DetectedTagKey, string]>

export { SENTIMENT_LABELS, DATE_PRESET_LABELS }
