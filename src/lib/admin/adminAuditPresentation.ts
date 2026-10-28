import type { AdminAuditEvent } from "@/types/adminOps"

import { londonDayBoundaryUtcIso } from "./londonTime"

export interface AdminAuditFilterInput {
  action: string
  restaurantId: string
  /** Calendar day in Europe/London, `YYYY-MM-DD`, or blank. */
  from: string
  /** Calendar day in Europe/London, `YYYY-MM-DD`, or blank. */
  to: string
  targetId: string
}

export const EMPTY_AUDIT_FILTERS: AdminAuditFilterInput = {
  action: "",
  restaurantId: "",
  from: "",
  to: "",
  targetId: "",
}

export type AdminAuditQueryParams = Record<string, string | number>

export type AdminAuditQueryResult =
  | { ok: true; params: AdminAuditQueryParams }
  | { ok: false; error: string }

/**
 * Builds the query for `GET /api/admin/audit-events`. Blank filters are left
 * out. Dates are London calendar days, converted to UTC instants.
 */
export function buildAdminAuditQuery(
  filters: AdminAuditFilterInput,
  paging: { skip: number; take: number }
): AdminAuditQueryResult {
  const params: AdminAuditQueryParams = {
    skip: paging.skip,
    take: paging.take,
  }

  const action = filters.action.trim()
  if (action) params.action = action

  const restaurantId = filters.restaurantId.trim()
  if (restaurantId) {
    if (!/^[1-9]\d*$/.test(restaurantId)) {
      return { ok: false, error: "Enter a restaurant ID as a whole number." }
    }
    params.restaurantId = Number(restaurantId)
  }

  const targetId = filters.targetId.trim()
  if (targetId) params.targetId = targetId

  const fromUtc = filters.from ? londonDayBoundaryUtcIso(filters.from, "start") : null
  if (filters.from && !fromUtc) {
    return { ok: false, error: "Enter a valid from date." }
  }

  const toUtc = filters.to ? londonDayBoundaryUtcIso(filters.to, "end") : null
  if (filters.to && !toUtc) {
    return { ok: false, error: "Enter a valid to date." }
  }

  if (filters.from && filters.to && filters.from > filters.to) {
    return { ok: false, error: "The from date must be on or before the to date." }
  }

  if (fromUtc) params.from = fromUtc
  if (toUtc) params.to = toUtc

  return { ok: true, params }
}

/** Pretty-printed detail for the expanded row. Non-JSON text is shown as it is. */
/** "Showing 1–50 of 120"; a page past the end says so instead of "51–50". */
export function auditPageRangeText(skip: number, rowCount: number, totalCount: number): string {
  if (rowCount === 0) {
    return totalCount === 0 ? "No events" : `No events on this page (${totalCount} in total)`
  }
  return `Showing ${skip + 1}–${skip + rowCount} of ${totalCount}`
}

export function formatAuditDetail(detailJson: string | null): {
  text: string
  isJson: boolean
} {
  if (!detailJson || !detailJson.trim()) {
    return { text: "No detail recorded.", isJson: false }
  }

  try {
    return { text: JSON.stringify(JSON.parse(detailJson), null, 2), isJson: true }
  } catch {
    return { text: detailJson, isJson: false }
  }
}

export function auditResultLabel(event: Pick<AdminAuditEvent, "succeeded">): "Succeeded" | "Failed" {
  return event.succeeded ? "Succeeded" : "Failed"
}

export function auditActorLabel(
  event: Pick<AdminAuditEvent, "actorAdminUserId" | "actorIdentity">
): string {
  if (event.actorIdentity.trim()) return event.actorIdentity
  if (event.actorAdminUserId !== null) return `Admin #${event.actorAdminUserId}`
  return "System"
}

export function auditTargetLabel(event: Pick<AdminAuditEvent, "targetType" | "targetId">): string {
  return `${event.targetType} ${event.targetId}`.trim()
}
