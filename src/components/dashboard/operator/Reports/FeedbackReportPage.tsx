import { useState } from "react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import {
  Calendar,
  ChevronDown,
  ChevronRight,
  Download,
  ArrowUpRight,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { ReportsWeeklyBriefDialog } from "@/components/dashboard/operator/Reports/ReportsWeeklyBriefDialog"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import {
  FEEDBACK_REPORT_PAGE_COPY,
  DATE_PRESET_LABELS,
  mockFeedbackReportData,
  type FeedbackReportData,
  type DatePreset,
} from "@/lib/operatorReports/feedbackReportPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardCaptureLocationPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { cn } from "@/lib/utils"

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
  locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockFeedbackReportData,
}: FeedbackReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [datePreset, setDatePreset] = useState<DatePreset>("7d")
  const [isWeeklyBriefOpen, setIsWeeklyBriefOpen] = useState(false)
  const [isExportOpen, setIsExportOpen] = useState(false)

  const isPageEmpty = propIsEmpty ?? (searchParams.get("empty") === "true")
  const dateRangeLabel = DATE_PRESET_LABELS[datePreset]

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleOpenFeedbackInbox = () => {
    navigate(operatorDashboardNavPath(mode, "feedback", selectedLocationId))
  }

  const handleCreatePlacement = () => {
    if (mode === "multi") {
      navigate(operatorDashboardCaptureLocationPath(selectedLocationId))
    } else {
      navigate(operatorDashboardNavPath(mode, "capture", selectedLocationId))
    }
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
    <div className="w-full flex flex-col gap-6">
      {/* 1. Breadcrumb Navigation */}
      <nav aria-label="Breadcrumb" className="inline-flex items-center gap-2.5">
        <Link
          to={reportsBasePath}
          className="text-base font-medium text-op-text-primary hover:text-op-text-primary/80 transition-colors"
        >
          {FEEDBACK_REPORT_PAGE_COPY.breadcrumbReports}
        </Link>
        <ChevronRight className="size-4 text-op-text-muted shrink-0" />
        <span className="text-base font-medium text-op-text-muted">
          {FEEDBACK_REPORT_PAGE_COPY.breadcrumbFeedbackReport}
        </span>
      </nav>

      {/* 2. Page Header Row */}
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div className="flex flex-col gap-1">
          <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
            {FEEDBACK_REPORT_PAGE_COPY.title}
          </h1>
          <p className="text-base font-medium text-op-text-muted">
            {FEEDBACK_REPORT_PAGE_COPY.subtitle}
          </p>
        </div>

        {/* Actions Row */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Generate Brief Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() => setIsWeeklyBriefOpen(true)}
          >
            <img
              src={aiIconPng}
              alt=""
              className="size-4 shrink-0 brightness-0 dark:invert"
            />
            <span>{FEEDBACK_REPORT_PAGE_COPY.generateBrief}</span>
          </Button>

          {/* Export Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() => setIsExportOpen(true)}
          >
            <Download className="size-4" />
            <span>{FEEDBACK_REPORT_PAGE_COPY.export}</span>
          </Button>

          {/* Date Range Selector */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="op-date"
                className="h-10 gap-2 rounded-xs border-op-button-date-border px-4 text-xs font-medium text-op-button-date-text hover:bg-op-surface-secondary"
              >
                <Calendar className="size-3.5" />
                <span>{dateRangeLabel}</span>
                <ChevronDown className="size-3.5 opacity-70" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className="w-44 border-op-border-default bg-op-background-primary text-op-text-primary z-[220]"
            >
              {(Object.keys(DATE_PRESET_LABELS) as DatePreset[]).map((key) => (
                <DropdownMenuItem
                  key={key}
                  onClick={() => setDatePreset(key)}
                  className={cn(
                    "cursor-pointer text-xs",
                    datePreset === key && "font-semibold text-op-action-primary"
                  )}
                >
                  {DATE_PRESET_LABELS[key]}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {isPageEmpty ? (
        /* Empty State */
        <div className="flex w-full min-h-[400px] flex-col items-center justify-center gap-2 py-20 text-center">
          <h2 className="text-lg font-bold text-op-text-primary">
            {FEEDBACK_REPORT_PAGE_COPY.emptyTitle}
          </h2>
          <p className="max-w-md text-sm font-medium leading-relaxed text-op-text-muted">
            {FEEDBACK_REPORT_PAGE_COPY.emptySubtitle}
          </p>
          <Button
            type="button"
            variant="op-secondary"
            className="mt-4 h-10 px-5 rounded-xs text-sm font-medium"
            onClick={handleOpenFeedbackInbox}
          >
            {FEEDBACK_REPORT_PAGE_COPY.checkGuestForm}
          </Button>
        </div>
      ) : (
        /* Populated Feedback Report Content */
        <div className="flex flex-col gap-6">
          {/* Card 1: 5-KPI Strip */}
          <div className="w-full rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 divide-y sm:divide-y-0 sm:divide-x divide-op-border-default">
              {topKpisList.map((kpi, index) => (
                <div
                  key={kpi.label}
                  className={cn(
                    "flex flex-col justify-between gap-1 py-3 sm:py-2",
                    index === 0 ? "sm:pl-2" : "sm:px-4",
                    index === topKpisList.length - 1 ? "sm:pr-2" : ""
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

          {/* Card 2: Feedback over time */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex flex-col gap-1">
              <h2 className="text-xl font-bold text-op-text-primary">
                {FEEDBACK_REPORT_PAGE_COPY.feedbackOverTimeTitle}
              </h2>
              <p className="text-sm font-medium text-op-text-muted">
                {FEEDBACK_REPORT_PAGE_COPY.feedbackOverTimeSubtitle}
              </p>
            </div>

            {/* Visual chart container */}
            <div className="w-full h-72 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col justify-between p-6 relative overflow-hidden">
              {/* Subtle background grid lines */}
              <div className="absolute inset-x-6 inset-y-6 flex flex-col justify-between pointer-events-none opacity-20">
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
                <div className="w-full border-b border-dashed border-op-border-default" />
              </div>

              {/* Chart SVG Graphic */}
              <div className="flex-1 w-full flex items-end justify-between z-10 px-4 pb-2">
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
                    className="flex flex-col items-center gap-2 h-full justify-end group cursor-pointer"
                  >
                    <div className="opacity-0 group-hover:opacity-100 transition-opacity text-[11px] font-semibold text-op-action-primary">
                      {bar.count}
                    </div>
                    <div
                      className="w-8 sm:w-12 bg-op-action-primary/80 hover:bg-op-action-primary rounded-t-xs transition-all duration-300"
                      style={{ height: bar.height }}
                    />
                    <span className="text-xs font-medium text-op-text-muted">
                      {bar.day}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Card 3: Common themes */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex items-center gap-2.5 pb-2 border-b border-op-border-default">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 dark:invert"
              />
              <h2 className="text-xl font-bold text-op-text-primary">
                {FEEDBACK_REPORT_PAGE_COPY.commonThemesTitle}
              </h2>
            </div>

            <div className="flex flex-col gap-3">
              {data.themes.map((theme) => (
                <div
                  key={theme.id}
                  className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col justify-start items-start gap-4"
                >
                  <p className="text-sm font-normal text-op-text-primary leading-relaxed max-w-4xl">
                    {theme.theme}
                  </p>
                  <p className="text-xs font-normal text-op-text-muted leading-relaxed">
                    {theme.meta}
                  </p>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="h-9 px-4 rounded-xs text-xs font-medium"
                    onClick={handleOpenFeedbackInbox}
                  >
                    {FEEDBACK_REPORT_PAGE_COPY.viewSourceFeedback}
                  </Button>
                </div>
              ))}
            </div>
          </div>

          {/* Card 4: Needs follow-up */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex flex-col gap-1">
              <h2 className="text-xl font-bold text-op-text-primary">
                {FEEDBACK_REPORT_PAGE_COPY.needsFollowUpTitle}
              </h2>
              <p className="text-sm font-medium text-op-text-muted">
                {FEEDBACK_REPORT_PAGE_COPY.needsFollowUpSubtitle}
              </p>
            </div>

            {/* Follow-up Table */}
            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-op-surface-secondary/90 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[100px]">
                      Date
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[120px]">
                      Guest
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[140px]">
                      Source
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[280px]">
                      Feedback
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[120px]">
                      Reason
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[140px]">
                      Status
                    </th>
                    <th className="px-4 py-3 text-center min-w-[80px]">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.followUpList.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.date}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.guest}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-muted">
                        {row.source}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary font-normal">
                        {row.feedback}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-muted">
                        {row.reason}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default">
                        <span className="inline-flex items-center px-2 py-1 rounded-xs text-xs font-medium bg-amber-500/15 text-amber-400">
                          {row.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Button
                          type="button"
                          variant="op-tertiary"
                          className="h-8 px-3 rounded-xs text-xs font-medium"
                          onClick={handleOpenFeedbackInbox}
                        >
                          {FEEDBACK_REPORT_PAGE_COPY.openAction}
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* CTA */}
            <Button
              type="button"
              variant="op-primary"
              className="h-10 rounded-xs px-4 text-sm font-medium self-start"
              onClick={handleOpenFeedbackInbox}
            >
              {FEEDBACK_REPORT_PAGE_COPY.openFeedbackInbox}
            </Button>
          </div>

          {/* Card 5: Feedback by source */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex flex-col gap-1">
              <h2 className="text-xl font-bold text-op-text-primary">
                {FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceTitle}
              </h2>
              <p className="text-sm font-medium text-op-text-muted">
                {FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceSubtitle}
              </p>
            </div>

            {/* Source Table */}
            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-op-surface-secondary/90 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[160px]">
                      Source
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[100px]">
                      Feedback
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[120px]">
                      Contactable
                    </th>
                    <th className="px-4 py-3 min-w-[140px]">
                      Follow-up needed
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.sourcesList.map((row) => (
                    <tr
                      key={row.source}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.source}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.feedback}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.contactable}
                      </td>
                      <td className="px-4 py-3 text-op-text-primary">
                        {row.followUpNeeded}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* AI Callout Banner */}
            <div className="w-full p-4 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex items-center gap-3">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 dark:invert mt-0.5"
              />
              <p className="text-sm font-medium text-op-text-primary leading-relaxed">
                {FEEDBACK_REPORT_PAGE_COPY.sourceInsightText}
              </p>
            </div>
          </div>

          {/* Card 6: Feedback status */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="flex flex-col gap-1">
              <h2 className="text-xl font-bold text-op-text-primary">
                {FEEDBACK_REPORT_PAGE_COPY.feedbackStatusTitle}
              </h2>
            </div>

            {/* Status 5-KPI Strip */}
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 divide-y sm:divide-y-0 sm:divide-x divide-op-border-default">
              {statusKpisList.map((kpi, index) => (
                <div
                  key={kpi.label}
                  className={cn(
                    "flex flex-col justify-between gap-1 py-3 sm:py-2",
                    index === 0 ? "sm:pl-2" : "sm:px-4",
                    index === statusKpisList.length - 1 ? "sm:pr-2" : ""
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

            {/* CTA */}
            <Button
              type="button"
              variant="op-primary"
              className="h-10 rounded-xs px-4 text-sm font-medium self-start"
              onClick={handleOpenFeedbackInbox}
            >
              {FEEDBACK_REPORT_PAGE_COPY.manageFeedback}
            </Button>
          </div>
        </div>
      )}

      {/* Weekly Brief Dialog */}
      <ReportsWeeklyBriefDialog
        open={isWeeklyBriefOpen}
        onOpenChange={setIsWeeklyBriefOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
        onNavigateToFeedback={handleOpenFeedbackInbox}
        onNavigateToCampaigns={() =>
          navigate(
            operatorDashboardNavPath(mode, "campaigns", selectedLocationId)
          )
        }
        onNavigateToCapture={handleCreatePlacement}
      />

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
