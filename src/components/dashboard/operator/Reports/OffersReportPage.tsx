import { useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import { ReportsInsightBanner } from "@/components/dashboard/operator/Reports/ReportsInsightBanner"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
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
  REPORTS_BODY_STACK_CLASS,
  REPORTS_PAGE_ACTION_BUTTON_CLASS,
  REPORTS_TABLE_BODY_CELL_CLASS,
  REPORTS_TABLE_BODY_ROW_CLASS,
  REPORTS_TABLE_CLASS,
  REPORTS_TABLE_FRAME_CLASS,
  REPORTS_TABLE_HEAD_CELL_CLASS,
  REPORTS_TABLE_HEAD_ROW_CLASS,
  REPORTS_TABLE_NAME_CELL_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

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
  locations: _locations = [],
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

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"
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
    <ReportsPageChrome
      title={OFFERS_REPORT_PAGE_COPY.pageTitle}
      subtitle={OFFERS_REPORT_PAGE_COPY.pageSubtitle}
      breadcrumb={{
        reportsBasePath,
        currentLabel: OFFERS_REPORT_PAGE_COPY.breadcrumbOffersReport,
      }}
      actions={
        <ReportsStandardHeaderActions
          onGenerateBrief={() =>
            navigate(
              operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
            )
          }
          onExport={() => setIsExportOpen(true)}
          selectedRange={dateRange}
          onCommitRange={setDateRange}
        />
      }
    >
      {isPageEmpty ? (
        <ReportsEmptyState
          title={OFFERS_REPORT_PAGE_COPY.emptyTitle}
          subtitle={OFFERS_REPORT_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
              onClick={handleCreateOffer}
            >
              {OFFERS_REPORT_PAGE_COPY.createOffer}
            </Button>
          }
        />
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={kpisList} />
          </ReportsSection>

          <ReportsSection
            title={OFFERS_REPORT_PAGE_COPY.performanceSectionTitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.offerHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.sourceHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.statusHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.claimsHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.redemptionsHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.rateHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.expiredHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.invalidHeader}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.performance.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.offer}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.source}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.status} />
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.claims}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.redemptions}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.rate}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.expired}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.invalid}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </ReportsSection>

          <ReportsSection
            title={OFFERS_REPORT_PAGE_COPY.recentRedemptionsSectionTitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.dateHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.offerHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.guestHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.locationHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {OFFERS_REPORT_PAGE_COPY.statusHeader}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.redemptionsList.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.date}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.offer}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.guest}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.location}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </ReportsSection>

          <ReportsSection
            title={OFFERS_REPORT_PAGE_COPY.controlSignalsSectionTitle}
          >
            <div className="flex flex-col gap-4">
              {data.controlSignals.map((signal) => (
                <div key={signal.id} className="flex flex-col gap-4">
                  <ReportsInsightBanner title={signal.title}>
                    {signal.subtitle}
                  </ReportsInsightBanner>
                  <div>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                      onClick={() => handleControlSignalAction(signal.target)}
                    >
                      {signal.cta}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </ReportsSection>
        </div>
      )}

      <ReportsExportDialog
        open={isExportOpen}
        onOpenChange={setIsExportOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
      />
    </ReportsPageChrome>
  )
}
