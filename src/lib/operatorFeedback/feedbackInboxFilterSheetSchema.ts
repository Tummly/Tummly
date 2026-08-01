/** Feedback inbox Filter sheet — Guest response · Issue tags · QR source · Contact · Date. */

import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type {
  FilterSheetSchema,
  SchemaOption,
} from "@/lib/operatorFilterSheet"
import { FEEDBACKS_DETECTED_TAG_OPTIONS } from "@/lib/operatorGuestProfile/guestFeedbacksFilterSheetSchema"
import {
  CONTACT_LABELS,
  SENTIMENT_LABELS,
} from "@/lib/operatorGuests/guestsFilterSheetSchema"

export const FEEDBACK_INBOX_CATALOG_QR_SOURCE_OPTIONS: SchemaOption[] = [
  { id: "CounterCard", label: "Counter card" },
  { id: "PackagingSticker", label: "Packaging sticker" },
  { id: "DeliveryInsert", label: "Delivery insert" },
  { id: "WindowSticker", label: "Window sticker" },
  { id: "SmartGuest", label: "Smart Guest" },
]

export function feedbackInboxDigitalLinkOptionId(qrCodeId: number): string {
  return `dgl:${qrCodeId}`
}

export function feedbackInboxFilterSheetSchema(catalog: {
  digitalGuestLinks?: readonly SchemaOption[]
} = {}): FilterSheetSchema {
  return {
    fields: [
      {
        id: "sentiment",
        kind: "multi-select",
        label: "Guest response",
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
        id: "qrSource",
        kind: "multi-select",
        label: "QR source",
        chipKind: "qr-source",
        options: [
          ...FEEDBACK_INBOX_CATALOG_QR_SOURCE_OPTIONS,
          ...(catalog.digitalGuestLinks ?? []),
        ],
      },
      {
        id: "contact",
        kind: "multi-select",
        label: "Contact",
        chipKind: "contact",
        options: Object.entries(CONTACT_LABELS).map(([id, label]) => ({
          id,
          label,
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
