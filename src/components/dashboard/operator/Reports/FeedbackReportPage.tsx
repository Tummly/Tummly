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
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsChildChrome } from "@/components/dashboard/operator/Reports/utils/useReportsChildChrome"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import { FEEDBACK_REPORT_PAGE_COPY } from "@/lib/operatorReports/feedbackReportPresentation"
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
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type FeedbackReportPageProps = {
  mode?: DashboardProps["mode"]
}

export function FeedbackReportPage({
  mode = "single",
}: FeedbackReportPageProps) {
  const navigate = useNavigate()
  const reportsChrome = useReportsChildChrome("feedback", mode)
  const reports = useReportsPageModule()
  const {
    dateRange,
    exportAllowed,
    generateBusy,
    openExportDialog,
    commitRange,
    onGenerateBrief,
  } = reportsChrome
  const {
    feedbackLoadStatus,
    feedbackReport,
    feedbackLoadError,
    selectedLocationId,
  } = reports.snapshot

  const locationId = selectedLocationId ?? 1

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    locationId
  )

  const handleOpenFeedbackInbox = (feedbackId?: number) => {
    const inboxPath = operatorDashboardNavPath(
      mode,
      "feedback",
      locationId
    )
    if (feedbackId == null) {
      navigate(inboxPath)
      return
    }
    const separator = inboxPath.includes("?") ? "&" : "?"
    navigate(`${inboxPath}${separator}feedbackId=${feedbackId}`)
  }

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
          onGenerateBrief={onGenerateBrief}
          generateBusy={generateBusy}
          onExport={openExportDialog}
          exportDisabled={!exportAllowed}
          selectedRange={dateRange}
          onCommitRange={commitRange}
        />
      }
    >
      {feedbackLoadStatus === "loading" || feedbackLoadStatus === "idle" ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading feedback report"
        >
          <Spinner />
        </div>
      ) : null}

      {feedbackLoadStatus === "error" ? (
        <div className="flex flex-col items-start gap-3" role="alert">
          <p className="m-0 text-sm text-destructive">
            {feedbackLoadError ?? FEEDBACK_REPORT_PAGE_COPY.loadError}
          </p>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => {
              void reports.retryFeedbackLoad()
            }}
          >
            {FEEDBACK_REPORT_PAGE_COPY.retry}
          </Button>
        </div>
      ) : null}

      {feedbackLoadStatus === "lifetimeEmpty" ? (
        <ReportsEmptyState
          title={FEEDBACK_REPORT_PAGE_COPY.emptyTitle}
          subtitle={FEEDBACK_REPORT_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={() => handleOpenFeedbackInbox()}
            >
              {FEEDBACK_REPORT_PAGE_COPY.checkGuestForm}
            </Button>
          }
        />
      ) : null}

      {feedbackLoadStatus === "ready" && feedbackReport != null ? (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={feedbackReport.topKpis} />
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
                      Status
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                      Actions
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {feedbackReport.followUpList.length === 0 ? (
                    <TableRow className={REPORTS_TABLE_BODY_ROW_CLASS}>
                      <TableCell
                        className={REPORTS_TABLE_BODY_CELL_CLASS}
                        colSpan={6}
                      >
                        No feedback needs follow-up in this period.
                      </TableCell>
                    </TableRow>
                  ) : (
                    feedbackReport.followUpList.map((row) => (
                      <TableRow
                        key={row.feedbackId}
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
                          <ReportsStatusBadge status={row.status} />
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_ACTIONS_CELL_CLASS}>
                          <Button
                            type="button"
                            variant="op-tertiary"
                            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                            onClick={() =>
                              handleOpenFeedbackInbox(row.feedbackId)
                            }
                          >
                            {FEEDBACK_REPORT_PAGE_COPY.openAction}
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>

            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={() => handleOpenFeedbackInbox()}
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
                      Marketing opt-ins
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Follow-up needed
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {feedbackReport.sourcesList.length === 0 ? (
                    <TableRow className={REPORTS_TABLE_BODY_ROW_CLASS}>
                      <TableCell
                        className={REPORTS_TABLE_BODY_CELL_CLASS}
                        colSpan={4}
                      >
                        No feedback by source in this period.
                      </TableCell>
                    </TableRow>
                  ) : (
                    feedbackReport.sourcesList.map((row) => (
                      <TableRow
                        key={row.qrCodeId}
                        className={REPORTS_TABLE_BODY_ROW_CLASS}
                      >
                        <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                          {row.source}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.feedback}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.marketingOptIns}
                        </TableCell>
                        <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                          {row.followUpNeeded}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </ReportsSection>

          <ReportsSection title={FEEDBACK_REPORT_PAGE_COPY.feedbackStatusTitle}>
            <ReportsKpiStrip items={feedbackReport.statusKpis} />

            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={() => handleOpenFeedbackInbox()}
            >
              {FEEDBACK_REPORT_PAGE_COPY.manageFeedback}
            </Button>
          </ReportsSection>
        </div>
      ) : null}
    </ReportsPageChrome>
  )
}
