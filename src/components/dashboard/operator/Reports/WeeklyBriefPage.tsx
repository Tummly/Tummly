import { Link, useNavigate, useSearchParams } from "react-router-dom"
import { toast } from "sonner"
import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import {
  ChevronRight,
  Download,
  CheckCircle,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  mockWeeklyBriefData,
  type WeeklyBriefData,
} from "@/lib/operatorReports/weeklyBriefPresentation"
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
  selectedLocationName = "Mehmet's Grill",
  locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockWeeklyBriefData,
}: WeeklyBriefPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const isPageEmpty = propIsEmpty ?? (searchParams.get("empty") === "true")

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
    <div className="w-full flex flex-col gap-6">
      {/* 1. Breadcrumb Navigation */}
      <nav aria-label="Breadcrumb" className="inline-flex items-center gap-2.5">
        <Link
          to={reportsBasePath}
          className="text-base font-medium text-op-text-primary hover:text-op-text-primary/80 transition-colors"
        >
          {WEEKLY_BRIEF_PAGE_COPY.breadcrumbReports}
        </Link>
        <ChevronRight className="size-4 text-op-text-muted shrink-0" />
        <span className="text-base font-medium text-op-text-muted">
          {WEEKLY_BRIEF_PAGE_COPY.breadcrumbWeeklyBrief}
        </span>
      </nav>

      {/* 2. Page Header Row */}
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div className="flex flex-col gap-1">
          <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
            {WEEKLY_BRIEF_PAGE_COPY.pageTitle}
          </h1>
          <p className="text-sm sm:text-base font-medium text-op-text-muted">
            {WEEKLY_BRIEF_PAGE_COPY.pageSubtitle}
          </p>
        </div>

        {/* Header Action Buttons */}
        <div className="flex items-center gap-3 shrink-0">
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={handleDownloadPdf}
          >
            <Download className="size-4" />
            <span>{WEEKLY_BRIEF_PAGE_COPY.downloadPdf}</span>
          </Button>

          <Button
            type="button"
            variant="op-tertiary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={handleMarkAsReviewed}
          >
            <CheckCircle className="size-4" />
            <span>{WEEKLY_BRIEF_PAGE_COPY.markAsReviewed}</span>
          </Button>
        </div>
      </div>

      {isPageEmpty ? (
        /* Empty State View */
        <div className="w-full min-h-[420px] flex flex-col items-center justify-center gap-4 rounded-md border border-op-border-default bg-op-card-background p-12 text-center shadow-sm">
          <div className="flex flex-col items-center gap-2 max-w-md">
            <h2 className="text-xl font-bold text-op-text-primary">
              {WEEKLY_BRIEF_PAGE_COPY.emptyTitle}
            </h2>
            <p className="text-sm font-medium text-op-text-muted leading-relaxed">
              {WEEKLY_BRIEF_PAGE_COPY.emptySubtitle}
            </p>
          </div>

          <Button
            type="button"
            variant="op-primary"
            className="h-10 rounded-xs px-5 text-sm font-medium mt-2"
            onClick={() =>
              navigate(
                operatorDashboardFeedbackReportPath(mode, selectedLocationId)
              )
            }
          >
            {WEEKLY_BRIEF_PAGE_COPY.generateBrief}
          </Button>
        </div>
      ) : (
        /* Populated 6-Card View */
        <div className="w-full flex flex-col gap-6">
          {/* 1. Period & Metadata Card */}
          <div className="w-full flex flex-col gap-4 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm sm:text-base font-semibold text-op-text-muted">
                  Period
                </span>
                <span className="text-sm sm:text-base font-medium text-op-text-primary text-right">
                  {data.period}
                </span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm sm:text-base font-semibold text-op-text-muted">
                  Data sources
                </span>
                <span className="text-sm sm:text-base font-medium text-op-text-primary text-right">
                  {data.dataSources}
                </span>
              </div>
            </div>

            <div className="h-px w-full bg-op-border-default" />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm sm:text-base font-semibold text-op-text-muted">
                  Location
                </span>
                <span className="text-sm sm:text-base font-medium text-op-text-primary text-right">
                  {data.location}
                </span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm sm:text-base font-semibold text-op-text-muted">
                  Confidence
                </span>
                <span className="text-sm sm:text-base font-medium text-op-text-primary text-right">
                  {data.confidence}
                </span>
              </div>
            </div>

            <div className="h-px w-full bg-op-border-default" />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="flex items-center justify-between gap-4">
                <span className="text-sm sm:text-base font-semibold text-op-text-muted">
                  Generated
                </span>
                <span className="text-sm sm:text-base font-medium text-op-text-primary text-right">
                  {data.generated}
                </span>
              </div>
            </div>
          </div>

          {/* 2. Executive Summary Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <div className="flex items-center gap-2.5 pb-2 border-b border-op-border-default">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 invert"
              />
              <h2 className="text-xl font-bold text-op-text-primary">
                {WEEKLY_BRIEF_PAGE_COPY.executiveSummaryTitle}
              </h2>
            </div>

            <div className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60">
              <p className="text-sm font-medium text-op-text-muted leading-relaxed max-w-4xl">
                {data.executiveSummary}
              </p>
            </div>
          </div>

          {/* 3. What Changed Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {WEEKLY_BRIEF_PAGE_COPY.whatChangedTitle}
            </h2>

            {/* Changes Table */}
            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-neutral-800/90 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default w-60 min-w-[180px]">
                      {WEEKLY_BRIEF_PAGE_COPY.areaHeader}
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default w-40 min-w-[120px]">
                      {WEEKLY_BRIEF_PAGE_COPY.changeHeader}
                    </th>
                    <th className="px-4 py-3 min-w-[280px]">
                      {WEEKLY_BRIEF_PAGE_COPY.meaningHeader}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.changes.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.area}
                      </td>
                      <td
                        className={cn(
                          "px-4 py-3 border-r border-op-border-default font-semibold",
                          row.change.startsWith("+")
                            ? "text-green-500"
                            : row.change.startsWith("-")
                              ? "text-red-400"
                              : "text-op-text-primary"
                        )}
                      >
                        {row.change}
                      </td>
                      <td className="px-4 py-3 text-op-text-primary font-normal">
                        {row.meaning}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* 4. Feedback Summary Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <div className="flex items-center gap-2.5 pb-2 border-b border-op-border-default">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 invert"
              />
              <h2 className="text-xl font-bold text-op-text-primary">
                {WEEKLY_BRIEF_PAGE_COPY.feedbackSummaryTitle}
              </h2>
            </div>

            <div className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <p className="text-base font-medium text-op-text-primary leading-relaxed max-w-4xl">
                  {data.feedbackSummary.text}
                </p>
                <p className="text-sm font-medium text-op-text-muted">
                  {data.feedbackSummary.subtitle}
                </p>
              </div>

              <div>
                <Button
                  type="button"
                  variant="op-tertiary"
                  className="h-10 px-4 rounded-xs text-sm font-medium"
                  onClick={() => handleActionNavigation("feedback-inbox")}
                >
                  {WEEKLY_BRIEF_PAGE_COPY.reviewFollowUpQueue}
                </Button>
              </div>
            </div>
          </div>

          {/* 5. Recommended Actions Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <div className="flex items-center gap-2.5 pb-2 border-b border-op-border-default">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 invert"
              />
              <h2 className="text-xl font-bold text-op-text-primary">
                {WEEKLY_BRIEF_PAGE_COPY.recommendedActionsTitle}
              </h2>
            </div>

            <div className="flex flex-col gap-4">
              {data.recommendedActions.map((action) => (
                <div
                  key={action.id}
                  className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col gap-4"
                >
                  <div className="flex flex-col gap-1">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      {action.title}
                    </h3>
                    <p className="text-sm font-normal text-op-text-muted leading-relaxed">
                      {action.subtitle}
                    </p>
                  </div>

                  <div>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className="h-10 px-4 rounded-xs text-sm font-medium"
                      onClick={() => handleActionNavigation(action.target)}
                    >
                      {action.cta}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* 6. Suggested Campaign Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 sm:p-7 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {WEEKLY_BRIEF_PAGE_COPY.suggestedCampaignTitle}
            </h2>

            <div className="w-72 sm:w-80 p-4 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col justify-between gap-6">
              <div className="flex flex-col gap-3">
                <span className="w-fit px-2.5 py-1 rounded-xs bg-op-card-background text-xs font-normal text-op-text-primary border border-op-border-default">
                  {data.suggestedCampaign.status}
                </span>
                <div className="flex flex-col gap-1.5">
                  <h3 className="text-base font-semibold text-op-text-primary leading-snug">
                    {data.suggestedCampaign.title}
                  </h3>
                  <p className="text-xs font-normal text-op-text-muted leading-relaxed">
                    {data.suggestedCampaign.subtitle}
                  </p>
                </div>
              </div>

              <div>
                <Button
                  type="button"
                  variant="op-secondary"
                  className="h-10 px-4 rounded-xs text-sm font-medium"
                  onClick={() => handleActionNavigation("campaigns")}
                >
                  {data.suggestedCampaign.cta}
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
