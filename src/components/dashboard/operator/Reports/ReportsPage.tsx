import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import { useStore } from "zustand"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_HUB_PAGE_COPY,
  REPORTS_INSIGHT_BANNER_CLASS,
  REPORTS_INSIGHT_BODY_CLASS,
  REPORTS_INSIGHT_TITLE_CLASS,
  REPORTS_TABLE_BODY_CELL_CLASS,
  REPORTS_TABLE_BODY_ROW_CLASS,
  REPORTS_TABLE_CLASS,
  REPORTS_TABLE_FRAME_CLASS,
  REPORTS_TABLE_HEAD_CELL_CLASS,
  REPORTS_TABLE_HEAD_ROW_CLASS,
  REPORTS_TABLE_NAME_CELL_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import {
  REPORTS_HUB_GUEST_LOOP_COPY,
  REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
} from "@/lib/operatorReports/reportsWeeklyBriefPresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardCaptureReportPath,
  operatorDashboardFeedbackReportPath,
  operatorDashboardOffersReportPath,
  operatorDashboardCampaignsReportPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type ReportsPageProps = {
  mode?: DashboardProps["mode"]
}

export function ReportsPage({ mode = "single" }: ReportsPageProps) {
  const navigate = useNavigate()
  const dashboardUiStore = useDashboardUiStoreApi()
  const setReportsDateRange = useStore(
    dashboardUiStore,
    (state) => state.setReportsDateRange
  )
  const reports = useReportsPageModule()
  const pageModule = useReportsPageModuleApi()
  const {
    hubLoadStatus,
    hubOverview,
    hubLoadError,
    weeklyBrief,
    exportAllowed,
    dateRange,
    selectedLocationId,
  } = reports.snapshot

  useEffect(() => {
    pageModule.setActiveSurface("hub")
  }, [pageModule])

  const locationId = selectedLocationId ?? 1

  const handleCommitRange = (range: HomePerformanceDateRange) => {
    setReportsDateRange(range)
    void reports.reloadForReportsDateRange()
  }

  const handleGenerateBrief = async () => {
    const ok = await reports.ensureWeeklyBriefReady()
    if (ok) {
      navigate(operatorDashboardWeeklyBriefPath(mode, locationId))
    }
  }

  const navTo = (
    destination: "feedback" | "capture" | "campaigns" | "offers"
  ) => {
    navigate(operatorDashboardNavPath(mode, destination, locationId))
  }

  const showDateRange = hubLoadStatus !== "lifetimeEmpty"

  return (
    <ReportsPageChrome
      title={REPORTS_HUB_PAGE_COPY.title}
      subtitle={REPORTS_HUB_PAGE_COPY.subtitle}
      actions={
        <ReportsStandardHeaderActions
          onGenerateBrief={() => {
            void handleGenerateBrief()
          }}
          generateBusy={weeklyBrief.generateBusy}
          onExport={() => reports.openExportDialog()}
          exportDisabled={!exportAllowed}
          selectedRange={dateRange}
          onCommitRange={handleCommitRange}
          showDateRange={showDateRange}
        />
      }
    >
      {hubLoadStatus === "loading" || hubLoadStatus === "idle" ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading reports"
        >
          <div
            className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
            aria-hidden
          />
        </div>
      ) : null}

      {hubLoadStatus === "error" ? (
        <div className="flex flex-col items-start gap-3" role="alert">
          <p className="m-0 text-sm text-destructive">
            {hubLoadError ?? "Could not load report data."}
          </p>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => {
              void reports.retryHubLoad()
            }}
          >
            Retry
          </Button>
        </div>
      ) : null}

      {hubLoadStatus === "lifetimeEmpty" ? <ReportsEmptyState /> : null}

      {hubLoadStatus === "ready" && hubOverview != null ? (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection title={REPORTS_HUB_GUEST_LOOP_COPY.sectionTitle}>
            {weeklyBrief.status === "loading" ? (
              <div
                className="flex min-h-20 items-center justify-center"
                role="status"
                aria-live="polite"
                aria-label="Loading weekly brief"
              >
                <div
                  className="size-6 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
                  aria-hidden
                />
              </div>
            ) : null}

            {weeklyBrief.status === "empty" ? (
              <div className="flex flex-col gap-3">
                <p className="m-0 text-sm text-op-text-muted">
                  {REPORTS_HUB_GUEST_LOOP_COPY.emptyHelper}
                </p>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="button"
                    variant="op-primary"
                    className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                    disabled={weeklyBrief.generateBusy}
                    onClick={() => {
                      void handleGenerateBrief()
                    }}
                  >
                    {REPORTS_HUB_GUEST_LOOP_COPY.generateBrief}
                  </Button>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                    onClick={() => navTo("campaigns")}
                  >
                    {REPORTS_HUB_GUEST_LOOP_COPY.createCampaign}
                  </Button>
                </div>
              </div>
            ) : null}

            {weeklyBrief.status === "error" ? (
              <div className="flex flex-col items-start gap-3" role="alert">
                <p className="m-0 text-sm text-destructive">
                  {weeklyBrief.errorMessage ??
                    REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE}
                </p>
                {weeklyBrief.errorRetryable ? (
                  <Button
                    type="button"
                    variant="op-secondary"
                    className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                    onClick={() => {
                      void reports.retryWeeklyBrief()
                    }}
                  >
                    {REPORTS_HUB_GUEST_LOOP_COPY.retry}
                  </Button>
                ) : null}
              </div>
            ) : null}

            {weeklyBrief.status === "ready" ? (
              <div className="flex flex-col gap-3">
                <div className={REPORTS_INSIGHT_BANNER_CLASS}>
                  <div className="flex min-w-0 flex-col gap-1">
                    <p className={REPORTS_INSIGHT_TITLE_CLASS}>
                      {weeklyBrief.headline}
                    </p>
                    {weeklyBrief.secondary ? (
                      <p className={REPORTS_INSIGHT_BODY_CLASS}>
                        {weeklyBrief.secondary}
                      </p>
                    ) : null}
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="button"
                    variant="op-primary"
                    className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                    onClick={() =>
                      navigate(
                        operatorDashboardWeeklyBriefPath(mode, locationId)
                      )
                    }
                  >
                    {REPORTS_HUB_GUEST_LOOP_COPY.viewWeeklyBrief}
                  </Button>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                    onClick={() => navTo("campaigns")}
                  >
                    {REPORTS_HUB_GUEST_LOOP_COPY.createCampaign}
                  </Button>
                </div>
              </div>
            ) : null}
          </ReportsSection>

          <ReportsSection
            title="Guest Loop funnel"
            subtitle="See where guests move from scan to feedback, contact and offer use."
          >
            <ReportsKpiStrip items={hubOverview.funnelKpis} />
          </ReportsSection>

          <ReportsSection
            title="Private feedback"
            subtitle="What guests told you through the feedback form."
          >
            <ReportsKpiStrip items={hubOverview.privateFeedbackKpis} />

            <div>
              <Button
                type="button"
                variant="op-primary"
                className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardFeedbackReportPath(mode, locationId)
                  )
                }
              >
                Open feedback report
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Top capture sources"
            subtitle="Which QR placements generated guest activity."
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Source
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Scans
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Marketing opt-ins
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {hubOverview.topCaptureSources.length === 0 ? (
                    <TableRow className={REPORTS_TABLE_BODY_ROW_CLASS}>
                      <TableCell
                        className={REPORTS_TABLE_BODY_CELL_CLASS}
                        colSpan={4}
                      >
                        No capture source activity in this period.
                      </TableCell>
                    </TableRow>
                  ) : (
                    hubOverview.topCaptureSources.map((row) => (
                      <TableRow
                        key={row.qrCodeId}
                        className={REPORTS_TABLE_BODY_ROW_CLASS}
                      >
                        <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                          {row.source}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.scans}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.feedback}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.marketingOptIns}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardCaptureReportPath(mode, locationId)
                  )
                }
              >
                View capture report
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Offers and campaigns"
            subtitle="Track claims, redemptions and campaign response."
          >
            <ReportsKpiStrip items={hubOverview.offersKpis} />

            <div className="flex flex-wrap items-center gap-3">
              <Button
                type="button"
                variant="op-primary"
                className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardOffersReportPath(mode, locationId)
                  )
                }
              >
                View offers report
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardCampaignsReportPath(mode, locationId)
                  )
                }
              >
                View campaign reports
              </Button>
            </div>
          </ReportsSection>
        </div>
      ) : null}
    </ReportsPageChrome>
  )
}
