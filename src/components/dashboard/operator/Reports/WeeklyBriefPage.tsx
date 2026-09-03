import { useNavigate, useSearchParams } from "react-router-dom"
import { toast } from "sonner"
import { Download, CheckCircle } from "lucide-react"
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
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  mockWeeklyBriefData,
  type WeeklyBriefData,
} from "@/lib/operatorReports/weeklyBriefPresentation"
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
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardFeedbackReportPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { cn } from "@/lib/utils"

type WeeklyBriefPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
  data?: WeeklyBriefData
}

export function WeeklyBriefPage({
  selectedLocationId = 1,
  selectedLocationName: _selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockWeeklyBriefData,
}: WeeklyBriefPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleDownloadPdf = () => {
    toast.success(WEEKLY_BRIEF_PAGE_COPY.pdfDownloadedToast)
  }

  const handleMarkAsReviewed = () => {
    toast.success(WEEKLY_BRIEF_PAGE_COPY.reviewedToast)
  }

  const handleActionNavigation = (
    target: "feedback" | "feedback-inbox" | "campaigns"
  ) => {
    if (target === "feedback" || target === "feedback-inbox") {
      navigate(operatorDashboardNavPath(mode, "feedback", selectedLocationId))
    } else if (target === "campaigns") {
      navigate(operatorDashboardNavPath(mode, "campaigns", selectedLocationId))
    }
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
      {isPageEmpty ? (
        <ReportsEmptyState
          title={WEEKLY_BRIEF_PAGE_COPY.emptyTitle}
          subtitle={WEEKLY_BRIEF_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={() =>
                navigate(
                  operatorDashboardFeedbackReportPath(mode, selectedLocationId)
                )
              }
            >
              {WEEKLY_BRIEF_PAGE_COPY.generateBrief}
            </Button>
          }
        />
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm font-semibold text-op-text-muted sm:text-base">
                  Period
                </span>
                <span className="text-right text-sm font-medium text-op-text-primary sm:text-base">
                  {data.period}
                </span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm font-semibold text-op-text-muted sm:text-base">
                  Data sources
                </span>
                <span className="text-right text-sm font-medium text-op-text-primary sm:text-base">
                  {data.dataSources}
                </span>
              </div>
            </div>

            <div className="h-px w-full bg-op-border-default" />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm font-semibold text-op-text-muted sm:text-base">
                  Location
                </span>
                <span className="text-right text-sm font-medium text-op-text-primary sm:text-base">
                  {data.location}
                </span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm font-semibold text-op-text-muted sm:text-base">
                  Confidence
                </span>
                <span className="text-right text-sm font-medium text-op-text-primary sm:text-base">
                  {data.confidence}
                </span>
              </div>
            </div>

            <div className="h-px w-full bg-op-border-default" />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm font-semibold text-op-text-muted sm:text-base">
                  Generated
                </span>
                <span className="text-right text-sm font-medium text-op-text-primary sm:text-base">
                  {data.generated}
                </span>
              </div>
            </div>
          </ReportsSection>

          <ReportsSection title={WEEKLY_BRIEF_PAGE_COPY.executiveSummaryTitle}>
            <ReportsInsightBanner>
              {data.executiveSummary}
            </ReportsInsightBanner>
          </ReportsSection>

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
                  {data.changes.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.area}
                      </TableCell>
                      <TableCell
                        className={cn(
                          REPORTS_TABLE_BODY_CELL_CLASS,
                          "font-semibold",
                          row.change.startsWith("+")
                            ? "text-green-500"
                            : row.change.startsWith("-")
                              ? "text-red-400"
                              : undefined
                        )}
                      >
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

          <ReportsSection title={WEEKLY_BRIEF_PAGE_COPY.feedbackSummaryTitle}>
            <ReportsInsightBanner title={data.feedbackSummary.text}>
              {data.feedbackSummary.subtitle}
            </ReportsInsightBanner>
            <div>
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={() => handleActionNavigation("feedback-inbox")}
              >
                {WEEKLY_BRIEF_PAGE_COPY.reviewFollowUpQueue}
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title={WEEKLY_BRIEF_PAGE_COPY.recommendedActionsTitle}
          >
            <div className="flex flex-col gap-4">
              {data.recommendedActions.map((action) => (
                <div key={action.id} className="flex flex-col gap-4">
                  <ReportsInsightBanner title={action.title}>
                    {action.subtitle}
                  </ReportsInsightBanner>
                  <div>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                      onClick={() => handleActionNavigation(action.target)}
                    >
                      {action.cta}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </ReportsSection>

          <ReportsSection
            title={WEEKLY_BRIEF_PAGE_COPY.suggestedCampaignTitle}
          >
            <div className="flex w-72 flex-col justify-between gap-6 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-4 sm:w-80">
              <div className="flex flex-col gap-3">
                <ReportsStatusBadge
                  status={data.suggestedCampaign.status}
                />
                <div className="flex flex-col gap-1.5">
                  <h3 className="text-base font-semibold leading-snug text-op-text-primary">
                    {data.suggestedCampaign.title}
                  </h3>
                  <p className="text-xs font-normal leading-relaxed text-op-text-muted">
                    {data.suggestedCampaign.subtitle}
                  </p>
                </div>
              </div>

              <div>
                <Button
                  type="button"
                  variant="op-primary"
                  className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                  onClick={() => handleActionNavigation("campaigns")}
                >
                  {data.suggestedCampaign.cta}
                </Button>
              </div>
            </div>
          </ReportsSection>
        </div>
      )}
    </ReportsPageChrome>
  )
}
