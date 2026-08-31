/** Locations page copy and chrome — Figma 5748:103523 / 3753:66374. */

import type { VariantProps } from "class-variance-authority"

import type { badgeVariants } from "@/components/ui/badge"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_SORT_MENU_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

export {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS as LOCATIONS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS as LOCATIONS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_ROW_ACTIONS_ITEM_CLASS as LOCATIONS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS as LOCATIONS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS as LOCATIONS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS as LOCATIONS_ROW_ACTIONS_TRIGGER_CLASS,
  GUESTS_SORT_BUTTON_CLASS as LOCATIONS_SORT_BUTTON_CLASS,
  GUESTS_SORT_MENU_CLASS as LOCATIONS_SORT_MENU_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS as LOCATIONS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS as LOCATIONS_TABLE_MENU_ITEM_SELECTED_CLASS,
}

/** Settings card fill — same chrome as Account Workspace / Team Permissions. */
export { ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS as LOCATIONS_CARD_CLASS } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

export const LOCATIONS_PAGE_COPY = {
  title: "Locations",
  subtitle:
    "Manage the restaurant locations connected to Guest Loop activity, access and reporting.",
  addLocation: "Add location",
  importLocations: "Import locations",
  searchPlaceholder: "Search by location name, city or postcode",
  filtersLabel: "Filters",
  filtersTitle: "Filter locations",
  emptyTitle: "No locations found",
  emptyHelper: "Try a different search or clear filters.",
  clearSearchAndFilters: "Clear search and filters",
  needsAttentionTitle: "Needs attention",
  needsAttentionSubtitle: (count: number) =>
    count === 1
      ? "1 location needs attention"
      : `${count} locations need attention`,
  needsAttentionEmptyTitle: "All locations are ready",
  needsAttentionEmptyBody:
    "No setup issues need attention right now.",
  reviewLocation: "Review location",
  activityTitle: "Activity",
  activityEmptyTitle: "No activity yet",
  activityEmptyBody: "Location activity will appear here.",
} as const

/** Needs-attention row chrome — Figma 5748:103603. */
export const LOCATIONS_NEEDS_ATTENTION_ROW_CLASS =
  "flex w-full items-center gap-3.5 overflow-clip rounded bg-op-background-secondary px-5"

export const LOCATIONS_NEEDS_ATTENTION_ROW_COPY_CLASS =
  "min-w-0 flex-1 py-5 text-base font-semibold leading-6 tracking-[-0.4px] text-op-text-primary"

export const LOCATIONS_ACTIVITY_ITEM_TIME_CLASS =
  "m-0 text-op-sm font-medium leading-[19px] text-op-text-primary"

export const LOCATIONS_ACTIVITY_ITEM_BODY_CLASS =
  "m-0 text-op-sm font-medium leading-[19px] text-op-card-subtitle-color"

export type LocationsSetupAttentionItemId =
  | "privacy-review"
  | "no-active-qr"

export type LocationsSetupAttentionItem = {
  id: LocationsSetupAttentionItemId
  message: string
}

export type LocationsActivityItem = {
  id: string
  timeLabel: string
  description: string
}

export function formatNeedsAttentionSubtitle(count: number): string {
  return LOCATIONS_PAGE_COPY.needsAttentionSubtitle(count)
}

export type LocationsTabId = "locations" | "setup-readiness" | "activity"

export const LOCATIONS_TAB_IDS = [
  "locations",
  "setup-readiness",
  "activity",
] as const satisfies readonly LocationsTabId[]

export const LOCATIONS_TAB_LABELS: Record<LocationsTabId, string> = {
  locations: "Locations",
  "setup-readiness": "Setup & readiness",
  activity: "Activity",
}

export function resolveLocationsTabId(
  raw: string | null | undefined
): LocationsTabId {
  if (raw === "setup-readiness" || raw === "activity" || raw === "locations") {
    return raw
  }
  return "locations"
}

/** Figma Setup & readiness tab count chip. */
export const LOCATIONS_TAB_COUNT_BADGE_CLASS =
  "inline-flex size-4 shrink-0 items-center justify-center overflow-clip rounded bg-[#e8e8e8] p-1 text-[10px] font-normal leading-none text-foreground dark:bg-[#232323] dark:text-white"

export type LocationLifecycleStatus =
  | "active"
  | "draft"
  | "paused"
  | "archived"

export type LocationSetupStatus =
  | "ready"
  | "needs-attention"
  | "blocked"
  | "not-started"

export const LOCATION_LIFECYCLE_LABELS: Record<
  LocationLifecycleStatus,
  string
> = {
  active: "Active",
  draft: "Draft",
  paused: "Paused",
  archived: "Archived",
}

export const LOCATION_SETUP_LABELS: Record<LocationSetupStatus, string> = {
  ready: "Ready",
  "needs-attention": "Needs attention",
  blocked: "Blocked",
  "not-started": "Not started",
}

