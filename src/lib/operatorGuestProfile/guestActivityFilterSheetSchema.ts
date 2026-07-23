/** Guest Activity tab Filter sheet schema — Activity type · Date on OccurredAt (no axis). */

import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type { FilterSheetSchema } from "@/lib/operatorFilterSheet"
import { OPERATOR_GUEST_ACTIVITY_TYPE_LABELS } from "@/lib/operatorGuestProfile/guestProfilePresentation"

export type ActivityTypeId = keyof typeof OPERATOR_GUEST_ACTIVITY_TYPE_LABELS

export const ACTIVITY_TYPE_OPTIONS = Object.entries(
  OPERATOR_GUEST_ACTIVITY_TYPE_LABELS
).map(([id, label]) => ({ id, label }))

export function guestActivityFilterSheetSchema(): FilterSheetSchema {
  return {
    fields: [
      {
        id: "activityType",
        kind: "multi-select",
        label: "Activity type",
        chipKind: "activity-type",
        options: ACTIVITY_TYPE_OPTIONS,
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
