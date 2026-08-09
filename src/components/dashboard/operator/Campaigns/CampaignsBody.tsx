import { CampaignsListSection } from "@/components/dashboard/operator/Campaigns/CampaignsListSection"
import { CampaignsMessagingUsage } from "@/components/dashboard/operator/Campaigns/CampaignsMessagingUsage"
import { CampaignsOverviewDateRangeControl } from "@/components/dashboard/operator/Campaigns/CampaignsOverviewDateRangeControl"
import { CampaignsRecommendedNextStep } from "@/components/dashboard/operator/Campaigns/CampaignsRecommendedNextStep"
import { CampaignsSummary } from "@/components/dashboard/operator/Campaigns/CampaignsSummary"
import { Button } from "@/components/ui/button"
import {
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_PAGE_META_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  labelForCampaignsOverviewDateRange,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import type { OperatorCampaignsPageViewModel } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  CampaignRecommendation,
  OperatorCampaignsListViewId,
} from "@/types/operatorCampaigns"

type CampaignsBodyProps = {
  viewModel: OperatorCampaignsPageViewModel
  /** Visit-scoped store value — keeps the control in sync if refetch fails. */
  selectedDateRange: CampaignsOverviewDateRange
  onCommitDateRange: (range: CampaignsOverviewDateRange) => void
  onListViewChange: (viewId: OperatorCampaignsListViewId) => void
  onSearchQueryChange: (query: string) => void
  onViewAllCampaigns: () => void
  onClearAllFilters: () => void
  onContinueEditing: (campaignId: number) => void
  /** Opens blank Create campaign wizard at Goal (ticket 22). */
  onCreateCampaign?: () => void
  /** Opens the template catalogue picker (ticket 21). */
  onUseTemplate?: () => void
  onRetryRecommendation?: () => void
  onReviewRecommendationDraft?: (
    recommendation: CampaignRecommendation
  ) => void
  onViewRecommendationAudience?: () => void
  onCloseRecommendationAudience?: () => void
  onDismissRecommendation?: () => void
  onRetryMessagingUsage?: () => void
}

/** Campaigns page body — header, summary, messaging usage, recommended, list (Figma stack). */
export function CampaignsBody({
  viewModel,
  selectedDateRange,
  onCommitDateRange,
  onListViewChange,
  onSearchQueryChange,
  onViewAllCampaigns,
  onClearAllFilters,
  onContinueEditing,
  onCreateCampaign,
  onUseTemplate,
  onRetryRecommendation,
  onReviewRecommendationDraft,
  onViewRecommendationAudience,
  onCloseRecommendationAudience,
  onDismissRecommendation,
  onRetryMessagingUsage,
}: CampaignsBodyProps) {
  const copy = CAMPAIGNS_PAGE_COPY
  const dateRangeLabel = labelForCampaignsOverviewDateRange(selectedDateRange)

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
          <p className={CAMPAIGNS_PAGE_META_CLASS}>
            {viewModel.locationName} · {dateRangeLabel}
          </p>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onCreateCampaign}
          >
            {viewModel.header.createCampaignLabel}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onUseTemplate}
          >
            {viewModel.header.useTemplateLabel}
          </Button>
          <CampaignsOverviewDateRangeControl
            dateRangeLabel={dateRangeLabel}
            selectedRange={selectedDateRange}
            onCommitRange={onCommitDateRange}
          />
        </div>
      </div>

      <CampaignsSummary summary={viewModel.summary} />

      <CampaignsMessagingUsage
        messagingUsage={viewModel.messagingUsage}
        onRetry={onRetryMessagingUsage}
      />

      <CampaignsRecommendedNextStep
        recommendation={viewModel.recommendation}
        locationName={viewModel.locationName}
        dateRangeLabel={dateRangeLabel}
        onRetry={() => {
          onRetryRecommendation?.()
        }}
        onReviewDraft={(recommendation) => {
          onReviewRecommendationDraft?.(recommendation)
        }}
        onViewAudience={() => {
          onViewRecommendationAudience?.()
        }}
        onCloseAudience={() => {
          onCloseRecommendationAudience?.()
        }}
        onNotNow={() => {
          onDismissRecommendation?.()
        }}
      />

      <CampaignsListSection
        list={viewModel.list}
        onViewChange={onListViewChange}
        onSearchQueryChange={onSearchQueryChange}
        onContinueEditing={onContinueEditing}
        onCreateCampaign={onCreateCampaign}
        onUseTemplate={onUseTemplate}
        onViewAllCampaigns={onViewAllCampaigns}
        onClearAllFilters={onClearAllFilters}
      />
    </div>
  )
}
