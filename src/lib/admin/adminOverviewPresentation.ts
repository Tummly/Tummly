import type { AdminOverview, AdminOverviewMetric } from "@/types/adminOps"

import { formatLondonDateTime } from "./londonTime"

export const UNAVAILABLE_COUNT_TEXT = "Unavailable"

/** Tab ids on the Admin dashboard, plus the Help Centre route. */
export type AdminOverviewTarget =
  | { kind: "tab"; tab: "orders" | "materials" | "pilots" | "audit" }
  | { kind: "route"; to: string }

export interface AdminOverviewCard {
  id: string
  title: string
  description: string
  valueText: string
  /** Null when the count is unavailable, so the card never reads as zero. */
  available: boolean
  detailText: string | null
  /** Null when no existing screen shows this count yet. */
  target: AdminOverviewTarget | null
  linkLabel: string | null
}

function countText<T>(metric: AdminOverviewMetric<T>, format: (value: T) => string) {
  return metric.available && metric.value !== null
    ? format(metric.value)
    : UNAVAILABLE_COUNT_TEXT
}

function plainCount(metric: AdminOverviewMetric<number>) {
  return countText(metric, (value) => value.toLocaleString("en-GB"))
}

export function buildAdminOverviewCards(
  overview: AdminOverview,
  supportUrl: string
): AdminOverviewCard[] {
  const workspaces = overview.workspacesByBillingStatus
  const workspaceRows = workspaces.value ?? []
  const workspaceTotal = workspaceRows.reduce((sum, row) => sum + row.count, 0)

  return [
    {
      id: "workspaces",
      title: "Workspaces by billing status",
      description: "Every workspace, grouped by billing status.",
      valueText: countText(workspaces, () => workspaceTotal.toLocaleString("en-GB")),
      available: workspaces.available,
      detailText: workspaces.available
        ? workspaceRows.map((row) => `${row.billingStatus} ${row.count}`).join(" · ")
        : null,
      target: null,
      linkLabel: null,
    },
    {
      id: "shop-to-dispatch",
      title: "Shop orders to dispatch",
      description: "Paid, not yet dispatched.",
      valueText: plainCount(overview.shopOrdersPaidNotDispatched),
      available: overview.shopOrdersPaidNotDispatched.available,
      detailText: null,
      target: { kind: "tab", tab: "orders" },
      linkLabel: "Open shop orders",
    },
    {
      id: "shop-review",
      title: "Shop orders flagged for review",
      description: "Paid orders that need a manual decision.",
      valueText: plainCount(overview.shopOrdersFlaggedForReview),
      available: overview.shopOrdersFlaggedForReview.available,
      detailText: null,
      target: { kind: "tab", tab: "orders" },
      linkLabel: "Open shop orders",
    },
    {
      id: "print-failed",
      title: "Print assets failed",
      description: "Print-ready QR assets that failed to build.",
      valueText: plainCount(overview.printAssetsFailed),
      available: overview.printAssetsFailed.available,
      detailText: null,
      target: { kind: "tab", tab: "orders" },
      linkLabel: "Open shop orders",
    },
    {
      id: "pilots-ending",
      title: "Pilots ending in 7 days",
      description: "Pilot periods that end within the next seven days.",
      valueText: plainCount(overview.pilotsEndingWithin7Days),
      available: overview.pilotsEndingWithin7Days.available,
      detailText: null,
      target: { kind: "tab", tab: "pilots" },
      linkLabel: "Open pilots",
    },
    {
      id: "support-open",
      title: "Open Help Centre queries",
      description: "Queries that are new, in progress, waiting or escalated.",
      valueText: plainCount(overview.openHelpCentreQueries),
      available: overview.openHelpCentreQueries.available,
      detailText: null,
      target: { kind: "route", to: supportUrl },
      linkLabel: "Open support",
    },
    {
      id: "audit-failed",
      title: "Failed audit events (7 days)",
      description: "Admin actions in the last seven days that did not succeed.",
      valueText: plainCount(overview.failedAuditEventsLast7Days),
      available: overview.failedAuditEventsLast7Days.available,
      detailText: null,
      target: { kind: "tab", tab: "audit" },
      linkLabel: "Open audit log",
    },
  ]
}

export function hasUnavailableAdminOverviewCounts(overview: AdminOverview): boolean {
  return [
    overview.workspacesByBillingStatus,
    overview.shopOrdersPaidNotDispatched,
    overview.shopOrdersFlaggedForReview,
    overview.printAssetsFailed,
    overview.pilotsEndingWithin7Days,
    overview.openHelpCentreQueries,
    overview.failedAuditEventsLast7Days,
  ].some((metric) => !metric.available)
}

export function adminOverviewAsOfText(generatedAtUtc: string): string {
  return `As of ${formatLondonDateTime(generatedAtUtc)} (Europe/London)`
}
