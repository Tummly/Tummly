import { useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import {
  Calendar,
  ChevronDown,
  Download,
  ArrowUpRight,
  ArrowDownRight,
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
import { ReportsEmptyState } from "@/components/dashboard/operator/Reports/ReportsEmptyState"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import { cn } from "@/lib/utils"

type DatePreset = "7d" | "30d" | "90d" | "month" | "ytd"

const DATE_PRESET_LABELS: Record<DatePreset, string> = {
  "7d": "Last 7 days",
  "30d": "Last 30 days",
  "90d": "Last 90 days",
  month: "This month",
  ytd: "Year to date",
}

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
    feedbackReceived: { value: "42", delta: "+8% vs previous period", positive: true },
    contactableGuests: { value: "28", delta: "+12% vs previous period", positive: true },
    offerRedemptions: { value: "12", delta: "+20% vs previous period", positive: true },
    campaignActivity: { value: "3", delta: "0% vs previous period", positive: null },
  },
  privateFeedback: {
    feedbackMessages: { value: "42", delta: "+8% vs previous period", positive: true },
    contactable: { value: "28", delta: "+12% vs previous period", positive: true },
    followUpNeeded: { value: "6", delta: "6 pending reviews", attention: true },
    followedUp: { value: "36", delta: "+15% vs previous period", positive: true },
  },
  captureSources: [
    { source: "Delivery insert", scans: 72, feedback: 18, contactable: 11 },
    { source: "Counter card", scans: 45, feedback: 12, contactable: 7 },
    { source: "Receipt QR", scans: 33, feedback: 15, contactable: 5 },
    { source: "Table card", scans: 88, feedback: 22, contactable: 9 },
  ],
  offersAndCampaigns: {
    activeOffers: { value: "3", delta: "0% vs previous period", positive: null },
    offerClaims: { value: "38", delta: "+15% vs previous period", positive: true },
    offerRedemptions: { value: "12", delta: "+20% vs previous period", positive: true },
    campaignsSent: { value: "2", delta: "+1 vs previous period", positive: true },
    unsubscribes: { value: "1", delta: "-50% vs previous period", positive: true },
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
  locations = [],
  mode = "single",
  isEmpty: propIsEmpty,
}: ReportsPageProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [datePreset, setDatePreset] = useState<DatePreset>("7d")
  const [isWeeklyBriefOpen, setIsWeeklyBriefOpen] = useState(false)
  const [isExportOpen, setIsExportOpen] = useState(false)

  const isPageEmpty = propIsEmpty ?? (searchParams.get("empty") === "true")
  const dateRangeLabel = DATE_PRESET_LABELS[datePreset]

  const navTo = (destination: "feedback" | "capture" | "campaigns" | "offers") => {
    const path = operatorDashboardNavPath(mode, destination, selectedLocationId)
    navigate(path)
  }

  return (
    <div className="w-full flex flex-col gap-7">
      {/* Page Header */}
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div className="flex flex-col gap-1">
          <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
            Reports
          </h1>
          <p className="text-sm font-medium text-op-text-muted">
            See what is working across guest capture, private feedback, offers and campaigns.
          </p>
        </div>

        {/* Header Action Buttons */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Generate Brief Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-9 gap-2 rounded-xs px-3.5 text-xs font-medium"
            onClick={() => setIsWeeklyBriefOpen(true)}
          >
            <img src={aiIconPng} alt="" className="size-3.5 shrink-0 brightness-0" />
            <span>Generate brief</span>
          </Button>

          {/* Export Button */}
          <Button
            type="button"
            variant="op-secondary"
            className="h-9 gap-2 rounded-xs px-3.5 text-xs font-medium"
            onClick={() => setIsExportOpen(true)}
          >
            <Download className="size-3.5" />
            <span>Export</span>
          </Button>

          {/* Date Range Selector */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="outline"
                className="h-9 gap-2 rounded-xs border-op-border-default bg-transparent px-3.5 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
              >
                <Calendar className="size-3.5 text-op-text-muted" />
                <span>{dateRangeLabel}</span>
                <ChevronDown className="size-3.5 text-op-text-muted" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className="w-44 border-op-border-default bg-op-background-primary text-op-text-primary"
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
        /* Empty State View */
        <ReportsEmptyState />
      ) : (
        /* Populated 6-Card View with realistic mock values */
        <>
          {/* 1. This week's guest loop Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex items-center gap-2.5">
              <img src={aiIconPng} alt="" className="size-4 shrink-0 brightness-0" />
              <h2 className="text-base sm:text-lg font-bold text-op-text-primary">
                This week's guest loop
              </h2>
            </div>

            <div className="w-full flex flex-col gap-6 rounded-sm bg-op-background-primary/80 border border-op-border-default/60 p-6">
              <p className="max-w-4xl text-sm font-normal leading-relaxed text-op-text-primary">
                You received {mockReportsData.summary.feedbackCount} feedback messages this week and captured {mockReportsData.summary.contactableCount} contactable guests. {mockReportsData.summary.topSource} created the most feedback, while your quiet-day offer drove {mockReportsData.summary.quietDayOfferRedemptions} redemptions.
              </p>

              <div className="flex flex-wrap items-center gap-3">
                <Button
                  type="button"
                  variant="op-primary"
                  className="h-9 rounded-xs px-4 text-xs font-medium"
                  onClick={() => setIsWeeklyBriefOpen(true)}
                >
                  View weekly brief
                </Button>

                <Button
                  type="button"
                  variant="outline"
                  className="h-9 rounded-xs border-op-border-default bg-transparent px-4 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                  onClick={() => navTo("campaigns")}
                >
                  Create campaign
                </Button>
              </div>
            </div>
          </div>

          {/* 2. Guest Loop funnel Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex flex-col gap-1">
              <h2 className="text-lg sm:text-xl font-bold text-op-text-primary">
                Guest Loop funnel
              </h2>
              <p className="text-xs sm:text-sm font-medium text-op-text-muted">
                See where guests move from scan to feedback, contact and offer use.
              </p>
            </div>

            {/* 5-KPI Strip with clean vertical dividing lines */}
            <div className="w-full grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6 lg:gap-0 lg:divide-x lg:divide-op-border-default pt-2">
              {/* KPI 1 */}
              <div className="flex flex-col gap-1 lg:pr-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">QR scans</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.funnel.qrScans.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.funnel.qrScans.delta}</span>
                </div>
              </div>

              {/* KPI 2 */}
              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Feedback received</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.funnel.feedbackReceived.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.funnel.feedbackReceived.delta}</span>
                </div>
              </div>

              {/* KPI 3 */}
              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Contactable guests</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.funnel.contactableGuests.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.funnel.contactableGuests.delta}</span>
                </div>
              </div>

              {/* KPI 4 */}
              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Offer redemptions</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.funnel.offerRedemptions.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.funnel.offerRedemptions.delta}</span>
                </div>
              </div>

              {/* KPI 5 */}
              <div className="flex flex-col gap-1 lg:pl-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Campaign activity</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.funnel.campaignActivity.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-text-muted pt-0.5">
                  <span>{mockReportsData.funnel.campaignActivity.delta}</span>
                </div>
              </div>
            </div>
          </div>

          {/* 3. Private feedback Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex flex-col gap-1">
              <h2 className="text-lg sm:text-xl font-bold text-op-text-primary">
                Private feedback
              </h2>
              <p className="text-xs sm:text-sm font-medium text-op-text-muted">
                What guests told you through the feedback form.
              </p>
            </div>

            {/* 4-KPI Strip with clean vertical dividing lines */}
            <div className="w-full grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6 md:gap-0 md:divide-x md:divide-op-border-default pt-2">
              <div className="flex flex-col gap-1 md:pr-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Feedback messages</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.privateFeedback.feedbackMessages.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.privateFeedback.feedbackMessages.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 md:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Contactable</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.privateFeedback.contactable.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.privateFeedback.contactable.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 md:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Follow-up needed</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.privateFeedback.followUpNeeded.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-amber-500 pt-0.5">
                  <span>{mockReportsData.privateFeedback.followUpNeeded.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 md:pl-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Followed up</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.privateFeedback.followedUp.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.privateFeedback.followedUp.delta}</span>
                </div>
              </div>
            </div>

            {/* AI Insight banner */}
            <div className="flex items-start gap-3 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-4">
              <img src={aiIconPng} alt="" className="mt-0.5 size-4 shrink-0 brightness-0" />
              <p className="text-xs sm:text-sm font-medium leading-relaxed text-op-text-muted">
                Common themes this period: guests mentioned delivery packaging, wait time during busy periods and friendly staff. A few comments may need follow-up because the guest shared contact details and raised a specific issue.
              </p>
            </div>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className="h-9 rounded-xs px-4 text-xs font-medium"
                onClick={() => navTo("feedback")}
              >
                Open feedback report
              </Button>
            </div>
          </div>

          {/* 4. Top capture sources Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex flex-col gap-1">
              <h2 className="text-lg sm:text-xl font-bold text-op-text-primary">
                Top capture sources
              </h2>
              <p className="text-xs sm:text-sm font-medium text-op-text-muted">
                Which QR placements generated guest activity.
              </p>
            </div>

            {/* Clean data table */}
            <div className="w-full overflow-hidden rounded-sm border border-op-border-default/60 bg-op-background-primary">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs sm:text-sm">
                  <thead className="border-b border-op-border-default bg-op-surface-secondary/80 text-xs font-medium text-op-text-muted">
                    <tr>
                      <th className="px-5 py-3.5">Source</th>
                      <th className="px-5 py-3.5">Scans</th>
                      <th className="px-5 py-3.5">Feedback</th>
                      <th className="px-5 py-3.5">Contactable guests</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-op-border-default/60">
                    {mockReportsData.captureSources.map((row) => (
                      <tr key={row.source} className="hover:bg-op-surface-secondary/20 transition-colors">
                        <td className="px-5 py-3.5 font-semibold text-op-text-primary">
                          {row.source}
                        </td>
                        <td className="px-5 py-3.5 text-op-text-muted">
                          {row.scans}
                        </td>
                        <td className="px-5 py-3.5 text-op-text-muted">
                          {row.feedback}
                        </td>
                        <td className="px-5 py-3.5 text-op-text-muted">
                          {row.contactable}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className="h-9 rounded-xs px-4 text-xs font-medium"
                onClick={() => navTo("capture")}
              >
                View capture report
              </Button>
            </div>
          </div>

          {/* 5. Offers and campaigns Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex flex-col gap-1">
              <h2 className="text-lg sm:text-xl font-bold text-op-text-primary">
                Offers and campaigns
              </h2>
              <p className="text-xs sm:text-sm font-medium text-op-text-muted">
                Track claims, redemptions and campaign response.
              </p>
            </div>

            {/* 5-KPI Strip with clean vertical dividing lines */}
            <div className="w-full grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6 lg:gap-0 lg:divide-x lg:divide-op-border-default pt-2">
              <div className="flex flex-col gap-1 lg:pr-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Active offers</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.offersAndCampaigns.activeOffers.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-text-muted pt-0.5">
                  <span>{mockReportsData.offersAndCampaigns.activeOffers.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Offer claims</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.offersAndCampaigns.offerClaims.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.offersAndCampaigns.offerClaims.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Offer redemptions</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.offersAndCampaigns.offerRedemptions.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.offersAndCampaigns.offerRedemptions.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 lg:px-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Campaigns sent</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.offersAndCampaigns.campaignsSent.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowUpRight className="size-3 shrink-0" />
                  <span>{mockReportsData.offersAndCampaigns.campaignsSent.delta}</span>
                </div>
              </div>

              <div className="flex flex-col gap-1 lg:pl-6">
                <span className="text-xs sm:text-sm font-medium text-op-text-muted">Unsubscribes</span>
                <span className="text-3xl sm:text-4xl font-extrabold text-op-text-primary tracking-tight">
                  {mockReportsData.offersAndCampaigns.unsubscribes.value}
                </span>
                <div className="flex items-center gap-1 text-[11px] sm:text-xs font-semibold text-op-kpi-info-color pt-0.5">
                  <ArrowDownRight className="size-3 shrink-0" />
                  <span>{mockReportsData.offersAndCampaigns.unsubscribes.delta}</span>
                </div>
              </div>
            </div>

            {/* AI Insight banner */}
            <div className="flex items-start gap-3 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-4">
              <img src={aiIconPng} alt="" className="mt-0.5 size-4 shrink-0 brightness-0" />
              <p className="text-xs sm:text-sm font-medium leading-relaxed text-op-text-muted">
                Your quiet-day offer had the most redemptions this period. One campaign caused more opt-outs than usual, so review the audience before sending again.
              </p>
            </div>

            <div>
              <Button
                type="button"
                variant="op-primary"
                className="h-9 rounded-xs px-4 text-xs font-medium"
                onClick={() => navTo("campaigns")}
              >
                View campaign reports
              </Button>
            </div>
          </div>

          {/* 6. Recommended actions Card */}
          <div className="w-full flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-7 sm:p-8">
            <div className="flex items-center gap-2.5">
              <img src={aiIconPng} alt="" className="size-4 shrink-0 brightness-0" />
              <div className="flex flex-col gap-0.5">
                <h2 className="text-lg sm:text-xl font-bold text-op-text-primary">
                  Recommended actions
                </h2>
                <p className="text-xs sm:text-sm font-medium text-op-text-muted">
                  Practical next steps based on this period's activity.
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-4">
              {/* Action 1 */}
              <div className="flex flex-col gap-3.5 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-6">
                <div className="flex flex-col gap-1">
                  <h3 className="text-sm sm:text-base font-semibold text-op-text-primary">
                    Check delivery packaging feedback
                  </h3>
                  <p className="max-w-2xl text-xs sm:text-sm font-normal leading-relaxed text-op-text-muted">
                    Several recent comments mentioned delivery packaging. Review the original messages before making changes.
                  </p>
                </div>

                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-9 rounded-xs border-op-border-default bg-transparent px-4 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                    onClick={() => navTo("feedback")}
                  >
                    View feedback
                  </Button>
                </div>
              </div>

              {/* Action 2 */}
              <div className="flex flex-col gap-3.5 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-6">
                <div className="flex flex-col gap-1">
                  <h3 className="text-sm sm:text-base font-semibold text-op-text-primary">
                    Move your counter QR closer to payment
                  </h3>
                  <p className="max-w-2xl text-xs sm:text-sm font-normal leading-relaxed text-op-text-muted">
                    Your counter card is getting fewer scans than delivery inserts. Try placing it closer to the payment area.
                  </p>
                </div>

                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-9 rounded-xs border-op-border-default bg-transparent px-4 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                    onClick={() => navTo("capture")}
                  >
                    View QR codes
                  </Button>
                </div>
              </div>

              {/* Action 3 */}
              <div className="flex flex-col gap-3.5 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-6">
                <div className="flex flex-col gap-1">
                  <h3 className="text-sm sm:text-base font-semibold text-op-text-primary">
                    Follow up with 6 guests
                  </h3>
                  <p className="max-w-2xl text-xs sm:text-sm font-normal leading-relaxed text-op-text-muted">
                    These guests shared contact details and may need a response.
                  </p>
                </div>

                <div>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-9 rounded-xs border-op-border-default bg-transparent px-4 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                    onClick={() => navTo("feedback")}
                  >
                    Open follow-up queue
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Modals */}
      <ReportsWeeklyBriefDialog
        open={isWeeklyBriefOpen}
        onOpenChange={setIsWeeklyBriefOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
        onNavigateToFeedback={() => navTo("feedback")}
        onNavigateToCampaigns={() => navTo("campaigns")}
        onNavigateToCapture={() => navTo("capture")}
      />

      <ReportsExportDialog
        open={isExportOpen}
        onOpenChange={setIsExportOpen}
        locationName={selectedLocationName}
        dateRangeLabel={dateRangeLabel}
      />
    </div>
  )
}
