/** Applied / pending Guests Filters selection + chip projection (enabled fields only). */

export type MarketingOptionId = "eligible" | "not-opted-in"
export type ContactOptionId = "email" | "mobile"
export type SentimentOptionId = "positive" | "neutral" | "negative"
export type DateAxisId = "first-captured" | "last-interaction"
export type DatePresetId =
  | "any-time"
  | "today"
  | "last-7"
  | "last-30"
  | "this-month"
  | "previous-month"
  | "custom"

export type LocationOverride =
  | { kind: "none" }
  | { kind: "all" }
  | { kind: "individual"; locationIds: string[] }

export type DateFilter =
  | { kind: "none" }
  | {
      kind: "preset"
      axis: DateAxisId
      preset: Exclude<DatePresetId, "any-time" | "custom">
    }
  | {
      kind: "custom"
      axis: DateAxisId
      dateFrom: string
      dateTo: string
    }

export type GuestsFilterSelection = {
  marketing: MarketingOptionId[]
  contact: ContactOptionId[]
  sentiment: SentimentOptionId[]
  location: LocationOverride
  date: DateFilter
  tagIds: string[]
}

export type FilterChipKind =
  | "marketing"
  | "contact"
  | "sentiment"
  | "location-all"
  | "location-id"
  | "date"
  | "tag"

export type FilterChip = {
  id: string
  kind: FilterChipKind
  label: string
  value: string
}

export type FiltersPanelSession = {
  applied: GuestsFilterSelection
  pending: GuestsFilterSelection
  dateStep: "axis" | "preset" | null
  dateDraftAxis: DateAxisId | null
  locationStep: "mode" | "individual" | null
}

export function emptySelection(): GuestsFilterSelection {
  return {
    marketing: [],
    contact: [],
    sentiment: [],
    location: { kind: "none" },
    date: { kind: "none" },
    tagIds: [],
  }
}

export function cloneSelection(
  selection: GuestsFilterSelection
): GuestsFilterSelection {
  return {
    marketing: [...selection.marketing],
    contact: [...selection.contact],
    sentiment: [...selection.sentiment],
    location:
      selection.location.kind === "individual"
        ? {
            kind: "individual",
            locationIds: [...selection.location.locationIds],
          }
        : { ...selection.location },
    date: { ...selection.date },
    tagIds: [...selection.tagIds],
  }
}

export function selectionsEqual(
  a: GuestsFilterSelection,
  b: GuestsFilterSelection
): boolean {
  return JSON.stringify(a) === JSON.stringify(b)
}

export function isFiltersApplyDirty(session: FiltersPanelSession): boolean {
  return !selectionsEqual(session.applied, session.pending)
}

export function openFiltersSession(
  applied: GuestsFilterSelection
): FiltersPanelSession {
  return {
    applied: cloneSelection(applied),
    pending: cloneSelection(applied),
    dateStep: null,
    dateDraftAxis: null,
    locationStep: null,
  }
}

function toggleInList<T extends string>(list: T[], value: T): T[] {
  return list.includes(value)
    ? list.filter((item) => item !== value)
    : [...list, value]
}

export function toggleMarketing(
  session: FiltersPanelSession,
  id: MarketingOptionId
): FiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      marketing: toggleInList(session.pending.marketing, id),
    },
  }
}

export function toggleContact(
  session: FiltersPanelSession,
  id: ContactOptionId
): FiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      contact: toggleInList(session.pending.contact, id),
    },
  }
}

export function toggleSentiment(
  session: FiltersPanelSession,
  id: SentimentOptionId
): FiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      sentiment: toggleInList(session.pending.sentiment, id),
    },
  }
}

export function toggleTag(
  session: FiltersPanelSession,
  tagId: string
): FiltersPanelSession {
  return {
    ...session,
    pending: {
      ...session.pending,
      tagIds: toggleInList(session.pending.tagIds, tagId),
    },
  }
}

