/** Guest Feedbacks tab Filter sheet schema — Classification · Issue tags · Date (no axis). */

import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type { FilterSheetSchema } from "@/lib/operatorFilterSheet"
import type { DetectedTagKey } from "@/lib/operatorHome/detectedTags"
import { DETECTED_TAG_LABELS } from "@/lib/operatorHome/detectedTags"
import type { SentimentOptionId } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { SENTIMENT_LABELS } from "@/lib/operatorGuests/guestsFilterSheetSchema"

export type { SentimentOptionId }
export { SENTIMENT_LABELS }

export const FEEDBACKS_DETECTED_TAG_OPTIONS = Object.entries(
  DETECTED_TAG_LABELS
).map(([id, label]) => ({ id, label }))

export type { DetectedTagKey }

export function guestFeedbacksFilterSheetSchema(): FilterSheetSchema {
  return {
    fields: [
      {
        id: "sentiment",
        kind: "multi-select",
        label: "Classification",
        chipKind: "sentiment",
        options: Object.entries(SENTIMENT_LABELS).map(([id, label]) => ({
          id,
          label,
        })),
      },
      {
        id: "detectedTag",
        kind: "multi-select",
        label: "Issue tags",
        chipKind: "detected-tag",
        options: FEEDBACKS_DETECTED_TAG_OPTIONS,
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
