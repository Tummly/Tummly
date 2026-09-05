/**
 * Figma Operator Reports — Campaigns report sub-page.
 */

import type { ReportsKpiItem } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { CAMPAIGN_CHANNEL_OPTIONS } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import { CAMPAIGNS_LIST_TABLE_COPY } from "@/lib/operatorCampaigns/campaignListPresentation"
import {
  labelForCampaignGoalId,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_SUFFIX,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  ReportsCampaignsResponse,
  ReportsMetricWire,
} from "@/types/operatorReports"

export const CAMPAIGNS_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbCampaignsReport: "Campaigns report",
  title: "Campaigns report",
  pageTitle: "Campaigns report",
  subtitle: "See how your campaign sends and delivery perform.",
  pageSubtitle: "See how your campaign sends and delivery perform.",
  generateBrief: "Generate brief",
  export: "Export",

  emptyTitle: "No campaign reports yet",
  emptySubtitle:
    "Send your first campaign to eligible guests. Delivery and failed sends will show here.",
  createCampaign: "Create campaign",

  performanceSectionTitle: "Campaign performance",
  needsAttentionSectionTitle: "Needs attention",
  needsAttentionSectionSubtitle: "Review issues that may require action.",

  campaignHeader: "Campaign",
  goalHeader: "Goal",
  channelHeader: "Channel",
  sentHeader: "Sent",
  statusHeader: "Status",

  viewCampaigns: "View campaigns",
  loadError: "Could not load campaigns report. Please try again.",
  retry: "Retry",
} as const

export type CampaignsReportPerformanceRow = {
  campaignId: number
  name: string
  goal: string
  channel: string
  sent: number
  status: string
  statusLabel: string
}

export type CampaignsReportAttentionItem = {
  campaignId: number
  name: string
  status: string
  statusLabel: string
}

export type CampaignsReportViewModel = {
  kpis: ReportsKpiItem[]
  performance: CampaignsReportPerformanceRow[]
  attentionItems: CampaignsReportAttentionItem[]
}

function metricToKpi(label: string, metric: ReportsMetricWire): ReportsKpiItem {
  const trendPercent = computeKpiTrendPercent(
    metric.value,
    metric.valuePrevious,
  )
  const positive =
    trendPercent == null
      ? null
      : trendPercent > 0
        ? true
        : trendPercent < 0
          ? false
          : null
  return {
    label,
    value: String(metric.value),
    delta: `${formatKpiTrendPercentValue(trendPercent)}% ${PERFORMANCE_KPI_TREND_SUFFIX}`,
    positive,
  }
}

export function labelForCampaignsReportStatus(status: string): string {
  if (status === "partially-sent") {
    return "Partially sent"
  }
  if (status.length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

function labelForChannel(channel: string | null): string {
  if (channel == null || channel.trim().length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  const match = CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channel)
  return match?.title ?? channel
}

function labelForGoal(goal: string | null): string {
  if (goal == null || goal.trim().length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  return labelForCampaignGoalId(goal as CampaignGoalId) ?? goal
}

/** Map a ready Campaigns report API body into KPIs, table, and attention. */
export function buildCampaignsReportViewModel(
  response: Extract<ReportsCampaignsResponse, { lifetimeEmpty: false }>,
): CampaignsReportViewModel {
  return {
    kpis: [
      metricToKpi("Campaigns sent", response.campaignsSent),
      metricToKpi("Guests messaged", response.guestsMessaged),
      metricToKpi("Failed sends", response.failedSends),
    ],
    performance: response.performance.map((row) => ({
      campaignId: row.campaignId,
      name: row.name,
      goal: labelForGoal(row.goal),
      channel: labelForChannel(row.channel),
      sent: row.sent,
      status: row.status,
      statusLabel: labelForCampaignsReportStatus(row.status),
    })),
    attentionItems: response.needsAttention.map((row) => ({
      campaignId: row.campaignId,
      name: row.name,
      status: row.status,
      statusLabel: labelForCampaignsReportStatus(row.status),
    })),
  }
}
