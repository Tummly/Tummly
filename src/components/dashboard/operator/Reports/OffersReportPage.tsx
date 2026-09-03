import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsInsightBanner } from "@/components/dashboard/operator/Reports/ReportsInsightBanner"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsChildChrome } from "@/components/dashboard/operator/Reports/utils/useReportsChildChrome"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import { OFFERS_REPORT_PAGE_COPY } from "@/lib/operatorReports/offersReportPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_TABLE_BODY_CELL_CLASS,
  REPORTS_TABLE_BODY_ROW_CLASS,
  REPORTS_TABLE_CLASS,
  REPORTS_TABLE_FRAME_CLASS,
  REPORTS_TABLE_HEAD_CELL_CLASS,
  REPORTS_TABLE_HEAD_ROW_CLASS,
  REPORTS_TABLE_NAME_CELL_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type OffersReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
}

export function OffersReportPage({
  selectedLocationId = 1,
  selectedLocationName: _selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
}: OffersReportPageProps) {
  const navigate = useNavigate()
  const reportsChrome = useReportsChildChrome("offers", mode)
  const reports = useReportsPageModule()
  const {
    dateRange,
    exportAllowed,
    generateBusy,
    openExportDialog,
    commitRange,
    onGenerateBrief,
  } = reportsChrome
  const { offersLoadStatus, offersReport, offersLoadError } = reports.snapshot

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleCreateOffer = () => {
    navigate(operatorDashboardNavPath(mode, "offers", selectedLocationId))
  }

  const handleControlSignalAction = (target: "redemption-log" | "offers") => {
    if (target === "redemption-log") {
      navigate(
        operatorDashboardOffersRedemptionLogPath(mode, selectedLocationId)
      )
      return
    }
    navigate(operatorDashboardNavPath(mode, "offers", selectedLocationId))
  }

  const showDateRange = offersLoadStatus !== "lifetimeEmpty"
  const kpisList =
    offersReport == null
      ? []
      : [
          offersReport.kpis.activeOffers,
          offersReport.kpis.offerClaims,
          offersReport.kpis.redemptions,
          offersReport.kpis.redemptionRate,
          offersReport.kpis.expiredClaims,
          offersReport.kpis.invalidAttempts,
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
          onGenerateBrief={onGenerateBrief}
          generateBusy={generateBusy}
          onExport={openExportDialog}
          exportDisabled={!exportAllowed}
          selectedRange={dateRange}
          onCommitRange={commitRange}
          showDateRange={showDateRange}
        />
      }
    >
      {offersLoadStatus === "loading" || offersLoadStatus === "idle" ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading offers report"
        >
          <Spinner />
        </div>
      ) : null}

      {offersLoadStatus === "error" ? (
        <div className="flex flex-col items-start gap-3" role="alert">
          <p className="m-0 text-sm text-destructive">
            {offersLoadError ?? "Could not load report data."}
          </p>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => {
              void reports.retryOffersLoad()
            }}
          >
            Retry
          </Button>
        </div>
      ) : null}

      {offersLoadStatus === "lifetimeEmpty" ? (
        <ReportsEmptyState
          title={OFFERS_REPORT_PAGE_COPY.emptyTitle}
          subtitle={OFFERS_REPORT_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={handleCreateOffer}
            >
              {OFFERS_REPORT_PAGE_COPY.createOffer}
            </Button>
          }
        />
      ) : null}

      {offersLoadStatus === "ready" && offersReport != null ? (
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
                  {offersReport.performance.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.offer}
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
                  {offersReport.redemptionsList.map((row) => (
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
              {offersReport.controlSignals.map((signal) => (
                <div key={signal.id} className="flex flex-col gap-4">
                  <ReportsInsightBanner title={signal.title}>
                    {signal.subtitle}
                  </ReportsInsightBanner>
                  <div>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
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
      ) : null}
    </ReportsPageChrome>
  )
}
