import type { ReportsKpiItem } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_SUFFIX,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  ReportsFeedbackBySourceWire,
  ReportsFeedbackNeedsAttentionWire,
  ReportsFeedbackResponse,
  ReportsMetricWire,
} from "@/types/operatorReports"

export const FEEDBACK_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbFeedbackReport: "Feedback report",
  title: "Feedback report",
  pageTitle: "Feedback report",
  subtitle:
    "Read private guest feedback and follow up where needed.",
  pageSubtitle:
    "Read private guest feedback and follow up where needed.",
  generateBrief: "Generate brief",
  export: "Export",
  emptyTitle: "No feedback yet",
  emptySubtitle:
    "Once guests submit private feedback, you'll see messages, marketing opt-ins and follow-up activity here.",
  checkGuestForm: "Check guest form",
  needsFollowUpTitle: "Needs follow-up",
  needsFollowUpSubtitle: "Feedback that may need a team response.",
  openFeedbackInbox: "Open feedback inbox",
  openAction: "Open",
  feedbackBySourceTitle: "Feedback by source",
  feedbackBySourceSubtitle: "Which QR placements generated feedback.",
  feedbackStatusTitle: "Feedback status",
  manageFeedback: "Manage feedback",
  loadError: "Could not load feedback report. Please try again.",
  retry: "Retry",
} as const

export type FeedbackReportFollowUpRow = {
  feedbackId: number
  date: string
  guest: string
  source: string
  feedback: string
  status: string
}

export type FeedbackReportSourceRow = {
  qrCodeId: number
  source: string
  feedback: number
  marketingOptIns: number
  followUpNeeded: number
}

export type FeedbackReportViewModel = {
  topKpis: ReportsKpiItem[]
  statusKpis: ReportsKpiItem[]
  followUpList: FeedbackReportFollowUpRow[]
  sourcesList: FeedbackReportSourceRow[]
}

function metricToKpi(
  label: string,
  metric: ReportsMetricWire
): ReportsKpiItem {
  const trendPercent = computeKpiTrendPercent(
    metric.value,
    metric.valuePrevious
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

function formatSubmittedDate(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return date.toLocaleDateString(undefined, {
    day: "numeric",
    month: "short",
  })
}

function mapFollowUpRow(
  row: ReportsFeedbackNeedsAttentionWire
): FeedbackReportFollowUpRow {
  return {
    feedbackId: row.feedbackId,
    date: formatSubmittedDate(row.submittedAt),
    guest: row.guestName,
    source: row.source,
    feedback: row.commentPreview,
    status: row.workflowStatus,
  }
}

function mapSourceRow(
  row: ReportsFeedbackBySourceWire
): FeedbackReportSourceRow {
  return {
    qrCodeId: row.qrCodeId,
    source: row.source,
    feedback: row.feedback,
    marketingOptIns: row.marketingOptIns,
    followUpNeeded: row.followUpNeeded,
  }
}

/** Map a ready Feedback report API body into page strips + tables. */
export function buildFeedbackReportViewModel(
  response: Extract<ReportsFeedbackResponse, { lifetimeEmpty: false }>
): FeedbackReportViewModel {
  return {
    topKpis: [
      metricToKpi("Feedback received", response.kpis.feedbackReceived),
      metricToKpi("Marketing opt-ins", response.kpis.marketingOptIns),
      metricToKpi("Follow-up needed", response.kpis.followUpNeeded),
      metricToKpi("Resolved", response.kpis.resolved),
    ],
    statusKpis: [
      metricToKpi("New", response.status.new),
      metricToKpi("In progress", response.status.inProgress),
      metricToKpi("Follow-up needed", response.status.followUpNeeded),
      metricToKpi("Resolved", response.status.resolved),
    ],
    followUpList: response.needsAttention.map(mapFollowUpRow),
    sourcesList: response.bySource.map(mapSourceRow),
  }
}
