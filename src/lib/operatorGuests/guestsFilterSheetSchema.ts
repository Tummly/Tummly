/** Guests Filter sheet schema — full field set (marketing, contact, sentiment, location, date, tags). */

import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type {
  DateAxisId,
  FilterSheetSchema,
  SchemaOption,
} from "@/lib/operatorFilterSheet"

export type MarketingOptionId = "eligible" | "not-opted-in"
export type ContactOptionId = "email" | "mobile"
export type SentimentOptionId = "positive" | "neutral" | "negative"

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

function toOptions<TId extends string>(
  labels: Record<TId, string>
): SchemaOption[] {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

export function guestsFilterSheetSchema(
  catalog: {
    locations?: readonly SchemaOption[]
    tags?: readonly SchemaOption[]
  } = {}
): FilterSheetSchema {
  return {
    fields: [
      {
        id: "marketing",
        kind: "multi-select",
        label: "Marketing status",
        chipKind: "marketing",
        options: toOptions(MARKETING_LABELS),
      },
      {
        id: "contact",
        kind: "multi-select",
        label: "Contact channel",
        chipKind: "contact",
        options: toOptions(CONTACT_LABELS),
      },
      {
        id: "sentiment",
        kind: "multi-select",
        label: "Feedback classification",
        chipKind: "sentiment",
        options: toOptions(SENTIMENT_LABELS),
      },
      {
        id: "location",
        kind: "location-scope",
        label: "Location",
        locations: catalog.locations ?? [],
      },
      {
        id: "date",
        kind: "date",
        label: "Date",
        hasAxis: true,
        axisLabels: DATE_AXIS_LABELS,
        presetLabels: DATE_PRESET_LABELS,
      },
      {
        id: "tag",
        kind: "multi-select",
        label: "Tags",
        chipKind: "tag",
        options: catalog.tags ?? [],
      },
    ],
  }
}

/** Guests schema always shows Location; callers omit the field via `showLocationFilter`. */
export function guestsFilterSheetSchemaForWorkspace(catalog: {
  locations: readonly SchemaOption[]
  tags: readonly SchemaOption[]
  showLocationFilter: boolean
}): FilterSheetSchema {
  const schema = guestsFilterSheetSchema(catalog)
  if (catalog.showLocationFilter) {
    return schema
  }
  return { fields: schema.fields.filter((field) => field.id !== "location") }
}
