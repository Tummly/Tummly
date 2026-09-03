import type { ReportsKpiItem } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_SUFFIX,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  ReportsMetricWire,
  ReportsOverviewCaptureSourceWire,
  ReportsOverviewResponse,
} from "@/types/operatorReports"

export const REPORTS_HUB_LOAD_ERROR_MESSAGE =
  "Could not load report data. Please try again."

export type ReportsOverviewViewModel = {
  funnelKpis: ReportsKpiItem[]
  privateFeedbackKpis: ReportsKpiItem[]
  offersKpis: ReportsKpiItem[]
  topCaptureSources: ReportsOverviewCaptureSourceWire[]
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

/** Map a ready overview API body into hub KPI strips + top sources. */
export function buildReportsOverviewViewModel(
  response: Extract<ReportsOverviewResponse, { lifetimeEmpty: false }>
): ReportsOverviewViewModel {
  return {
    funnelKpis: [
      metricToKpi("QR scans", response.funnel.qrScans),
      metricToKpi("Feedback received", response.funnel.feedbackReceived),
      metricToKpi("Marketing opt-ins", response.funnel.marketingOptIns),
      metricToKpi("Offer redemptions", response.funnel.offerRedemptions),
      metricToKpi("Campaigns sent", response.funnel.campaignsSent),
    ],
    privateFeedbackKpis: [
      metricToKpi(
        "Feedback messages",
        response.privateFeedback.feedbackMessages
      ),
      metricToKpi(
        "Marketing opt-ins",
        response.privateFeedback.marketingOptIns
      ),
      metricToKpi("Follow-up needed", response.privateFeedback.followUpNeeded),
      metricToKpi("Followed up", response.privateFeedback.followedUp),
    ],
    offersKpis: [
      metricToKpi("Active offers", response.offersAndCampaigns.activeOffers),
      metricToKpi("Offer claims", response.offersAndCampaigns.offerClaims),
      metricToKpi(
        "Offer redemptions",
        response.offersAndCampaigns.offerRedemptions
      ),
      metricToKpi("Campaigns sent", response.offersAndCampaigns.campaignsSent),
      metricToKpi("Unsubscribes", response.offersAndCampaigns.unsubscribes),
    ],
    topCaptureSources: response.topCaptureSources,
  }
}