export function setLocationAll(
  session: FiltersPanelSession
): FiltersPanelSession {
  return {
    ...session,
    locationStep: null,
    pending: {
      ...session.pending,
      location: { kind: "all" },
    },
  }
}

export function beginLocationIndividual(
  session: FiltersPanelSession
): FiltersPanelSession {
  const existing =
    session.pending.location.kind === "individual"
      ? session.pending.location.locationIds
      : []
  return {
    ...session,
    locationStep: "individual",
    pending: {
      ...session.pending,
      location: { kind: "individual", locationIds: [...existing] },
    },
  }
}

export function clearLocation(
  session: FiltersPanelSession
): FiltersPanelSession {
  return {
    ...session,
    locationStep: null,
    pending: {
      ...session.pending,
      location: { kind: "none" },
    },
  }
}

export function toggleLocationId(
  session: FiltersPanelSession,
  locationId: string
): FiltersPanelSession {
  if (session.pending.location.kind !== "individual") {
    return beginLocationIndividual(session)
  }
  const nextIds = toggleInList(session.pending.location.locationIds, locationId)
  if (nextIds.length === 0) {
    return clearLocation(session)
  }
  return {
    ...session,
    pending: {
      ...session.pending,
      location: { kind: "individual", locationIds: nextIds },
    },
  }
}

/** Shell picker change while Location override active → clear Location. */
export function clearLocationOverrideOnShellChange(
  applied: GuestsFilterSelection
): GuestsFilterSelection {
  if (applied.location.kind === "none") {
    return applied
  }
  return {
    ...applied,
    location: { kind: "none" },
  }
}

export function beginDateAxisPick(
  session: FiltersPanelSession
): FiltersPanelSession {
  const draftAxis =
    session.pending.date.kind === "none" ? null : session.pending.date.axis
  return {
    ...session,
    dateStep: draftAxis == null ? "axis" : "preset",
    dateDraftAxis: draftAxis,
  }
}

export function pickDateAxis(
  session: FiltersPanelSession,
  axis: DateAxisId
): FiltersPanelSession {
  return {
    ...session,
    dateStep: "preset",
    dateDraftAxis: axis,
  }
}

export function pickDatePreset(
  session: FiltersPanelSession,
  preset: DatePresetId,
  customRange?: { dateFrom: string; dateTo: string }
): FiltersPanelSession {
  const axis =
    session.dateDraftAxis ??
    (session.pending.date.kind === "none" ? null : session.pending.date.axis)

  if (preset === "any-time" || axis == null) {
    return {
      ...session,
      dateStep: null,
      dateDraftAxis: null,
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
      dateDraftAxis: null,
      pending: {
        ...session.pending,
        date: {
          kind: "custom",
          axis,
          dateFrom: range.dateFrom,
          dateTo: range.dateTo,
        },
      },
    }
  }

  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    pending: {
      ...session.pending,
      date: { kind: "preset", axis, preset },
    },
  }
}

export function clearDate(session: FiltersPanelSession): FiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    pending: {
      ...session.pending,
      date: { kind: "none" },
    },
  }
}

export function clearAllPending(
  session: FiltersPanelSession
): FiltersPanelSession {
  return {
    ...session,
    dateStep: null,
    dateDraftAxis: null,
    locationStep: null,
    pending: emptySelection(),
  }
}

export function applyPendingFilters(
  session: FiltersPanelSession
): GuestsFilterSelection {
  return cloneSelection(session.pending)
}

export type ChipLabelResolvers = {
  locationName: (id: string) => string
  tagName: (id: string) => string
}

export const MARKETING_LABELS: Record<MarketingOptionId, string> = {
  eligible: "Eligible to contact",
  "not-opted-in": "Not opted in",
}

export const CONTACT_LABELS: Record<ContactOptionId, string> = {
  email: "Email available",
  mobile: "Mobile available",
}

export const SENTIMENT_LABELS: Record<SentimentOptionId, string> = {
  positive: "Positive",
  neutral: "Neutral",
  negative: "Negative",
}