export type LocationLifecycleBadgeVariant = NonNullable<
  VariantProps<typeof badgeVariants>["variant"]
>

/** Lifecycle chip paint — Archived is plain text in Figma, not a badge. */
export function locationLifecycleBadgeVariant(
  status: LocationLifecycleStatus
): LocationLifecycleBadgeVariant | null {
  switch (status) {
    case "active":
      return "positive"
    case "draft":
      return "soft"
    case "paused":
      return "neutral"
    case "archived":
      return null
  }
}

export type LocationRowActionId =
  | "view-location"
  | "edit-location"
  | "view-qr-placements"
  | "view-feedback"
  | "view-reports"
  | "pause-location"
  | "archive-location"
  | "continue-setup"
  | "delete-draft"
  | "view-historical-activity"
  | "resume-location"
  | "view-historical-record"
  | "restore-location"
  | "export-location-history"

export type LocationRowAction = {
  id: LocationRowActionId
  label: string
  enabled: boolean
}

const ACTION_LABELS: Record<LocationRowActionId, string> = {
  "view-location": "View location",
  "edit-location": "Edit location",
  "view-qr-placements": "View QR placements",
  "view-feedback": "View Feedback",
  "view-reports": "View Reports",
  "pause-location": "Pause location",
  "archive-location": "Archive location",
  "continue-setup": "Continue setup",
  "delete-draft": "Delete Draft",
  "view-historical-activity": "View historical activity",
  "resume-location": "Resume location",
  "view-historical-record": "View historical record",
  "restore-location": "Restore location",
  "export-location-history": "Export location history",
}

function actions(
  ids: readonly LocationRowActionId[]
): LocationRowAction[] {
  return ids.map((id) => ({
    id,
    label: ACTION_LABELS[id],
    enabled: true,
  }))
}

/** Row ⋯ menu options by lifecycle — Figma annotations on 3753:66374. */
export function locationRowActionsForLifecycle(
  status: LocationLifecycleStatus
): LocationRowAction[] {
  switch (status) {
    case "active":
      return actions([
        "view-location",
        "edit-location",
        "view-qr-placements",
        "view-feedback",
        "view-reports",
        "pause-location",
        "archive-location",
      ])
    case "draft":
      return actions(["continue-setup", "edit-location", "delete-draft"])
    case "paused":
      return actions([
        "view-location",
        "edit-location",
        "view-historical-activity",
        "resume-location",
        "archive-location",
      ])
    case "archived":
      return actions([
        "view-historical-record",
        "restore-location",
        "export-location-history",
      ])
  }
}

export type LocationsSortId = "name-asc" | "name-desc"

export const LOCATIONS_SORT_LABELS: Record<LocationsSortId, string> = {
  "name-asc": "Location name: A–Z",
  "name-desc": "Location name: Z–A",
}

export const LOCATIONS_SORT_OPTIONS = Object.entries(
  LOCATIONS_SORT_LABELS
) as Array<[LocationsSortId, string]>

export const LOCATIONS_DEFAULT_SORT_ID: LocationsSortId = "name-asc"

export const LOCATIONS_PAGE_SIZE = 10

export type LocationsKpiId =
  | "active-locations"
  | "draft-locations"
  | "paused-locations"
  | "setup-needs-attention"

export type LocationsKpi = {
  id: LocationsKpiId
  label: string
  primaryText: string
}

export const LOCATIONS_KPI_LABELS: Record<LocationsKpiId, string> = {
  "active-locations": "Active locations",
  "draft-locations": "Draft locations",
  "paused-locations": "Paused locations",
  "setup-needs-attention": "Setup needs attention",
}

export function buildLocationsKpis(counts: {
  active: number
  draft: number
  paused: number
  setupNeedsAttention: number
}): LocationsKpi[] {
  return [
    {
      id: "active-locations",
      label: LOCATIONS_KPI_LABELS["active-locations"],
      primaryText: String(counts.active),
    },
    {
      id: "draft-locations",
      label: LOCATIONS_KPI_LABELS["draft-locations"],
      primaryText: String(counts.draft),
    },
    {
      id: "paused-locations",
      label: LOCATIONS_KPI_LABELS["paused-locations"],
      primaryText: String(counts.paused),
    },
    {
      id: "setup-needs-attention",
      label: LOCATIONS_KPI_LABELS["setup-needs-attention"],
      primaryText: String(counts.setupNeedsAttention),
    },
  ]
}

export function formatLocationsPageRange(options: {
  page: number
  pageSize: number
  totalCount: number
}): string {
  if (options.totalCount === 0) {
    return "Showing 0 of 0 locations"
  }
  const start = (options.page - 1) * options.pageSize + 1
  const end = Math.min(options.page * options.pageSize, options.totalCount)
  return `Showing ${start}–${end} of ${options.totalCount} locations`
}
