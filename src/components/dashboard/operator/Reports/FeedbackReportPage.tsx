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
import { ReportsInsightBanner } from "@/components/dashboard/operator/Reports/ReportsInsightBanner"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsChildChrome } from "@/components/dashboard/operator/Reports/utils/useReportsChildChrome"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import {
  FEEDBACK_REPORT_PAGE_COPY,
  mockFeedbackReportData,
  type FeedbackReportData,
} from "@/lib/operatorReports/feedbackReportPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_TABLE_ACTIONS_CELL_CLASS,
  REPORTS_TABLE_BODY_CELL_CLASS,
  REPORTS_TABLE_BODY_ROW_CLASS,
  REPORTS_TABLE_CLASS,
  REPORTS_TABLE_FRAME_CLASS,
  REPORTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
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
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type FeedbackReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
  data?: FeedbackReportData
}

export function FeedbackReportPage({
  selectedLocationId = 1,
  selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockFeedbackReportData,
}: FeedbackReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const reportsChrome = useReportsChildChrome("feedback")
  const { dateRange, exportAllowed, openExportDialog, commitRange } = reportsChrome

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleOpenFeedbackInbox = () => {
    navigate(operatorDashboardNavPath(mode, "feedback", selectedLocationId))
  }

  const topKpisList = [
    data.kpis.feedbackReceived,
    data.kpis.contactableFeedback,
    data.kpis.followUpNeeded,
    data.kpis.followedUp,
    data.kpis.resolved,
  ]

  const statusKpisList = [
    data.statusKpis.newFeedback,
    data.statusKpis.reviewed,
    data.statusKpis.followUpNeeded,
    data.statusKpis.followedUp,
    data.statusKpis.resolved,
  ]

  return (
    <ReportsPageChrome
      title={FEEDBACK_REPORT_PAGE_COPY.title}
      subtitle={FEEDBACK_REPORT_PAGE_COPY.subtitle}
      breadcrumb={{
        reportsBasePath,
        currentLabel: FEEDBACK_REPORT_PAGE_COPY.breadcrumbFeedbackReport,
      }}
      actions={
        <ReportsStandardHeaderActions
          onGenerateBrief={() =>
            navigate(
              operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
            )
          }
          onExport={openExportDialog}
          exportDisabled={!exportAllowed}
          selectedRange={dateRange}
          onCommitRange={commitRange}
        />
      }
    >
      {isPageEmpty ? (
        <ReportsEmptyState
          title={FEEDBACK_REPORT_PAGE_COPY.emptyTitle}
          subtitle={FEEDBACK_REPORT_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={handleOpenFeedbackInbox}
            >
              {FEEDBACK_REPORT_PAGE_COPY.checkGuestForm}
            </Button>
          }
        />
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={topKpisList} />
          </ReportsSection>

          <ReportsSection
            title={FEEDBACK_REPORT_PAGE_COPY.feedbackOverTimeTitle}
            subtitle={FEEDBACK_REPORT_PAGE_COPY.feedbackOverTimeSubtitle}
          >
            <div className="relative flex h-72 w-full flex-col justify-between overflow-hidden rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-6">
              <div className="pointer-events-none absolute inset-x-6 inset-y-6 flex flex-col justify-between opacity-20">
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
              </div>

              <div className="z-10 flex w-full flex-1 items-end justify-between px-4 pb-2">
                {[
                  { day: "Mon", count: 6, height: "35%" },
                  { day: "Tue", count: 8, height: "45%" },
                  { day: "Wed", count: 12, height: "65%" },
                  { day: "Thu", count: 9, height: "50%" },
                  { day: "Fri", count: 18, height: "90%" },
                  { day: "Sat", count: 15, height: "75%" },
                  { day: "Sun", count: 10, height: "55%" },
                ].map((bar) => (
                  <div
                    key={bar.day}
                    className="group flex h-full cursor-pointer flex-col items-center justify-end gap-2"
                  >
                    <div className="text-[11px] font-semibold text-op-action-primary opacity-0 transition-opacity group-hover:opacity-100">
                      {bar.count}
                    </div>
                    <div
                      className="w-8 rounded-t-xs bg-op-action-primary/80 transition-all duration-300 hover:bg-op-action-primary sm:w-12"
                      style={{ height: bar.height }}
                    />
                    <span className="text-xs font-medium text-op-text-muted">
                      {bar.day}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </ReportsSection>

          <ReportsSection title={FEEDBACK_REPORT_PAGE_COPY.commonThemesTitle}>
            <div className="flex flex-col gap-3">
              {data.themes.map((theme) => (
                <div
                  key={theme.id}
                  className="flex w-full flex-col items-start justify-start gap-4 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-5"
                >
                  <p className="max-w-4xl text-sm font-normal leading-relaxed text-op-text-primary">
                    {theme.theme}
                  </p>
                  <p className="text-xs font-normal leading-relaxed text-op-text-muted">
                    {theme.meta}
                  </p>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                    onClick={handleOpenFeedbackInbox}
                  >
                    {FEEDBACK_REPORT_PAGE_COPY.viewSourceFeedback}
                  </Button>
                </div>
              ))}
            </div>
          </ReportsSection>

          <ReportsSection
            title={FEEDBACK_REPORT_PAGE_COPY.needsFollowUpTitle}
            subtitle={FEEDBACK_REPORT_PAGE_COPY.needsFollowUpSubtitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Date
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Guest
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Source
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Reason
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Status
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                      Actions
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.followUpList.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.date}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.guest}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.source}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.feedback}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.reason}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.status} />
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_ACTIONS_CELL_CLASS}>
                        <Button
                          type="button"
                          variant="op-tertiary"
                          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                          onClick={handleOpenFeedbackInbox}
                        >
                          {FEEDBACK_REPORT_PAGE_COPY.openAction}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={handleOpenFeedbackInbox}
            >
              {FEEDBACK_REPORT_PAGE_COPY.openFeedbackInbox}
            </Button>
          </ReportsSection>

          <ReportsSection
            title={FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceTitle}
            subtitle={FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceSubtitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Source
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Contactable
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Follow-up needed
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.sourcesList.map((row) => (
                    <TableRow
                      key={row.source}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.source}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.feedback}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.contactable}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.followUpNeeded}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <ReportsInsightBanner>
              {FEEDBACK_REPORT_PAGE_COPY.sourceInsightText}
            </ReportsInsightBanner>
          </ReportsSection>

          <ReportsSection title={FEEDBACK_REPORT_PAGE_COPY.feedbackStatusTitle}>
            <ReportsKpiStrip items={statusKpisList} />

            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={handleOpenFeedbackInbox}
            >
              {FEEDBACK_REPORT_PAGE_COPY.manageFeedback}
            </Button>
          </ReportsSection>
        </div>
      )}

    </ReportsPageChrome>
  )
}
