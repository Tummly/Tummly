import type { DatePresetId } from "@/lib/operatorFilterSheet/types"

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
