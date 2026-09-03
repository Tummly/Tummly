import { useNavigate, useSearchParams } from "react-router-dom"
import { MessageSquare, Megaphone, Tag } from "lucide-react"
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
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import { useReportsChildChrome } from "@/components/dashboard/operator/Reports/utils/useReportsChildChrome"
import { ReportsStatusBadge } from "@/components/dashboard/operator/Reports/ReportsStatusBadge"
import {
  CAMPAIGNS_REPORT_PAGE_COPY,
  mockCampaignsReportData,
  type CampaignsReportData,
} from "@/lib/operatorReports/campaignsReportPresentation"
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
import {
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type CampaignsReportPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
  data?: CampaignsReportData
}

export function CampaignsReportPage({
  selectedLocationId = 1,
  selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
  data = mockCampaignsReportData,
}: CampaignsReportPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const reportsChrome = useReportsChildChrome("campaigns", mode)
  const { dateRange, exportAllowed, generateBusy, openExportDialog, commitRange, onGenerateBrief } = reportsChrome

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"

  const reportsBasePath = operatorDashboardNavPath(
    mode,
    "reports",
    selectedLocationId
  )

  const handleCreateCampaign = () => {
    navigate(operatorDashboardNavPath(mode, "campaigns", selectedLocationId))
  }

  const handleAttentionAction = (
    target: "feedback" | "credits-usage" | "shop" | "offers" | "redemption-log"
  ) => {
    switch (target) {
      case "feedback":
        navigate(operatorDashboardNavPath(mode, "feedback", selectedLocationId))
        break
      case "credits-usage":
        navigate(
          operatorDashboardNavPath(
            mode,
            "settings/account-workspace",
            selectedLocationId
          )
        )
        break
      case "shop":
        navigate(operatorDashboardNavPath(mode, "shop", selectedLocationId))
        break
      case "offers":
        navigate(operatorDashboardNavPath(mode, "offers", selectedLocationId))
        break
      case "redemption-log":
        navigate(
          operatorDashboardOffersRedemptionLogPath(mode, selectedLocationId)
        )
        break
    }
  }

  const kpisList = [
    data.kpis.campaignsSent,
    data.kpis.guestsMessaged,
    data.kpis.offerClaims,
    data.kpis.offerRedemptions,
    data.kpis.unsubscribes,
    data.kpis.failedSends,
  ]

  const renderAttentionIcon = (type: "feedback" | "credits" | "offer") => {
    switch (type) {
      case "feedback":
        return (
          <MessageSquare className="size-4 shrink-0 text-op-action-primary" />
        )
      case "credits":
        return <Megaphone className="size-4 shrink-0 text-op-action-primary" />
      case "offer":
        return <Tag className="size-4 shrink-0 text-op-action-primary" />
    }
  }

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
        />
      }
    >
      {isPageEmpty ? (
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
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection>
            <ReportsKpiStrip items={kpisList} />
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
                      {CAMPAIGNS_REPORT_PAGE_COPY.claimsHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.redemptionsHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.unsubscribesHeader}
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      {CAMPAIGNS_REPORT_PAGE_COPY.statusHeader}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.performance.map((row) => (
                    <TableRow
                      key={row.id}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.campaign}
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
                        {row.claims}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.redemptions}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        {row.unsubscribes}
                      </TableCell>
                      <TableCell className={REPORTS_TABLE_BODY_CELL_CLASS}>
                        <ReportsStatusBadge status={row.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </ReportsSection>

          <ReportsSection
            title={CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionTitle}
            subtitle={CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionSubtitle}
          >
            <div className="flex flex-col gap-3">
              {data.attentionItems.map((item) => (
                <div
                  key={item.id}
                  className="flex w-full flex-col items-start justify-between gap-4 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-5 sm:flex-row sm:items-center"
                >
                  <div className="flex flex-1 items-start gap-3.5">
                    <div className="mt-1">{renderAttentionIcon(item.type)}</div>
                    <div className="flex flex-col gap-1">
                      <h3 className="text-base font-semibold leading-6 text-op-text-primary">
                        {item.title}
                      </h3>
                      <p className="text-sm font-medium leading-relaxed text-op-text-primary">
                        {item.description}
                      </p>
                      <p className="pt-0.5 text-xs font-medium text-op-text-muted">
                        {item.meta}
                      </p>
                    </div>
                  </div>

                  <div className="flex shrink-0 items-center gap-2 self-start sm:self-center">
                    {item.actions.map((act) => (
                      <Button
                        key={act.label}
                        type="button"
                        variant="op-tertiary"
                        className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                        onClick={() => handleAttentionAction(act.target)}
                      >
                        {act.label}
                      </Button>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </ReportsSection>
        </div>
      )}

    </ReportsPageChrome>
  )
}
