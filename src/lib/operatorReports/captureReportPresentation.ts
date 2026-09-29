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
  createQr: "Create QR",
  funnelSectionTitle: "Scan-to-guest funnel",
  reviewGuestForm: "Review guest form",
  placementSectionTitle: "QR placement performance",
  placementInsightTitle: "Placement insight",
  createPlacement: "Create another QR placement",
  actionsMenuLabel: "Actions",
  viewPlacement: "View QR placement",
  editDetails: "Edit details",
  downloadQr: "Download QR code",
} as const

/** Same floor as Weekly Brief underperform-qr (strongest peer scans). */
export const CAPTURE_PLACEMENT_INSIGHT_MIN_PEER_SCANS = 5

/** Same ratio as Weekly Brief underperform-qr (worst ≤ best × ratio). */
export const CAPTURE_PLACEMENT_INSIGHT_MAX_SCAN_RATIO = 0.5

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
  /** Largest numeric drop-off between consecutive funnel steps; null when none. */
  funnelInsight: string | null
  placements: CaptureReportPlacementRow[]
  /** Clear underperformer vs strongest peer; null when no clear signal. */
  placementInsight: string | null
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

/**
 * Largest numeric drop-off between consecutive funnel steps.
 * Names the prior → current step pair in plain English.
 */
export function deriveCaptureFunnelInsight(
  funnel: readonly CaptureReportFunnelStep[]
): string | null {
  let bestIndex = -1
  let bestDrop = 0
  for (let i = 1; i < funnel.length; i += 1) {
    const value = funnel[i]?.dropOff
    if (typeof value !== "number" || value <= bestDrop) {
      continue
    }
    bestDrop = value
    bestIndex = i
  }
  if (bestIndex < 1 || bestDrop <= 0) {
    return null
  }
  const prior = funnel[bestIndex - 1]?.step
  const current = funnel[bestIndex]?.step
  if (prior == null || current == null) {
    return null
  }
  return `Most drop-off happened between ${prior} and ${current}.`
}

/**
 * Names a clear underperformer when strongest peer has enough scans and the
 * worst is at or below half of strongest (and strictly below strongest).
 */
export function deriveCapturePlacementInsight(
  placements: readonly CaptureReportPlacementRow[]
): string | null {
  if (placements.length < 2) {
    return null
  }
  let best = placements[0]!
  let worst = placements[0]!
  for (const row of placements) {
    if (row.scans > best.scans) {
      best = row
    }
    if (
      row.scans < worst.scans
      || (row.scans === worst.scans && row.contactable < worst.contactable)
    ) {
      worst = row
    }
  }
  if (best.scans < CAPTURE_PLACEMENT_INSIGHT_MIN_PEER_SCANS) {
    return null
  }
  const maxAllowed = Math.floor(
    best.scans * CAPTURE_PLACEMENT_INSIGHT_MAX_SCAN_RATIO
  )
  if (worst.id === best.id || worst.scans > maxAllowed) {
    return null
  }
  return `${worst.qrName} had ${worst.scans} scans this period — well below your strongest placement.`
}

/** Map a ready Capture API body into KPIs, funnel steps, and placements. */
export function buildReportsCaptureViewModel(
  response: Extract<ReportsCaptureResponse, { lifetimeEmpty: false }>
): CaptureReportViewModel {
  const scans = response.funnel.qrScans.value
  const feedback = response.funnel.feedbackSubmitted.value
  const contactable = response.funnel.contactableGuests.value
  const claimed = response.funnel.offerClaimed.value

  const funnel: CaptureReportFunnelStep[] = [
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
  ]

  const placements = response.placements.map((row) => ({
    id: String(row.qrCodeId),
    qrName: row.name,
    placement: row.name,
    status: row.status,
    scans: row.scans,
    feedback: row.feedback,
    contactable: row.contactable,
    conversion: conversionLabel(row.feedback, row.scans),
  }))

  return {
    funnelKpis: [
      metricToKpi("QR scans", response.funnel.qrScans),
      metricToKpi("Feedback submitted", response.funnel.feedbackSubmitted),
      metricToKpi("Contactable guests", response.funnel.contactableGuests),
      metricToKpi("Offer claimed", response.funnel.offerClaimed),
    ],
    funnel,
    funnelInsight: deriveCaptureFunnelInsight(funnel),
    placements,
    placementInsight: deriveCapturePlacementInsight(placements),
  }
}
