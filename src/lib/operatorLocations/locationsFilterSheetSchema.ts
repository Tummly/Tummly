import type { FilterSheetSchema } from "@/lib/operatorFilterSheet"
import {
  LOCATION_LIFECYCLE_LABELS,
  LOCATION_SETUP_LABELS,
  type LocationLifecycleStatus,
  type LocationSetupStatus,
} from "@/lib/operatorLocations/locationsPresentation"

function toOptions<TId extends string>(
  labels: Record<TId, string>
): Array<{ id: string; label: string }> {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

export type LocationsGuestFormStatusId =
  | "live"
  | "draft"
  | "ready-to-publish"
  | "paused"
  | "not-assigned"
  | "error"

export type LocationsQrStatusId =
  | "has-active-qr"
  | "no-active-qr"
  | "draft-qr-only"
  | "paused-qr-only"
  | "qr-error"
  | "smart-guest-link-only"

export type LocationsManagerFilterId = "assigned" | "unassigned"

export type LocationsLastActivityFilterId =
  | "today"
  | "last-7-days"
  | "last-30-days"
  | "no-activity-30-days"
  | "never"

const GUEST_FORM_STATUS_LABELS: Record<LocationsGuestFormStatusId, string> = {
  live: "Live",
  draft: "Draft",
  "ready-to-publish": "Ready to publish",
  paused: "Paused",
  "not-assigned": "Not assigned",
  error: "Error",
}

const QR_STATUS_LABELS: Record<LocationsQrStatusId, string> = {
  "has-active-qr": "Has active QR placement",
  "no-active-qr": "No active QR placement",
  "draft-qr-only": "Draft QR only",
  "paused-qr-only": "Paused QR only",
  "qr-error": "QR error",
  "smart-guest-link-only": "Smart Guest Link only",
}

const MANAGER_FILTER_LABELS: Record<LocationsManagerFilterId, string> = {
  assigned: "Assigned authorised managers",
  unassigned: "Unassigned",
}

const LAST_ACTIVITY_FILTER_LABELS: Record<
  LocationsLastActivityFilterId,
  string
> = {
  today: "Today",
  "last-7-days": "Last 7 days",
  "last-30-days": "Last 30 days",
  "no-activity-30-days": "No activity in 30 days",
  never: "Never",
}

/** Filter locations sheet — Figma Filters panel notes on 3753:66374. */
export function locationsFilterSheetSchema(options: {
  cities?: ReadonlyArray<{ id: string; label: string }>
} = {}): FilterSheetSchema {
  return {
    fields: [
      {
        id: "lifecycle",
        kind: "multi-select",
        label: "Lifecycle status",
        chipKind: "lifecycle",
        options: toOptions(
          LOCATION_LIFECYCLE_LABELS as Record<LocationLifecycleStatus, string>
        ),
      },
      {
        id: "setup",
        kind: "multi-select",
        label: "Setup status",
        chipKind: "setup",
        options: toOptions(
          LOCATION_SETUP_LABELS as Record<LocationSetupStatus, string>
        ),
      },
      {
        id: "guestForm",
        kind: "multi-select",
        label: "Guest Form status",
        chipKind: "guestForm",
        options: toOptions(GUEST_FORM_STATUS_LABELS),
      },
      {
        id: "qr",
        kind: "multi-select",
        label: "QR status",
        chipKind: "qr",
        options: toOptions(QR_STATUS_LABELS),
      },
      {
        id: "manager",
        kind: "multi-select",
        label: "Location manager",
        chipKind: "manager",
        options: toOptions(MANAGER_FILTER_LABELS),
      },
      {
        id: "city",
        kind: "multi-select",
        label: "Location",
        chipKind: "city",
        options: options.cities ?? [],
      },
      {
        id: "lastActivity",
        kind: "multi-select",
        label: "Last activity",
        chipKind: "lastActivity",
        options: toOptions(LAST_ACTIVITY_FILTER_LABELS),
      },
    ],
  }
}
