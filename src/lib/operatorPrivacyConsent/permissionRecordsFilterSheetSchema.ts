import type { FilterSheetSchema } from "@/lib/operatorFilterSheet"
import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import {
  PERMISSION_RECORD_CURRENT_STATE_LABELS,
  type GuestPermissionId,
  type PermissionRecordCurrentState,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"

const PERMISSION_FILTER_LABELS: Record<GuestPermissionId, string> = {
  "email-marketing": "Email marketing",
  "sms-marketing": "SMS marketing",
  "feedback-follow-up": "Feedback follow-up",
}

function toOptions<TId extends string>(
  labels: Record<TId, string>
): Array<{ id: string; label: string }> {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

/**
 * Permission records filters — Figma 5746:100788 annotation:
 * Permission, Current state, Location, Date.
 */
export function permissionRecordsFilterSheetSchema(options: {
  locations?: ReadonlyArray<{ id: string; label: string }>
} = {}): FilterSheetSchema {
  return {
    fields: [
      {
        id: "permission",
        kind: "multi-select",
        label: "Permission",
        chipKind: "permission",
        options: toOptions(PERMISSION_FILTER_LABELS),
      },
      {
        id: "currentState",
        kind: "multi-select",
        label: "Current state",
        chipKind: "currentState",
        options: [
          ...toOptions(
            PERMISSION_RECORD_CURRENT_STATE_LABELS as Record<
              PermissionRecordCurrentState,
              string
            >
          ),
          // Figma 5746:100788 demo chips (Guests-style labels until product confirms).
          { id: "eligible-to-contact", label: "Eligible to contact" },
          { id: "negative", label: "Negative" },
        ],
      },
      {
        id: "location",
        kind: "multi-select",
        label: "Location",
        chipKind: "location",
        options: (options.locations ?? []).map((location) => ({
          id: location.id,
          label: location.label,
        })),
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
}
