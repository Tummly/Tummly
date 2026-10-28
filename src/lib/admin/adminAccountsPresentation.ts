import type { AdminAccountListParams, AdminAccountPlanLimit } from "@/api/adminAccountsApi"

const LONDON_DATE_FORMAT = new Intl.DateTimeFormat("en-GB", {
  timeZone: "Europe/London",
  day: "2-digit",
  month: "short",
  year: "numeric",
})

export const ALL_FILTER_VALUE = "all"

export type AdminAccountFilters = {
  search: string
  plan: string
  billingStatus: string
  createdFrom: string
  createdTo: string
}

export const ACCOUNT_PLAN_OPTIONS = [
  "Pilot",
  "Free",
  "Starter",
  "Growth",
  "Group",
  "Custom",
] as const

export const ACCOUNT_BILLING_STATUS_OPTIONS = [
  "Pilot",
  "Free",
  "Active",
  "Past due",
  "Soft lock",
  "Dormant",
] as const

export function buildAccountListParams(
  filters: AdminAccountFilters,
  skip: number,
  take: number
): AdminAccountListParams {
  const search = filters.search.trim()
  return {
    search: search === "" ? undefined : search,
    plan: filters.plan === ALL_FILTER_VALUE ? undefined : filters.plan,
    billingStatus:
      filters.billingStatus === ALL_FILTER_VALUE
        ? undefined
        : filters.billingStatus,
    createdFrom: filters.createdFrom === "" ? undefined : filters.createdFrom,
    createdTo: filters.createdTo === "" ? undefined : filters.createdTo,
    skip,
    take,
  }
}

/** Returns a message when the created range is reversed, otherwise null. */
export function validateCreatedRange(
  createdFrom: string,
  createdTo: string
): string | null {
  if (createdFrom === "" || createdTo === "") return null
  return createdFrom > createdTo
    ? "Created from must be on or before created to."
    : null
}

export function billingStatusBadgeVariant(
  status: string
): "secondary" | "ready" | "error" | "neutral" | "outline" {
  switch (status) {
    case "Pilot":
      return "secondary"
    case "Active":
      return "ready"
    case "Past due":
      return "error"
    case "Soft lock":
    case "Dormant":
      return "neutral"
    default:
      return "outline"
  }
}

export function locationStatusBadgeVariant(
  status: string
): "ready" | "neutral" | "secondary" | "outline" {
  switch (status) {
    case "Active":
      return "ready"
    case "Paused":
      return "neutral"
    case "Archived":
      return "secondary"
    default:
      return "outline"
  }
}

const LONDON_DATE_TIME_FORMAT = new Intl.DateTimeFormat("en-GB", {
  timeZone: "Europe/London",
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
})

/** Date and time in Europe/London, for "as of" labels. */
export function formatAccountDateTime(iso: string | null): string {
  if (iso == null) return "—"
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return LONDON_DATE_TIME_FORMAT.format(date)
}

export function formatAccountDate(iso: string | null): string {
  if (iso == null) return "—"
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return LONDON_DATE_FORMAT.format(date)
}

/** Shows "current of cap" for a capacity pool, or "Unavailable" when entitlements cannot be resolved. */
export function capacityLabel(limit: AdminAccountPlanLimit): string {
  if (!limit.available || limit.cap == null) return "Unavailable"
  return `${limit.current ?? 0} of ${limit.cap}`
}

export function pageSummary(
  skip: number,
  shownCount: number,
  totalCount: number
): string {
  if (totalCount === 0) return "No accounts"
  const first = skip + 1
  const last = skip + shownCount
  return `Showing ${first}–${last} of ${totalCount}`
}

export function hasNextPage(skip: number, take: number, totalCount: number): boolean {
  return skip + take < totalCount
}

export function hasPreviousPage(skip: number): boolean {
  return skip > 0
}

/** Describes a scheduled change from stored server fields. Returns null when none is scheduled. */
export function scheduledChangeLabel(scheduled: {
  hasScheduledChange: boolean
  targetSubscriptionPlan: string | null
  targetBillingCycle: string | null
  cancelScheduled: boolean
  effectiveAtUtc: string | null
}): string | null {
  if (!scheduled.hasScheduledChange && !scheduled.cancelScheduled) return null
  const when = formatAccountDate(scheduled.effectiveAtUtc)
  if (scheduled.cancelScheduled) {
    return `Cancels at renewal on ${when}`
  }
  const target = [scheduled.targetSubscriptionPlan, scheduled.targetBillingCycle]
    .filter((part): part is string => part != null && part !== "")
    .join(" ")
  return `Changes to ${target || "a new plan"} on ${when}`
}
