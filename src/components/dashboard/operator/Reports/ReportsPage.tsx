import { useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
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
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import { ReportsInsightBanner } from "@/components/dashboard/operator/Reports/ReportsInsightBanner"
import { ReportsKpiStrip } from "@/components/dashboard/operator/Reports/ReportsKpiStrip"
import { ReportsPageChrome } from "@/components/dashboard/operator/Reports/ReportsPageChrome"
import { ReportsSection } from "@/components/dashboard/operator/Reports/ReportsSection"
import { ReportsStandardHeaderActions } from "@/components/dashboard/operator/Reports/ReportsStandardHeaderActions"
import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  REPORTS_BODY_STACK_CLASS,
  REPORTS_HUB_PAGE_COPY,
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
  operatorDashboardNavPath,
  operatorDashboardCaptureReportPath,
  operatorDashboardFeedbackReportPath,
  operatorDashboardOffersReportPath,
  operatorDashboardCampaignsReportPath,
  operatorDashboardWeeklyBriefPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

// Structured mock data for easy replacement with live API query
export const mockReportsData = {
  summary: {
    feedbackCount: 42,
    contactableCount: 28,
    topSource: "Delivery inserts",
    quietDayOfferRedemptions: 12,
  },
  funnel: {
    qrScans: { value: "158", delta: "+14% vs previous period", positive: true },
    feedbackReceived: {
      value: "42",
      delta: "+8% vs previous period",
      positive: true,
    },
    contactableGuests: {
      value: "28",
      delta: "+12% vs previous period",
      positive: true,
    },
    offerRedemptions: {
      value: "12",
      delta: "+20% vs previous period",
      positive: true,
    },
    campaignActivity: {
      value: "3",
      delta: "0% vs previous period",
      positive: null,
    },
  },
  privateFeedback: {
    feedbackMessages: {
      value: "42",
      delta: "+8% vs previous period",
      positive: true,
    },
    contactable: {
      value: "28",
      delta: "+12% vs previous period",
      positive: true,
    },
    followUpNeeded: {
      value: "6",
      delta: "6 pending reviews",
      attention: true,
    },
    followedUp: {
      value: "36",
      delta: "+15% vs previous period",
      positive: true,
    },
  },
  captureSources: [
    { source: "Delivery insert", scans: 72, feedback: 18, contactable: 11 },
    { source: "Counter card", scans: 45, feedback: 12, contactable: 7 },
    { source: "Receipt QR", scans: 33, feedback: 15, contactable: 5 },
    { source: "Table card", scans: 88, feedback: 22, contactable: 9 },
  ],
  offersAndCampaigns: {
    activeOffers: {
      value: "3",
      delta: "0% vs previous period",
      positive: null,
    },
    offerClaims: {
      value: "38",
      delta: "+15% vs previous period",
      positive: true,
    },
    offerRedemptions: {
      value: "12",
      delta: "+20% vs previous period",
      positive: true,
    },
    campaignsSent: {
      value: "2",
      delta: "+1 vs previous period",
      positive: true,
    },
    unsubscribes: {
      value: "1",
      delta: "-50% vs previous period",
      positive: true,
    },
  },
}

type ReportsPageProps = {
  selectedLocationId?: number
  selectedLocationName?: string
  locations?: Array<{ id: number; locationName: string; address: string }>
  mode?: DashboardProps["mode"]
  isEmpty?: boolean
}

export function ReportsPage({
  selectedLocationId = 1,
  selectedLocationName = "Mehmet's Grill",
  locations: _locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
}: ReportsPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [dateRange, setDateRange] = useState<HomePerformanceDateRange>(
    DEFAULT_HOME_PERFORMANCE_DATE_RANGE
  )
  const [isExportOpen, setIsExportOpen] = useState(false)

  const isPageEmpty = propIsEmpty ?? searchParams.get("empty") === "true"
  const dateRangeLabel = labelForHomePerformanceDateRange(dateRange)

  const navTo = (
    destination: "feedback" | "capture" | "campaigns" | "offers"
  ) => {
    const path = operatorDashboardNavPath(mode, destination, selectedLocationId)
    navigate(path)
  }

  const funnelKpis = [
    {
      label: "QR scans",
      value: mockReportsData.funnel.qrScans.value,
      delta: mockReportsData.funnel.qrScans.delta,
      positive: mockReportsData.funnel.qrScans.positive,
    },
    {
      label: "Feedback received",
      value: mockReportsData.funnel.feedbackReceived.value,
      delta: mockReportsData.funnel.feedbackReceived.delta,
      positive: mockReportsData.funnel.feedbackReceived.positive,
    },
    {
      label: "Contactable guests",
      value: mockReportsData.funnel.contactableGuests.value,
      delta: mockReportsData.funnel.contactableGuests.delta,
      positive: mockReportsData.funnel.contactableGuests.positive,
    },
    {
      label: "Offer redemptions",
      value: mockReportsData.funnel.offerRedemptions.value,
      delta: mockReportsData.funnel.offerRedemptions.delta,
      positive: mockReportsData.funnel.offerRedemptions.positive,
    },
    {
      label: "Campaign activity",
      value: mockReportsData.funnel.campaignActivity.value,
      delta: mockReportsData.funnel.campaignActivity.delta,
      positive: mockReportsData.funnel.campaignActivity.positive,
    },
  ]

  const privateFeedbackKpis = [
    {
      label: "Feedback messages",
      value: mockReportsData.privateFeedback.feedbackMessages.value,
      delta: mockReportsData.privateFeedback.feedbackMessages.delta,
      positive: mockReportsData.privateFeedback.feedbackMessages.positive,
    },
    {
      label: "Contactable",
      value: mockReportsData.privateFeedback.contactable.value,
      delta: mockReportsData.privateFeedback.contactable.delta,
      positive: mockReportsData.privateFeedback.contactable.positive,
    },
    {
      label: "Follow-up needed",
      value: mockReportsData.privateFeedback.followUpNeeded.value,
      delta: mockReportsData.privateFeedback.followUpNeeded.delta,
      positive: null,
    },
    {
      label: "Followed up",
      value: mockReportsData.privateFeedback.followedUp.value,
      delta: mockReportsData.privateFeedback.followedUp.delta,
      positive: mockReportsData.privateFeedback.followedUp.positive,
    },
  ]

  const offersKpis = [
    {
      label: "Active offers",
      value: mockReportsData.offersAndCampaigns.activeOffers.value,
      delta: mockReportsData.offersAndCampaigns.activeOffers.delta,
      positive: mockReportsData.offersAndCampaigns.activeOffers.positive,
    },
    {
      label: "Offer claims",
      value: mockReportsData.offersAndCampaigns.offerClaims.value,
      delta: mockReportsData.offersAndCampaigns.offerClaims.delta,
      positive: mockReportsData.offersAndCampaigns.offerClaims.positive,
    },
    {
      label: "Offer redemptions",
      value: mockReportsData.offersAndCampaigns.offerRedemptions.value,
      delta: mockReportsData.offersAndCampaigns.offerRedemptions.delta,
      positive: mockReportsData.offersAndCampaigns.offerRedemptions.positive,
    },
    {
      label: "Campaigns sent",
      value: mockReportsData.offersAndCampaigns.campaignsSent.value,
      delta: mockReportsData.offersAndCampaigns.campaignsSent.delta,
      positive: mockReportsData.offersAndCampaigns.campaignsSent.positive,
    },
    {
      label: "Unsubscribes",
      value: mockReportsData.offersAndCampaigns.unsubscribes.value,
      delta: mockReportsData.offersAndCampaigns.unsubscribes.delta,
      positive: mockReportsData.offersAndCampaigns.unsubscribes.positive,
    },
  ]

  return (
    <ReportsPageChrome
      title={REPORTS_HUB_PAGE_COPY.title}
      subtitle={REPORTS_HUB_PAGE_COPY.subtitle}
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
        <ReportsEmptyState />
      ) : (
        <div className={REPORTS_BODY_STACK_CLASS}>
          <ReportsSection title="This week's guest loop">
            <ReportsInsightBanner>
              You received {mockReportsData.summary.feedbackCount} feedback
              messages this week and captured{" "}
              {mockReportsData.summary.contactableCount} contactable guests.{" "}
              {mockReportsData.summary.topSource} created the most feedback,
              while your quiet-day offer drove{" "}
              {mockReportsData.summary.quietDayOfferRedemptions} redemptions.
            </ReportsInsightBanner>

            <div className="flex flex-wrap items-center gap-3">
              <Button
                type="button"
                variant="op-primary"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardWeeklyBriefPath(mode, selectedLocationId)
                  )
                }
              >
                View weekly brief
              </Button>

              <Button
                type="button"
                variant="outline"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() => navTo("campaigns")}
              >
                Create campaign
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Guest Loop funnel"
            subtitle="See where guests move from scan to feedback, contact and offer use."
          >
            <ReportsKpiStrip items={funnelKpis} />
          </ReportsSection>

          <ReportsSection
            title="Private feedback"
            subtitle="What guests told you through the feedback form."
          >
            <ReportsKpiStrip items={privateFeedbackKpis} />

            <ReportsInsightBanner>
              Common themes this period: guests mentioned delivery packaging,
              wait time during busy periods and friendly staff. A few comments
              may need follow-up because the guest shared contact details and
              raised a specific issue.
            </ReportsInsightBanner>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardFeedbackReportPath(
                      mode,
                      selectedLocationId
                    )
                  )
                }
              >
                Open feedback report
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Top capture sources"
            subtitle="Which QR placements generated guest activity."
          >
            <div className={REPORTS_TABLE_FRAME_CLASS}>
              <Table className={REPORTS_TABLE_CLASS}>
                <TableHeader>
                  <TableRow className={REPORTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Source
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Scans
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback
                    </TableHead>
                    <TableHead className={REPORTS_TABLE_HEAD_CELL_CLASS}>
                      Contactable guests
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mockReportsData.captureSources.map((row) => (
                    <TableRow
                      key={row.source}
                      className={REPORTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={REPORTS_TABLE_NAME_CELL_CLASS}>
                        {row.source}
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
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardCaptureReportPath(
                      mode,
                      selectedLocationId
                    )
                  )
                }
              >
                View capture report
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Offers and campaigns"
            subtitle="Track claims, redemptions and campaign response."
          >
            <ReportsKpiStrip items={offersKpis} />

            <ReportsInsightBanner>
              Your quiet-day offer had the most redemptions this period. One
              campaign caused more opt-outs than usual, so review the audience
              before sending again.
            </ReportsInsightBanner>

            <div className="flex flex-wrap items-center gap-3">
              <Button
                type="button"
                variant="op-primary"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardOffersReportPath(
                      mode,
                      selectedLocationId
                    )
                  )
                }
              >
                View offers report
              </Button>
              <Button
                type="button"
                variant="outline"
                className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                onClick={() =>
                  navigate(
                    operatorDashboardCampaignsReportPath(
                      mode,
                      selectedLocationId
                    )
                  )
                }
              >
                View campaign reports
              </Button>
            </div>
          </ReportsSection>

          <ReportsSection
            title="Recommended actions"
            subtitle="Practical next steps based on this period's activity."
          >
            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-4">
                <ReportsInsightBanner title="Check delivery packaging feedback">
                  Several recent comments mentioned delivery packaging. Review
                  the original messages before making changes.
                </ReportsInsightBanner>
                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                    onClick={() => navTo("feedback")}
                  >
                    View feedback
                  </Button>
                </div>
              </div>

              <div className="flex flex-col gap-4">
                <ReportsInsightBanner title="Move your counter QR closer to payment">
                  Your counter card is getting fewer scans than delivery
                  inserts. Try placing it closer to the payment area.
                </ReportsInsightBanner>
                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                    onClick={() => navTo("capture")}
                  >
                    View QR codes
                  </Button>
                </div>
              </div>

              <div className="flex flex-col gap-4">
                <ReportsInsightBanner title="Follow up with 6 guests">
                  These guests shared contact details and may need a response.
                </ReportsInsightBanner>
                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
                    onClick={() => navTo("feedback")}
                  >
                    Open follow-up queue
                  </Button>
                </div>
              </div>
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
    </ReportsPageChrome>
  )
}
