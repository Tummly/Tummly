import { Fragment, useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { MoreVertical } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import {
  CaptureReportPlacementActionModal,
  type CaptureReportPlacementActionType,
} from "@/components/dashboard/operator/Reports/CaptureReportPlacementActionModal"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsInsightBanner } from "@/components/dashboard/operator/Reports/ReportsInsightBanner"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  CAPTURE_REPORT_PAGE_COPY,
  mockCaptureReportData,
  type CaptureReportData,
  type CaptureReportPlacementRow,
} from "@/lib/operatorReports/captureReportPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_ROW_ACTIONS_ITEM_CLASS,
  REPORTS_ROW_ACTIONS_MENU_CLASS,
  REPORTS_ROW_ACTIONS_SEPARATOR_CLASS,
  REPORTS_ROW_ACTIONS_TRIGGER_CLASS,
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
  operatorDashboardCaptureLocationPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

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
  locations: _locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockCaptureReportData,
}: CaptureReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [dateRange, setDateRange] = useState<HomePerformanceDateRange>(
    DEFAULT_HOME_PERFORMANCE_DATE_RANGE
  )
  const [isExportOpen, setIsExportOpen] = useState(false)
  const [activePlacementModal, setActivePlacementModal] = useState<{
    actionType: CaptureReportPlacementActionType | null
    placement: CaptureReportPlacementRow | null
  }>({
    actionType: null,
    placement: null,
  })

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"
  const dateRangeLabel = labelForHomePerformanceDateRange(dateRange)

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
    <ReportsPageChrome
      title={CAPTURE_REPORT_PAGE_COPY.pageTitle}
      subtitle={CAPTURE_REPORT_PAGE_COPY.pageSubtitle}
      breadcrumb={{
        reportsBasePath,
        currentLabel: CAPTURE_REPORT_PAGE_COPY.breadcrumbCaptureReport,
      }}
      actions={
        <ReportsStandardHeaderActions
          onGenerateBrief={() =>
            navigate(
              operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
            )
          }
          onExport={() => setIsExportOpen(true)}
          selectedRange={dateRange}
          onCommitRange={setDateRange}
        />
      }
    >
      {isPageEmpty ? (
        <ReportsEmptyState
          title={CAPTURE_REPORT_PAGE_COPY.emptyTitle}
          subtitle={CAPTURE_REPORT_PAGE_COPY.emptySubtitle}
        />
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={kpisList} />
          </ReportsSection>

          <ReportsSection title={CAPTURE_REPORT_PAGE_COPY.funnelSectionTitle}>
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Step
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Count
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Drop-off
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.funnel.map((row) => (
                    <TableRow
                      key={row.step}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.step}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.count}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.dropOff}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <ReportsInsightBanner>
              {CAPTURE_REPORT_PAGE_COPY.funnelInsight}
            </ReportsInsightBanner>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
                onClick={handleReviewGuestForm}
              >
                {CAPTURE_REPORT_PAGE_COPY.reviewGuestForm}
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title={CAPTURE_REPORT_PAGE_COPY.placementSectionTitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      QR name
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Placement
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Status
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Scans
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Contactable
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Claims
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Conversion
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                      {CAPTURE_REPORT_PAGE_COPY.actionsMenuLabel}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.placements.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.qrName}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.placement}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.status} />
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.scans}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.feedback}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.contactable}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.claims}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.conversion}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_ACTIONS_CELL_CLASS}>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              type="button"
                              variant="op-ghost"
                              size="icon"
                              className={REPORTS_ROW_ACTIONS_TRIGGER_CLASS}
                              aria-label={`Actions for ${row.qrName}`}
                            >
                              <MoreVertical className="size-4" aria-hidden />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent
                            align="end"
                            className={REPORTS_ROW_ACTIONS_MENU_CLASS}
                          >
                            {(
                              [
                                {
                                  id: "view-qr",
                                  label: "View QR",
                                  actionType: "view-qr" as const,
                                },
                                {
                                  id: "download-pdf",
                                  label: "Download PDF",
                                  actionType: "download-pdf" as const,
                                },
                                {
                                  id: "pause-activate",
                                  label:
                                    row.status === "Active"
                                      ? "Pause"
                                      : "Activate",
                                  actionType:
                                    row.status === "Active"
                                      ? ("pause" as const)
                                      : ("activate" as const),
                                },
                                {
                                  id: "duplicate",
                                  label: "Duplicate",
                                  actionType: "duplicate" as const,
                                },
                                {
                                  id: "archive",
                                  label: "Archive",
                                  actionType: "archive" as const,
                                },
                              ] as const
                            ).map((item, index) => (
                              <Fragment key={item.id}>
                                {index > 0 ? (
                                  <DropdownMenuSeparator
                                    className={
                                      REPORTS_ROW_ACTIONS_SEPARATOR_CLASS
                                    }
                                  />
                                ) : null}
                                <DropdownMenuItem
                                  className={REPORTS_ROW_ACTIONS_ITEM_CLASS}
                                  onClick={() =>
                                    setActivePlacementModal({
                                      actionType: item.actionType,
                                      placement: row,
                                    })
                                  }
                                >
                                  {item.label}
                                </DropdownMenuItem>
                              </Fragment>
                            ))}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <ReportsInsightBanner
                title={CAPTURE_REPORT_PAGE_COPY.placementInsightTitle}
                className="flex-1"
              >
                {CAPTURE_REPORT_PAGE_COPY.placementInsightSubtitle}
              </ReportsInsightBanner>
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={handleCreatePlacement}
              >
                {CAPTURE_REPORT_PAGE_COPY.createPlacement}
              </Button>
            </div>
          </ReportsSection>
        </div>
      )}

      <ReportsExportDialog
        open={isExportOpen}
        onOpenChange={setIsExportOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
      />

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
    </ReportsPageChrome>
  )
}