export const DATE_AXIS_LABELS: Record<DateAxisId, string> = {
  "first-captured": "First captured",
  "last-interaction": "Last interaction",
}

export const DATE_PRESET_LABELS: Record<
  Exclude<DatePresetId, "any-time" | "custom">,
  string
> = {
  today: "Today",
  "last-7": "Last 7 days",
  "last-30": "Last 30 days",
  "this-month": "This month",
  "previous-month": "Previous month",
}

function dateChipLabel(date: DateFilter): string | null {
  if (date.kind === "none") {
    return null
  }
  const axis = DATE_AXIS_LABELS[date.axis]
  if (date.kind === "custom") {
    return `${axis} · ${date.dateFrom}–${date.dateTo}`
  }
  return `${axis} · ${DATE_PRESET_LABELS[date.preset]}`
}

/** FE-only page chips — one per selected option (Date = one compound). */
export function projectFilterChips(
  selection: GuestsFilterSelection,
  resolvers: ChipLabelResolvers
): FilterChip[] {
  const chips: FilterChip[] = []

  for (const id of selection.marketing) {
    chips.push({
      id: `marketing:${id}`,
      kind: "marketing",
      label: MARKETING_LABELS[id],
      value: id,
    })
  }
  for (const id of selection.contact) {
    chips.push({
      id: `contact:${id}`,
      kind: "contact",
      label: CONTACT_LABELS[id],
      value: id,
    })
  }
  for (const id of selection.sentiment) {
    chips.push({
      id: `sentiment:${id}`,
      kind: "sentiment",
      label: SENTIMENT_LABELS[id],
      value: id,
    })
  }

  if (selection.location.kind === "all") {
    chips.push({
      id: "location:all",
      kind: "location-all",
      label: "All permitted locations",
      value: "all",
    })
  } else if (selection.location.kind === "individual") {
    for (const locationId of selection.location.locationIds) {
      chips.push({
        id: `location:${locationId}`,
        kind: "location-id",
        label: resolvers.locationName(locationId),
        value: locationId,
      })
    }
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

  for (const tagId of selection.tagIds) {
    chips.push({
      id: `tag:${tagId}`,
      kind: "tag",
      label: resolvers.tagName(tagId),
      value: tagId,
    })
  }

  return chips
}

export function filterChipBadgeCount(selection: GuestsFilterSelection): number {
  return projectFilterChips(selection, {
    locationName: (id) => id,
    tagName: (id) => id,
  }).length
}

export function removeAppliedChip(
  applied: GuestsFilterSelection,
  chip: FilterChip
): GuestsFilterSelection {
  const next = cloneSelection(applied)

  switch (chip.kind) {
    case "marketing":
      next.marketing = next.marketing.filter((id) => id !== chip.value)
      break
    case "contact":
      next.contact = next.contact.filter((id) => id !== chip.value)
      break
    case "sentiment":
      next.sentiment = next.sentiment.filter((id) => id !== chip.value)
      break
    case "location-all":
      next.location = { kind: "none" }
      break
    case "location-id":
      if (next.location.kind === "individual") {
        const locationIds = next.location.locationIds.filter(
          (id) => id !== chip.value
        )
        next.location =
          locationIds.length === 0
            ? { kind: "none" }
            : { kind: "individual", locationIds }
      }
      break
    case "date":
      next.date = { kind: "none" }
      break
    case "tag":
      next.tagIds = next.tagIds.filter((id) => id !== chip.value)
      break
  }

  return next
}

export function removePendingFieldChip(
  session: FiltersPanelSession,
  chip: FilterChip
): FiltersPanelSession {
  const selection = removeAppliedChip(session.pending, chip)
  return {
    ...session,
    pending: selection,
    dateStep: chip.kind === "date" ? null : session.dateStep,
    dateDraftAxis: chip.kind === "date" ? null : session.dateDraftAxis,
    locationStep:
      chip.kind === "location-all" || chip.kind === "location-id"
        ? null
        : session.locationStep,
  }
}
