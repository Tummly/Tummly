import { useEffect } from "react"
import { Download, CheckCircle } from "lucide-react"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import {
  WEEKLY_BRIEF_BODY_CLASS,
  WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS,
  WEEKLY_BRIEF_DOMAIN_LABEL_CLASS,
  WEEKLY_BRIEF_DOMAIN_SUMMARY_CLASS,
  WEEKLY_BRIEF_HEADLINE_CLASS,
  WEEKLY_BRIEF_RETRY_LABEL,
  WEEKLY_BRIEF_STATUS_SHELL_CLASS,
  WEEKLY_BRIEF_WATCH_LIST_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_PAGE_ACTION_BUTTON_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import {
  REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
} from "@/lib/operatorReports/reportsWeeklyBriefPresentation"
import { WEEKLY_BRIEF_PAGE_COPY } from "@/lib/operatorReports/weeklyBriefPresentation"
import type { WeeklyBriefBody, WeeklyBriefSection } from "@/types/operatorHome"

type WeeklyBriefPageProps = {
  mode?: DashboardProps["mode"]
}

function DomainBlock(props: {
  label: string
  section: WeeklyBriefSection
}) {
  return (
    <div className={WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS}>
      <p className={WEEKLY_BRIEF_DOMAIN_LABEL_CLASS}>{props.label}</p>
      <p className={WEEKLY_BRIEF_DOMAIN_SUMMARY_CLASS}>
        {props.section.summary}
      </p>
    </div>
  )
}

function ReadyBody(props: { body: WeeklyBriefBody }) {
  const { body } = props
  return (
    <div className={WEEKLY_BRIEF_BODY_CLASS}>
      <p className={WEEKLY_BRIEF_HEADLINE_CLASS}>{body.headline}</p>
      <DomainBlock label="Capture" section={body.capture} />
      <DomainBlock label="Feedback" section={body.feedback} />
      <DomainBlock label="Offers" section={body.offers} />
      <DomainBlock label="Campaigns" section={body.campaigns} />
      {body.watchNext.length > 0 ? (
        <div className={WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS}>
          <p className={WEEKLY_BRIEF_DOMAIN_LABEL_CLASS}>Watch next</p>
          <ul className={WEEKLY_BRIEF_WATCH_LIST_CLASS}>
            {body.watchNext.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  )
}

/**
 * Reports Weekly Brief page — same durable body as Home; Reports chrome only.
 * PDF / Mark as reviewed stay presentational stubs (lock 02 out).
 */
export function WeeklyBriefPage({ mode = "single" }: WeeklyBriefPageProps) {
  const reports = useReportsPageModule()
  const pageModule = useReportsPageModuleApi()
  const { weeklyBrief, selectedLocationId } = reports.snapshot
  const locationId = selectedLocationId ?? 1

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
    toast.success(WEEKLY_BRIEF_PAGE_COPY.reviewedToast)
  }

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
            onClick={handleMarkAsReviewed}
          >
            <CheckCircle className="size-4" aria-hidden />
            <span>{WEEKLY_BRIEF_PAGE_COPY.markAsReviewed}</span>
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

      {weeklyBrief.status === "ready" && weeklyBrief.body != null ? (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReadyBody body={weeklyBrief.body} />
          </ReportsSection>
        </div>
      ) : null}
    </ReportsPageChrome>
  )
}
