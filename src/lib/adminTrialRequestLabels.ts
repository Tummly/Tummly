import {
  BUSINESS_CATEGORY_OPTIONS,
  LOCATION_COUNT_OPTIONS,
  MAIN_GOAL_OPTIONS,
  ROLE_OPTIONS,
} from "@/components/home/hero-trial-options"
import type { SelectOption } from "@/components/ui/floating-label-select"

function resolveOptionLabel(options: SelectOption[], value: string) {
  const match = options.find(
    (option) => option.value === value || option.label === value
  )
  return match?.label ?? value
}

export function formatTrialRequestGoal(goal: string) {
  return resolveOptionLabel(MAIN_GOAL_OPTIONS, goal)
}

export function formatTrialRequestRole(role: string) {
  return resolveOptionLabel(ROLE_OPTIONS, role)
}

export function formatTrialRequestCategory(category: string) {
  return resolveOptionLabel(BUSINESS_CATEGORY_OPTIONS, category)
}

export function formatTrialRequestLocations(locations: string) {
  return resolveOptionLabel(LOCATION_COUNT_OPTIONS, locations)
}

export function formatAdminDate(value?: string | null) {
  if (!value) {
    return null
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return null
  }

  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  })
}

export function formatAdminBoolean(value: boolean) {
  return value ? "Yes" : "No"
}

export function formatAdminText(value?: string | null) {
  const trimmed = value?.trim()
  return trimmed ? trimmed : "—"
}
