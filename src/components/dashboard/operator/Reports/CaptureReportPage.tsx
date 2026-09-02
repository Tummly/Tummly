import { useState } from "react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import {
  Calendar,
  ChevronDown,
  ChevronRight,
  Download,
  ArrowUpRight,
  MoreVertical,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import {
  CaptureReportPlacementActionModal,
  type CaptureReportPlacementActionType,
} from "@/components/dashboard/operator/Reports/CaptureReportPlacementActionModal"
import {
  CAPTURE_REPORT_PAGE_COPY,
  DATE_PRESET_LABELS,
  mockCaptureReportData,
  type CaptureReportData,
  type CaptureReportPlacementRow,
  type DatePreset,
} from "@/lib/operatorReports/captureReportPresentation"
import {
  operatorDashboardNavPath,
  operatorDashboardCaptureLocationPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { cn } from "@/lib/utils"

type CaptureReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
  data?: CaptureReportData
}

export function CaptureReportPage({
  selectedLocationId = 1,
  selectedLocationName = "Mehmet's Grill",
  locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockCaptureReportData,
}: CaptureReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [datePreset, setDatePreset] = useState<DatePreset>("7d")
  const [isExportOpen, setIsExportOpen] = useState(false)
  const [activePlacementModal, setActivePlacementModal] = useState<{
    actionType: CaptureReportPlacementActionType | null
    placement: CaptureReportPlacementRow | null
  }>({
    actionType: null,
    placement: null,
  })

  const isPageEmpty = propIsEmpty ?? (searchParams.get("empty") === "true")
  const dateRangeLabel = DATE_PRESET_LABELS[datePreset]

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleReviewGuestForm = () => {
    navigate(operatorDashboardNavPath(mode, "feedback", selectedLocationId))
  }

  const handleCreatePlacement = () => {
    if (mode === "multi") {
      navigate(operatorDashboardCaptureLocationPath(selectedLocationId))
    } else {
      navigate(operatorDashboardNavPath(mode, "capture", selectedLocationId))
    }
  }

  const kpisList = [
    data.kpis.qrScans,
    data.kpis.formOpened,
    data.kpis.feedbackSubmitted,
    data.kpis.contactProvided,
    data.kpis.contactableGuests,
    data.kpis.offerClaims,
  ]

  return (
    <div className="w-full flex flex-col gap-6">
      {/* 1. Breadcrumb Navigation */}
      <nav aria-label="Breadcrumb" className="inline-flex items-center gap-2.5">
        <Link
          to={reportsBasePath}
          className="text-base font-medium text-op-text-primary hover:text-op-text-primary/80 transition-colors"
        >
          {CAPTURE_REPORT_PAGE_COPY.breadcrumbReports}
        </Link>
        <ChevronRight className="size-4 text-op-text-muted shrink-0" />
        <span className="text-base font-medium text-op-text-muted">
          {CAPTURE_REPORT_PAGE_COPY.breadcrumbCaptureReport}
        </span>
      </nav>

      {/* 2. Page Header Row */}
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div className="flex flex-col gap-1">
          <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
            {CAPTURE_REPORT_PAGE_COPY.pageTitle}
          </h1>
          <p className="text-sm sm:text-base font-medium text-op-text-muted">
            {CAPTURE_REPORT_PAGE_COPY.pageSubtitle}
          </p>
        </div>

        {/* Header Action Buttons */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Generate Brief Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() =>
              navigate(
                operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
              )
            }
          >
            <img
              src={aiIconPng}
              alt=""
              className="size-4 shrink-0 brightness-0 invert"
            />
            <span>{CAPTURE_REPORT_PAGE_COPY.generateBrief}</span>
          </Button>

          {/* Export Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 gap-2 rounded-xs px-4 text-sm font-medium"
            onClick={() => setIsExportOpen(true)}
          >
            <Download className="size-4" />
            <span>{CAPTURE_REPORT_PAGE_COPY.export}</span>
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
            {CAPTURE_REPORT_PAGE_COPY.emptyTitle}
          </h2>
          <p className="max-w-md text-sm font-medium leading-relaxed text-op-text-muted">
            {CAPTURE_REPORT_PAGE_COPY.emptySubtitle}
          </p>
        </div>
      ) : (
        /* Populated Capture Report Content */
        <div className="flex flex-col gap-6">
          {/* Card 1: 6-KPI Strip */}
          <div className="w-full rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 divide-y sm:divide-y-0 sm:divide-x divide-op-border-default">
              {kpisList.map((kpi, index) => (
                <div
                  key={kpi.label}
                  className={cn(
                    "flex flex-col justify-between gap-1 py-3 sm:py-2",
                    index === 0 ? "sm:pl-2" : "sm:px-4",
                    index === kpisList.length - 1 ? "sm:pr-2" : ""
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

          {/* Card 2: Scan-to-guest funnel */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {CAPTURE_REPORT_PAGE_COPY.funnelSectionTitle}
            </h2>

            {/* Funnel Table */}
            <div className="w-full overflow-hidden rounded-xs border border-op-border-default">
              <div className="grid grid-cols-12 bg-op-surface-secondary/90 border-b border-op-border-default text-sm font-semibold text-op-text-primary">
                <div className="col-span-4 px-4 py-3 border-r border-op-border-default">
                  Step
                </div>
                <div className="col-span-4 px-4 py-3 border-r border-op-border-default">
                  Count
                </div>
                <div className="col-span-4 px-4 py-3">Drop-off</div>
              </div>

              <div className="divide-y divide-op-border-default">
                {data.funnel.map((row) => (
                  <div
                    key={row.step}
                    className="grid grid-cols-12 hover:bg-op-surface-secondary/20 transition-colors"
                  >
                    <div className="col-span-4 px-4 py-3 border-r border-op-border-default text-sm font-semibold text-op-text-primary">
                      {row.step}
                    </div>
                    <div className="col-span-4 px-4 py-3 border-r border-op-border-default text-sm font-normal text-op-text-muted">
                      {row.count}
                    </div>
                    <div className="col-span-4 px-4 py-3 text-sm font-normal text-op-text-muted">
                      {row.dropOff}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* AI Insight Box */}
            <div className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex items-start gap-3">
              <img
                src={aiIconPng}
                alt=""
                className="size-5 shrink-0 brightness-0 invert mt-0.5"
              />
              <p className="text-sm font-medium text-op-text-secondary leading-relaxed max-w-3xl">
                {CAPTURE_REPORT_PAGE_COPY.funnelInsight}
              </p>
            </div>

            {/* Action CTA */}
            <div>
              <Button
                type="button"
                variant="op-primary"
                className="h-10 rounded-xs px-4 text-sm font-medium"
                onClick={handleReviewGuestForm}
              >
                {CAPTURE_REPORT_PAGE_COPY.reviewGuestForm}
              </Button>
            </div>
          </div>

          {/* Card 3: QR placement performance */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 shadow-sm">
            <h2 className="text-xl font-bold text-op-text-primary">
              {CAPTURE_REPORT_PAGE_COPY.placementSectionTitle}
            </h2>

            {/* Placements Table */}
            <div className="w-full overflow-x-auto rounded-xs border border-op-border-default">
              <table className="w-full text-left text-sm">
                <thead className="bg-op-surface-secondary/90 border-b border-op-border-default text-op-text-primary font-semibold">
                  <tr>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[180px]">
                      QR name
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[120px]">
                      Placement
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[100px]">
                      Status
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[80px]">
                      Scans
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[90px]">
                      Feedback
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[110px]">
                      Contactable
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[80px]">
                      Claims
                    </th>
                    <th className="px-4 py-3 border-r border-op-border-default min-w-[100px]">
                      Conversion
                    </th>
                    <th className="px-4 py-3 text-center min-w-[80px]">
                      {CAPTURE_REPORT_PAGE_COPY.actionsMenuLabel}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-op-border-default">
                  {data.placements.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-op-surface-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3 border-r border-op-border-default font-semibold text-op-text-primary">
                        {row.qrName}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-muted">
                        {row.placement}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default">
                        <span className="inline-flex items-center px-2 py-0.5 rounded-xs text-xs font-medium bg-green-600/20 text-green-600">
                          {row.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.scans}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.feedback}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.contactable}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.claims}
                      </td>
                      <td className="px-4 py-3 border-r border-op-border-default text-op-text-primary">
                        {row.conversion}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              className="size-8 text-op-text-primary hover:bg-op-surface-secondary"
                              aria-label={`Actions for ${row.qrName}`}
                            >
                              <MoreVertical className="size-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent
                            align="end"
                            className="w-40 border-op-border-default bg-op-background-primary text-op-text-primary z-[220] py-1"
                          >
                            <DropdownMenuItem
                              className="cursor-pointer text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary/80 focus:bg-op-surface-secondary"
                              onClick={() =>
                                setActivePlacementModal({
                                  actionType: "view-qr",
                                  placement: row,
                                })
                              }
                            >
                              View QR
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="cursor-pointer text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary/80 focus:bg-op-surface-secondary"
                              onClick={() =>
                                setActivePlacementModal({
                                  actionType: "download-pdf",
                                  placement: row,
                                })
                              }
                            >
                              Download PDF
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="cursor-pointer text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary/80 focus:bg-op-surface-secondary"
                              onClick={() =>
                                setActivePlacementModal({
                                  actionType:
                                    row.status === "Active" ? "pause" : "activate",
                                  placement: row,
                                })
                              }
                            >
                              {row.status === "Active" ? "Pause" : "Activate"}
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="cursor-pointer text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary/80 focus:bg-op-surface-secondary"
                              onClick={() =>
                                setActivePlacementModal({
                                  actionType: "duplicate",
                                  placement: row,
                                })
                              }
                            >
                              Duplicate
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="cursor-pointer text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary/80 focus:bg-op-surface-secondary"
                              onClick={() =>
                                setActivePlacementModal({
                                  actionType: "archive",
                                  placement: row,
                                })
                              }
                            >
                              Archive
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* AI Insight Box with Action Button */}
            <div className="w-full p-5 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <div className="flex items-start gap-3 flex-1">
                <img
                  src={aiIconPng}
                  alt=""
                  className="size-5 shrink-0 brightness-0 invert mt-0.5"
                />
                <div className="flex flex-col gap-1">
                  <h3 className="text-sm font-semibold text-op-text-primary">
                    {CAPTURE_REPORT_PAGE_COPY.placementInsightTitle}
                  </h3>
                  <p className="text-sm font-medium text-op-text-secondary leading-relaxed max-w-3xl">
                    {CAPTURE_REPORT_PAGE_COPY.placementInsightSubtitle}
                  </p>
                </div>
              </div>

              <Button
                type="button"
                variant="op-tertiary"
                className="h-10 px-4 py-2.5 rounded-xs text-sm font-medium shrink-0"
                onClick={handleCreatePlacement}
              >
                {CAPTURE_REPORT_PAGE_COPY.createPlacement}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Export Dialog */}
      <ReportsExportDialog
        open={isExportOpen}
        onOpenChange={setIsExportOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
      />

      {/* Reusable Placement Action Modal */}
      <CaptureReportPlacementActionModal
        open={activePlacementModal.actionType != null}
        actionType={activePlacementModal.actionType}
        placement={activePlacementModal.placement}
        locationName={selectedLocationName}
        onOpenChange={(open) => {
          if (!open) {
            setActivePlacementModal({ actionType: null, placement: null })
          }
        }}
      />
    </div>
  )
}
