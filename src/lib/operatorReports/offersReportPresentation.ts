/**
 * Figma Operator Reports — Offers report sub-page.
 */

import type { ReportsKpiItem } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_SUFFIX,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  ReportsMetricWire,
  ReportsOffersControlSignalWire,
  ReportsOffersResponse,
  ReportsRateMetricWire,
} from "@/types/operatorReports"

export const OFFERS_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbOffersReport: "Offers report",
  title: "Offers report",
  pageTitle: "Offers report",
  subtitle: "Track offer claims, redemptions, expiry and usage controls.",
  pageSubtitle: "Track offer claims, redemptions, expiry and usage controls.",
  generateBrief: "Generate brief",
  export: "Export",

  // Empty state copy
  emptyTitle: "No offer reports yet",
  emptySubtitle:
    "Create a controlled offer with expiry and redemption rules to track claims and redemptions.",
  createOffer: "Create offer",

  // Section titles
  performanceSectionTitle: "Offer performance",
  recentRedemptionsSectionTitle: "Recent redemptions",
  controlSignalsSectionTitle: "Offer control signals",

  // Table headers
  offerHeader: "Offer",
  statusHeader: "Status",
  claimsHeader: "Claims",
  redemptionsHeader: "Redemptions",
  rateHeader: "Rate",
  expiredHeader: "Expired",
  invalidHeader: "Invalid",
  dateHeader: "Date",
  guestHeader: "Guest",
  locationHeader: "Location",

  // CTAs
  viewRedemptionLog: "View redemption log",
  reviewOffer: "Review offer",
  loadError: "Could not load offers report. Please try again.",
  retry: "Retry",
} as const

export type OffersReportKpi = ReportsKpiItem

export type OffersReportPerformanceRow = {
  id: string
  offer: string
  status: string
  claims: number
  redemptions: number
  rate: string
  expired: number
  invalid: number
}

export type OffersReportRedemptionRow = {
  id: string
  date: string
  offer: string
  guest: string
  location: string
  status: string
}

export type OffersReportControlSignal = {
  id: string
  title: string
  subtitle: string
  cta: string
  target: "redemption-log" | "offers"
}

export type OffersReportData = {
  kpis: {
    activeOffers: OffersReportKpi
    offerClaims: OffersReportKpi
    redemptions: OffersReportKpi
    redemptionRate: OffersReportKpi
    expiredClaims: OffersReportKpi
    invalidAttempts: OffersReportKpi
  }
  performance: OffersReportPerformanceRow[]
  redemptionsList: OffersReportRedemptionRow[]
  controlSignals: OffersReportControlSignal[]
}

function metricToKpi(
  label: string,
  metric: ReportsMetricWire
): OffersReportKpi {
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

function formatRatePercent(rate: number | null): string {
  if (rate == null) {
    return "—"
  }
  return `${Math.round(rate * 100)}%`
}

function rateMetricToKpi(
  label: string,
  metric: ReportsRateMetricWire
): OffersReportKpi {
  const current = metric.value
  const previous = metric.valuePrevious
  const trendPercent =
    current == null || previous == null
      ? null
      : computeKpiTrendPercent(current, previous)
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
    value: formatRatePercent(current),
    delta: `${formatKpiTrendPercentValue(trendPercent)}% ${PERFORMANCE_KPI_TREND_SUFFIX}`,
    positive,
  }
}

function formatCatalogStatus(status: string): string {
  if (status.length === 0) {
    return status
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

function formatRedemptionDate(dateTimeUtc: string): string {
  const parsed = new Date(dateTimeUtc)
  if (Number.isNaN(parsed.getTime())) {
    return dateTimeUtc
  }
  return parsed.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
  })
}

function mapControlSignal(
  signal: ReportsOffersControlSignalWire
): OffersReportControlSignal {
  if (signal.kind === "repeated-invalid") {
    return {
      id: signal.kind,
      title: "Repeated invalid attempts",
      subtitle: `${signal.count} attempts this period were already-used or expired offers.`,
      cta: OFFERS_REPORT_PAGE_COPY.viewRedemptionLog,
      target: "redemption-log",
    }
  }

  return {
    id: signal.kind,
    title: "High claims, lower redemptions",
    subtitle: `The ${signal.offerTitle} offer had ${signal.claims} claims and ${signal.redemptions} redemptions.`,
    cta: OFFERS_REPORT_PAGE_COPY.reviewOffer,
    target: "offers",
  }
}

/** Map a ready Offers report API body into the Offers report view model. */
export function buildOffersReportViewModel(
  response: Extract<ReportsOffersResponse, { lifetimeEmpty: false }>
): OffersReportData {
  return {
    kpis: {
      activeOffers: metricToKpi("Active offers", response.kpis.activeOffers),
      offerClaims: metricToKpi("Offer claims", response.kpis.offerClaims),
      redemptions: metricToKpi("Redemptions", response.kpis.redemptions),
      redemptionRate: rateMetricToKpi(
        "Redemption rate",
        response.kpis.redemptionRate
      ),
      expiredClaims: metricToKpi("Expired claims", response.kpis.expiredClaims),
      invalidAttempts: metricToKpi(
        "Invalid attempts",
        response.kpis.invalidAttempts
      ),
    },
    performance: response.performance.map((row) => ({
      id: String(row.offerId),
      offer: row.offer,
      status: formatCatalogStatus(row.status),
      claims: row.claims,
      redemptions: row.redemptions,
      rate: formatRatePercent(row.rate),
      expired: row.expired,
      invalid: row.invalid,
    })),
    redemptionsList: response.recentRedemptions.map((row) => ({
      id: String(row.id),
      date: formatRedemptionDate(row.dateTimeUtc),
      offer: row.offerTitle,
      guest: row.guestName,
      location: row.locationName,
      status: "Redeemed",
    })),
    controlSignals: response.controlSignals.map(mapControlSignal),
  }
}
