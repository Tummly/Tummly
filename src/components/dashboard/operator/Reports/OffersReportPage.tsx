import { useState } from "react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import { ChevronRight, Download, ArrowUpRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import { ReportsDateRangeControl } from "@/components/dashboard/operator/Reports/ReportsDateRangeControl"
import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  OFFERS_REPORT_PAGE_COPY,
  mockOffersReportData,
  type OffersReportData,
} from "@/lib/operatorReports/offersReportPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { cn } from "@/lib/utils"

type OffersReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
  data?: OffersReportData
}

export function OffersReportPage({
  selectedLocationId = 1,
  selectedLocationName = "Mehmet's Grill",
  locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockOffersReportData,
}: OffersReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [dateRange, setDateRange] = useState<HomePerformanceDateRange>(
    DEFAULT_HOME_PERFORMANCE_DATE_RANGE
  )
  const [isExportOpen, setIsExportOpen] = useState(false)

  const isPageEmpty = propIsEmpty ?? (searchParams.get("empty") === "true")
  const dateRangeLabel = labelForHomePerformanceDateRange(dateRange)

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleCreateOffer = () => {
    navigate(operatorDashboardNavPath(mode, "offers", selectedLocationId))
  }

  const handleControlSignalAction = (
    target: "redemption-log" | "offers" | "overrides"
  ) => {
    if (target === "redemption-log" || target === "overrides") {
      navigate(
        operatorDashboardOffersRedemptionLogPath(mode, selectedLocationId)
      )
    } else {
      navigate(operatorDashboardNavPath(mode, "offers", selectedLocationId))
    }
  }

  const kpisList = [
    data.kpis.activeOffers,
    data.kpis.offerClaims,
    data.kpis.redemptions,
    data.kpis.redemptionRate,
    data.kpis.expiredClaims,
    data.kpis.invalidAttempts,
  ]

  return (
    <div className="w-full flex flex-col gap-6">
      {/* 1. Breadcrumb Navigation */}
      <nav aria-label="Breadcrumb" className="inline-flex items-center gap-2.5">
        <Link
          to={reportsBasePath}
          className="text-base font-medium text-op-text-primary hover:text-op-text-primary/80 transition-colors"
        >
          {OFFERS_REPORT_PAGE_COPY.breadcrumbReports}
        </Link>
        <ChevronRight className="size-4 text-op-text-muted shrink-0" />
        <span className="text-base font-medium text-op-text-muted">
          {OFFERS_REPORT_PAGE_COPY.breadcrumbOffersReport}
        </span>
      </nav>

      {/* 2. Page Header Row */}
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div className="flex flex-col gap-1">
          <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
            {OFFERS_REPORT_PAGE_COPY.pageTitle}
          </h1>
          <p className="text-sm sm:text-base font-medium text-op-text-muted">
            {OFFERS_REPORT_PAGE_COPY.pageSubtitle}
          </p>
        </div>

        {/* Header Action Buttons */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Generate Brief Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() =>
              navigate(
                operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
              )
            }
          >
            <img
              src={aiIconPng}
              alt=""
              className="size-4 shrink-0 brightness-0 invert"
            />
            <span>{OFFERS_REPORT_PAGE_COPY.generateBrief}</span>
          </Button>

          {/* Export Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() => setIsExportOpen(true)}
          >
            <Download className="size-4" />
            <span>{OFFERS_REPORT_PAGE_COPY.export}</span>
          </Button>

          {/* Date Range Selector */}
          <ReportsDateRangeControl
            selectedRange={dateRange}
            onCommitRange={setDateRange}
          />
        </div>
      </div>

      {isPageEmpty ? (
        /* Empty State View */
        <div className="flex w-full min-h-[400px] flex-col items-center justify-center gap-3 py-20 text-center rounded-md border border-op-border-default bg-op-card-background">
          <h2 className="text-lg font-bold text-op-text-primary">
            {OFFERS_REPORT_PAGE_COPY.emptyTitle}
          </h2>
          <p className="max-w-md text-sm font-medium leading-relaxed text-op-text-muted">
            {OFFERS_REPORT_PAGE_COPY.emptySubtitle}
          </p>
          <Button
            type="button"
            variant="op-primary"
            className="mt-2 h-10 px-5 rounded-xs text-sm font-medium"
            onClick={handleCreateOffer}
          >
            {OFFERS_REPORT_PAGE_COPY.createOffer}
          </Button>
        </div>
      ) : (
        /* Populated Offers Report View */
        <div className="flex flex-col gap-6">
          {/* Card 1: 6-KPI Strip */}
          <div className="w-full rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 divide-y sm:divide-y-0 sm:divide-x divide-op-border-default">
              {kpisList.map((kpi, index) => (
                <div
                  key={kpi.label}
                  className={cn(
                    "flex flex-col justify-between gap-1 py-3 sm:py-2",
                    index === 0 ? "sm:pl-2" : "sm:px-4",
                    index === kpisList.length - 1 ? "sm:pr-2" : ""
                  )}
                >
                  <span className="text-sm font-medium text-op-text-muted">
                    {kpi.label}
                  </span>
                  <span className="text-3xl font-extrabold text-op-text-primary leading-9 tracking-tight">
                    {kpi.value}
                  </span>
                  <div className="flex items-center gap-1 text-xs font-semibold text-op-kpi-info-color pt-0.5">
                    <ArrowUpRight className="size-3.5 shrink-0" />
                    <span>{kpi.delta}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Card 2: Offer Performance Table */}
          <div className="w-full flex flex-col gap-5 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {OFFERS_REPORT_PAGE_COPY.performanceSectionTitle}
            </h2>

            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-neutral-800 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[200px]">
                      {OFFERS_REPORT_PAGE_COPY.offerHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[140px]">
                      {OFFERS_REPORT_PAGE_COPY.sourceHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[130px]">
                      {OFFERS_REPORT_PAGE_COPY.statusHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[90px]">
                      {OFFERS_REPORT_PAGE_COPY.claimsHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[110px]">
                      {OFFERS_REPORT_PAGE_COPY.redemptionsHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[80px]">
                      {OFFERS_REPORT_PAGE_COPY.rateHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[90px]">
                      {OFFERS_REPORT_PAGE_COPY.expiredHeader}
                    </th>
                    <th className="px-4 py-3 min-w-[90px]">
                      {OFFERS_REPORT_PAGE_COPY.invalidHeader}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.performance.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.offer}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.source}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default">
                        <span className="inline-flex items-center px-2 py-1 rounded-xs text-xs font-medium bg-neutral-800 text-op-text-primary border border-op-border-default">
                          {row.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary font-normal">
                        {row.claims}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary font-normal">
                        {row.redemptions}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary font-normal">
                        {row.rate}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary font-normal">
                        {row.expired}
                      </td>
                      <td className="px-4 py-3 text-op-text-primary font-normal">
                        {row.invalid}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Card 3: Recent Redemptions Table */}
          <div className="w-full flex flex-col gap-5 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {OFFERS_REPORT_PAGE_COPY.recentRedemptionsSectionTitle}
            </h2>

            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-neutral-800 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[120px]">
                      {OFFERS_REPORT_PAGE_COPY.dateHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[200px]">
                      {OFFERS_REPORT_PAGE_COPY.offerHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[140px]">
                      {OFFERS_REPORT_PAGE_COPY.guestHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[160px]">
                      {OFFERS_REPORT_PAGE_COPY.locationHeader}
                    </th>
                    <th className="px-4 py-3 min-w-[120px]">
                      {OFFERS_REPORT_PAGE_COPY.statusHeader}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.redemptionsList.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.date}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.offer}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.guest}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-muted">
                        {row.location}
                      </td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center px-2 py-1 rounded-xs text-xs font-medium bg-neutral-800 text-op-text-primary border border-op-border-default">
                          {row.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Card 4: Offer Control Signals */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex items-center gap-3 pb-4 border-b border-op-border-default">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 invert"
              />
              <h2 className="text-xl font-bold text-op-text-primary">
                {OFFERS_REPORT_PAGE_COPY.controlSignalsSectionTitle}
              </h2>
            </div>

            <div className="flex flex-col gap-4">
              {data.controlSignals.map((signal) => (
                <div
                  key={signal.id}
                  className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col gap-5"
                >
                  <div className="flex flex-col gap-1">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      {signal.title}
                    </h3>
                    <p className="text-sm font-normal text-op-text-muted leading-relaxed">
                      {signal.subtitle}
                    </p>
                  </div>

                  <div>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className="h-10 px-4 rounded-xs text-sm font-medium"
                      onClick={() => handleControlSignalAction(signal.target)}
                    >
                      {signal.cta}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Export Dialog */}
      <ReportsExportDialog
        open={isExportOpen}
        onOpenChange={setIsExportOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
      />
    </div>
  )
}
