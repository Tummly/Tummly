/** One overview count. `value` is null when `available` is false (never shown as 0). */
export interface AdminOverviewMetric<T> {
  available: boolean
  value: T | null
}

export interface AdminOverviewBillingStatusCount {
  billingStatus: string
  count: number
}

export interface AdminOverview {
  generatedAtUtc: string
  workspacesByBillingStatus: AdminOverviewMetric<AdminOverviewBillingStatusCount[]>
  shopOrdersPaidNotDispatched: AdminOverviewMetric<number>
  shopOrdersFlaggedForReview: AdminOverviewMetric<number>
  printAssetsFailed: AdminOverviewMetric<number>
  pilotsEndingWithin7Days: AdminOverviewMetric<number>
  openHelpCentreQueries: AdminOverviewMetric<number>
  failedAuditEventsLast7Days: AdminOverviewMetric<number>
}

export type AdminProviderStatus = "configured" | "not_configured"

export interface AdminEnvironment {
  environmentName: string
  isProduction: boolean
  commitSha: string
  apiStartedAtUtc: string
  latestMigrationId: string | null
  revolut: { status: AdminProviderStatus; mode: string }
  resend: { status: AdminProviderStatus; senderDomain: string | null }
  twilio: { status: AdminProviderStatus; fromNumberMasked: string | null }
  vatModeActive: boolean
}

export interface AdminAuditEvent {
  id: string
  occurredAtUtc: string
  action: string
  actorAdminUserId: number | null
  actorIdentity: string
  targetType: string
  targetId: string
  restaurantId: number | null
  detailJson: string | null
  succeeded: boolean
  correlationId: string | null
}

export interface AdminAuditEventsResponse {
  success: boolean
  data: AdminAuditEvent[]
  totalCount: number
}
