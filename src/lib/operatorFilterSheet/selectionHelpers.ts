/** Read helpers for query-param builders — keep OperatorFilterSelection as the
 * page/tab module's stored shape while builders map field ids → wire params.
 */
import type {
  DateFilterValue,
  LocationOverride,
  OperatorFilterSelection,
} from "@/lib/operatorFilterSheet/types"

export function getMultiSelectIds(
  selection: OperatorFilterSelection,
  fieldId: string
): string[] {
  const value = selection[fieldId]
  return value?.kind === "multi-select" ? value.ids : []
}

export function getDateValue(
  selection: OperatorFilterSelection,
  fieldId: string
): DateFilterValue {
  const value = selection[fieldId]
  return value?.kind === "date" ? value.value : { kind: "none" }
}

export function getLocationOverride(
  selection: OperatorFilterSelection,
  fieldId: string
): LocationOverride {
  const value = selection[fieldId]
  return value?.kind === "location-scope" ? value.value : { kind: "none" }
}
