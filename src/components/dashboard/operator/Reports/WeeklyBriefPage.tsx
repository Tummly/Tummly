import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { Download, CheckCircle } from "lucide-react"
import { toast } from "sonner"

import { AiIcon } from "@/components/ui/ai-icon"
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
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import {
  WEEKLY_BRIEF_RETRY_LABEL,
  WEEKLY_BRIEF_STATUS_SHELL_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_PAGE_ACTION_BUTTON_CLASS,
  REPORTS_SECTION_SUBTITLE_CLASS,
  REPORTS_SECTION_TITLE_CLASS,
  REPORTS_TABLE_BODY_CELL_CLASS,
  REPORTS_TABLE_BODY_ROW_CLASS,
  REPORTS_TABLE_CLASS,
  REPORTS_TABLE_FRAME_CLASS,
  REPORTS_TABLE_HEAD_CELL_CLASS,
  REPORTS_TABLE_HEAD_ROW_CLASS,
  REPORTS_TABLE_NAME_CELL_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import { REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE } from "@/lib/operatorReports/reportsWeeklyBriefPresentation"
import {
  formatWeeklyBriefDataSources,
  formatWeeklyBriefGeneratedAt,
  planWeeklyBriefFeedbackFollowUpCta,
  shouldShowWeeklyBriefFeedbackSummary,
  shouldShowWeeklyBriefWhatChanged,
  weeklyBriefMarkAsReviewedLabel,
  WEEKLY_BRIEF_PAGE_COPY,
} from "@/lib/operatorReports/weeklyBriefPresentation"
import type { OperatorReportsWeeklyBriefMeta } from "@/lib/operatorReports/createOperatorReportsPageModule"
import type {
  WeeklyBriefFeedbackSummary,
  WeeklyBriefWhatChangedRow,
} from "@/types/operatorHome"

type WeeklyBriefPageProps = {
  mode?: DashboardProps["mode"]
}

const META_LABEL_CLASS =
  "m-0 text-sm font-medium leading-5 text-op-text-muted"
const META_VALUE_CLASS =
  "m-0 text-sm font-medium leading-5 text-op-card-title-color"
const META_FIELD_CLASS =
  "flex min-w-0 items-start justify-between gap-4 text-sm leading-5"
const EXEC_SUMMARY_BODY_CLASS =
  "m-0 rounded-op-md bg-op-background-secondary p-4 text-sm font-medium leading-6 text-op-card-title-color"
const FEEDBACK_SUMMARY_BODY_CLASS =
  "m-0 text-sm font-medium leading-6 text-op-card-title-color"

function MetaField(props: { label: string; value: string }) {
  return (
    <div className={META_FIELD_CLASS}>
      <p className={META_LABEL_CLASS}>{props.label}</p>
      <p className={`${META_VALUE_CLASS} text-right`}>{props.value}</p>
    </div>
  )
}

function WeeklyBriefMetaCard(props: {
  meta: OperatorReportsWeeklyBriefMeta
  locationName: string
}) {
  const { meta, locationName } = props
  return (
    <ReportsSection>
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 sm:gap-x-16 sm:gap-y-6">
        <MetaField
          label={WEEKLY_BRIEF_PAGE_COPY.periodLabel}
          value={meta.period}
        />
        <MetaField
          label={WEEKLY_BRIEF_PAGE_COPY.dataSourcesLabel}
          value={formatWeeklyBriefDataSources(meta.dataSources)}
        />
        <MetaField
          label={WEEKLY_BRIEF_PAGE_COPY.locationLabel}
          value={locationName}
        />
        <MetaField
          label={WEEKLY_BRIEF_PAGE_COPY.confidenceLabel}
          value={meta.confidence}
        />
        <MetaField
          label={WEEKLY_BRIEF_PAGE_COPY.generatedLabel}
          value={formatWeeklyBriefGeneratedAt(meta.generatedAtUtc)}
        />
      </div>
    </ReportsSection>
  )
}

function ExecutiveSummarySection(props: { summary: string }) {
  return (
    <ReportsSection>
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-2">
          <AiIcon size={22} />
          <h2 className={REPORTS_SECTION_TITLE_CLASS}>
            {WEEKLY_BRIEF_PAGE_COPY.executiveSummaryTitle}
          </h2>
        </div>
        <p className={EXEC_SUMMARY_BODY_CLASS}>{props.summary}</p>
      </div>
    </ReportsSection>
  )
}

function WhatChangedSection(props: {
  rows: readonly WeeklyBriefWhatChangedRow[]
}) {
  return (
    <ReportsSection title={WEEKLY_BRIEF_PAGE_COPY.whatChangedTitle}>
      <div className={REPORTS_TABLE_FRAME_CLASS}>
        <Table className={REPORTS_TABLE_CLASS}>
          <TableHeader>
            <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
              <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                {WEEKLY_BRIEF_PAGE_COPY.areaHeader}
              </TableHead>
              <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                {WEEKLY_BRIEF_PAGE_COPY.changeHeader}
              </TableHead>
              <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                {WEEKLY_BRIEF_PAGE_COPY.meaningHeader}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {props.rows.map((row) => (
              <TableRow
                key={`${row.area}-${row.change}`}
                className={REPORTS_TABLE_BODY_ROW_CLASS}
              >
                <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                  {row.area}
                </TableCell>
                <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                  {row.change}
                </TableCell>
                <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                  {row.meaning}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </ReportsSection>
  )
}

function WeeklyBriefFeedbackSummarySection(props: {
  summary: WeeklyBriefFeedbackSummary
  onReviewFollowUp: () => void
}) {
  return (
    <ReportsSection>
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-2">
          <AiIcon size={22} />
          <h2 className={REPORTS_SECTION_TITLE_CLASS}>
            {WEEKLY_BRIEF_PAGE_COPY.feedbackSummaryTitle}
          </h2>
        </div>
        <p className={FEEDBACK_SUMMARY_BODY_CLASS}>{props.summary.text}</p>
        <p className={REPORTS_SECTION_SUBTITLE_CLASS}>{props.summary.subtitle}</p>
        <div>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={props.onReviewFollowUp}
          >
            {WEEKLY_BRIEF_PAGE_COPY.reviewFollowUpQueue}
          </Button>
        </div>
      </div>
    </ReportsSection>
  )
}

/**
 * Reports Weekly Brief page — Figma ready chrome (meta, executive summary,
 * What changed, Feedback summary). PDF stays a toast stub until 09.
 */
export function WeeklyBriefPage({ mode = "single" }: WeeklyBriefPageProps) {
  const navigate = useNavigate()
  const setFeedbackInboxIntent = useDashboardUiStore(
    (state) => state.setFeedbackInboxIntent
  )
  const reports = useReportsPageModule()
  const pageModule = useReportsPageModuleApi()
  const { weeklyBrief, selectedLocationId, selectedLocationName, markAsReviewedAllowed } =
    reports.snapshot
  const locationId = selectedLocationId ?? 1
  const locationName = selectedLocationName ?? "Location"

  useEffect(() => {
    pageModule.setActiveSurface("weekly-brief")
  }, [pageModule])

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    locationId
  )

  const handleDownloadPdf = () => {
    toast.success(WEEKLY_BRIEF_PAGE_COPY.pdfDownloadedToast)
  }

  const handleMarkAsReviewed = () => {
    if (!markAsReviewedAllowed || weeklyBrief.status !== "ready") {
      return
    }
    void (async () => {
      const ok = await reports.markWeeklyBriefAsReviewed()
      if (ok) {
        toast.success(WEEKLY_BRIEF_PAGE_COPY.reviewedToast)
      }
    })()
  }

  const handleReviewFollowUp = () => {
    const plan = planWeeklyBriefFeedbackFollowUpCta({ mode, locationId })
    setFeedbackInboxIntent(plan.feedbackInbox)
    navigate(plan.path)
  }

  const readyMeta = weeklyBrief.meta
  const readySummary = weeklyBrief.executiveSummary
  const showWhatChanged = shouldShowWeeklyBriefWhatChanged(
    weeklyBrief.whatChanged
  )
  const showFeedbackSummary = shouldShowWeeklyBriefFeedbackSummary(
    weeklyBrief.feedbackSummary
  )
  const markReviewedLabel = weeklyBriefMarkAsReviewedLabel(
    weeklyBrief.reviewedAtUtc
  )

  return (
    <ReportsPageChrome
      title={WEEKLY_BRIEF_PAGE_COPY.pageTitle}
      subtitle={WEEKLY_BRIEF_PAGE_COPY.pageSubtitle}
      breadcrumb={{
        reportsBasePath,
        currentLabel: WEEKLY_BRIEF_PAGE_COPY.breadcrumbWeeklyBrief,
      }}
      actions={
        <>
          <Button
            type="button"
            variant="op-secondary"
            className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
            onClick={handleDownloadPdf}
          >
            <Download className="size-4" aria-hidden />
            <span>{WEEKLY_BRIEF_PAGE_COPY.downloadPdf}</span>
          </Button>

          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled={
              !markAsReviewedAllowed
              || weeklyBrief.status !== "ready"
              || weeklyBrief.markReviewedBusy
            }
            onClick={handleMarkAsReviewed}
          >
            <CheckCircle className="size-4" aria-hidden />
            <span>{markReviewedLabel}</span>
          </Button>
        </>
      }
    >
      {weeklyBrief.status === "loading" ? (
        <div
          className={WEEKLY_BRIEF_STATUS_SHELL_CLASS}
          role="status"
          aria-live="polite"
          aria-label="Loading weekly brief"
        >
          <Spinner />
        </div>
      ) : null}

      {weeklyBrief.status === "empty" ? (
        <ReportsEmptyState
          title={WEEKLY_BRIEF_PAGE_COPY.emptyTitle}
          subtitle={WEEKLY_BRIEF_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              disabled={weeklyBrief.generateBusy}
              onClick={() => {
                void reports.generateWeeklyBriefInPlace()
              }}
            >
              {WEEKLY_BRIEF_PAGE_COPY.generateBrief}
            </Button>
          }
        />
      ) : null}

      {weeklyBrief.status === "error" ? (
        <div className={WEEKLY_BRIEF_STATUS_SHELL_CLASS} role="alert">
          <p className="m-0 text-center text-sm text-destructive">
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
              {WEEKLY_BRIEF_RETRY_LABEL}
            </Button>
          ) : null}
        </div>
      ) : null}

      {weeklyBrief.status === "ready" &&
      readyMeta != null &&
      readySummary != null ? (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <WeeklyBriefMetaCard meta={readyMeta} locationName={locationName} />
          <ExecutiveSummarySection summary={readySummary} />
          {showWhatChanged ? (
            <WhatChangedSection rows={weeklyBrief.whatChanged} />
          ) : null}
          {showFeedbackSummary && weeklyBrief.feedbackSummary != null ? (
            <WeeklyBriefFeedbackSummarySection
              summary={weeklyBrief.feedbackSummary}
              onReviewFollowUp={handleReviewFollowUp}
            />
          ) : null}
        </div>
      ) : null}
    </ReportsPageChrome>
  )
}
