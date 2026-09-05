import { useNavigate } from "react-router-dom"
import { AlertTriangle } from "lucide-react"
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
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsChildChrome } from "@/components/dashboard/operator/Reports/utils/useReportsChildChrome"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import { CAMPAIGNS_REPORT_PAGE_COPY } from "@/lib/operatorReports/campaignsReportPresentation"
import {
  REPORTS_BODY_STACK_CLASS,
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
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type CampaignsReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
}

export function CampaignsReportPage({
  selectedLocationId = 1,
  selectedLocationName: _selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
}: CampaignsReportPageProps) {
  const navigate = useNavigate()
  const setCampaignsIntent = useDashboardUiStore(
    (state) => state.setCampaignsIntent
  )
  const reportsChrome = useReportsChildChrome("campaigns", mode)
  const reports = useReportsPageModule()
  const {
    dateRange,
    exportAllowed,
    generateBusy,
    openExportDialog,
    commitRange,
    onGenerateBrief,
  } = reportsChrome
  const { campaignsLoadStatus, campaignsReport, campaignsLoadError } =
    reports.snapshot

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleCreateCampaign = () => {
    setCampaignsIntent({ openBlankCreate: true })
    navigate(operatorDashboardNavPath(mode, "campaigns", selectedLocationId))
  }

  const showDateRange = campaignsLoadStatus !== "lifetimeEmpty"

  return (
    <ReportsPageChrome
      title={CAMPAIGNS_REPORT_PAGE_COPY.pageTitle}
      subtitle={CAMPAIGNS_REPORT_PAGE_COPY.pageSubtitle}
      breadcrumb={{
        reportsBasePath,
        currentLabel: CAMPAIGNS_REPORT_PAGE_COPY.breadcrumbCampaignsReport,
      }}
      actions={
        <ReportsStandardHeaderActions
          onGenerateBrief={onGenerateBrief}
          generateBusy={generateBusy}
          onExport={openExportDialog}
          exportDisabled={!exportAllowed}
          selectedRange={dateRange}
          onCommitRange={commitRange}
          showDateRange={showDateRange}
        />
      }
    >
      {campaignsLoadStatus === "loading" || campaignsLoadStatus === "idle" ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading campaigns report"
        >
          <Spinner />
        </div>
      ) : null}

      {campaignsLoadStatus === "error" ? (
        <div className="flex flex-col items-start gap-3" role="alert">
          <p className="m-0 text-sm text-destructive">
            {campaignsLoadError ?? CAMPAIGNS_REPORT_PAGE_COPY.loadError}
          </p>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => {
              void reports.retryCampaignsLoad()
            }}
          >
            {CAMPAIGNS_REPORT_PAGE_COPY.retry}
          </Button>
        </div>
      ) : null}

      {campaignsLoadStatus === "lifetimeEmpty" ? (
        <ReportsEmptyState
          title={CAMPAIGNS_REPORT_PAGE_COPY.emptyTitle}
          subtitle={CAMPAIGNS_REPORT_PAGE_COPY.emptySubtitle}
          action={
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={handleCreateCampaign}
            >
              {CAMPAIGNS_REPORT_PAGE_COPY.createCampaign}
            </Button>
          }
        />
      ) : null}

      {campaignsLoadStatus === "ready" && campaignsReport != null ? (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={campaignsReport.kpis} />
          </ReportsSection>

          <ReportsSection
            title={CAMPAIGNS_REPORT_PAGE_COPY.performanceSectionTitle}
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.campaignHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.goalHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.channelHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.sentHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.statusHeader}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {campaignsReport.performance.map((row) => (
                    <TableRow
                      key={row.campaignId}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.name}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.goal}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.channel}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.sent}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.statusLabel} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </ReportsSection>

          {campaignsReport.attentionItems.length > 0 ? (
            <ReportsSection
              title={CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionTitle}
              subtitle={CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionSubtitle}
            >
              <div className="flex flex-col gap-3">
                {campaignsReport.attentionItems.map((item) => (
                  <div
                    key={item.campaignId}
                    className="flex w-full flex-col items-start justify-between gap-4 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-5 sm:flex-row sm:items-center"
                  >
                    <div className="flex flex-1 items-start gap-3.5">
                      <div className="mt-1">
                        <AlertTriangle className="size-4 shrink-0 text-op-action-primary" />
                      </div>
                      <div className="flex flex-col gap-1">
                        <h3 className="text-base font-semibold leading-6 text-op-text-primary">
                          {item.name}
                        </h3>
                        <p className="text-sm font-medium leading-relaxed text-op-text-primary">
                          {item.statusLabel}
                        </p>
                      </div>
                    </div>

                    <div className="flex shrink-0 items-center gap-2 self-start sm:self-center">
                      <Button
                        type="button"
                        variant="op-tertiary"
                        className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                        onClick={() => {
                          navigate(
                            operatorDashboardNavPath(
                              mode,
                              "campaigns",
                              selectedLocationId
                            )
                          )
                        }}
                      >
                        {CAMPAIGNS_REPORT_PAGE_COPY.viewCampaigns}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </ReportsSection>
          ) : null}
        </div>
      ) : null}
    </ReportsPageChrome>
  )
}
