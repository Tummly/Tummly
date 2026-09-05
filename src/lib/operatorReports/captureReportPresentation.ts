/**
 * Operator Reports — Capture report (live GET /api/reports/capture).
 */

import type { ReportsKpiItem } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_SUFFIX,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  ReportsCaptureResponse,
  ReportsMetricWire,
} from "@/types/operatorReports"

export const CAPTURE_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbCaptureReport: "Capture report",
  title: "Capture report",
  pageTitle: "Capture report",
  subtitle:
    "See which QR codes and placements turn scans into guest feedback and contactable guests.",
  pageSubtitle:
    "See which QR codes and placements turn scans into guest feedback and contactable guests.",
  generateBrief: "Generate brief",
  export: "Export",
  emptyTitle: "No QR activity yet",
  emptySubtitle:
    "Reports will appear once guests start scanning your QR codes or Smart Guest Links.",
  funnelSectionTitle: "Scan-to-guest funnel",
  funnelInsight:
    "Most drop-off happened between QR scans and submitted feedback. Review the form length, offer wording and page load speed.",
  reviewGuestForm: "Review guest form",
  placementSectionTitle: "QR placement performance",
  placementInsightTitle: "Placement insight",
  placementInsightSubtitle:
    "Your quiet-day offer had the most redemptions this period. One campaign caused more opt-outs than usual, so review the audience before sending again.",
  createPlacement: "Create another QR placement",
  actionsMenuLabel: "Actions",
  viewPlacement: "View QR placement",
  editDetails: "Edit details",
  downloadQr: "Download QR code",
} as const

export const REPORTS_CAPTURE_LOAD_ERROR_MESSAGE =
  "Could not load report data. Please try again."

export type CaptureReportKpi = ReportsKpiItem

export type CaptureReportFunnelStep = {
  step: string
  count: number
  dropOff: number | "—"
}

export type CaptureReportPlacementRow = {
  id: string
  qrName: string
  /** Same as qrName — kept for placement-action modal chrome. */
  placement: string
  status: "Active" | "Paused"
  scans: number
  feedback: number
  contactable: number
  conversion: string
}

export type CaptureReportViewModel = {
  funnelKpis: ReportsKpiItem[]
  funnel: CaptureReportFunnelStep[]
  placements: CaptureReportPlacementRow[]
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

function conversionLabel(feedback: number, scans: number): string {
  if (scans === 0) {
    return "—"
  }
  return `${Math.round((feedback / scans) * 100)}%`
}

function dropOff(priorCount: number, currentCount: number): number {
  return Math.max(0, priorCount - currentCount)
}

/** Map a ready Capture API body into KPIs, funnel steps, and placements. */
export function buildReportsCaptureViewModel(
  response: Extract<ReportsCaptureResponse, { lifetimeEmpty: false }>
): CaptureReportViewModel {
  const scans = response.funnel.qrScans.value
  const feedback = response.funnel.feedbackSubmitted.value
  const contactable = response.funnel.contactableGuests.value
  const claimed = response.funnel.offerClaimed.value

  return {
    funnelKpis: [
      metricToKpi("QR scans", response.funnel.qrScans),
      metricToKpi("Feedback submitted", response.funnel.feedbackSubmitted),
      metricToKpi("Contactable guests", response.funnel.contactableGuests),
      metricToKpi("Offer claimed", response.funnel.offerClaimed),
    ],
    funnel: [
      { step: "QR scans", count: scans, dropOff: "—" },
      {
        step: "Feedback submitted",
        count: feedback,
        dropOff: dropOff(scans, feedback),
      },
      {
        step: "Contactable guests",
        count: contactable,
        dropOff: dropOff(feedback, contactable),
      },
      {
        step: "Offer claimed",
        count: claimed,
        dropOff: dropOff(contactable, claimed),
      },
    ],
    placements: response.placements.map((row) => ({
      id: String(row.qrCodeId),
      qrName: row.name,
      placement: row.name,
      status: row.status,
      scans: row.scans,
      feedback: row.feedback,
      contactable: row.contactable,
      conversion: conversionLabel(row.feedback, row.scans),
    })),
  }
}
