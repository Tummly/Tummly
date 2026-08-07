import { CampaignsListSection } from "@/components/dashboard/operator/Campaigns/CampaignsListSection"
import { CampaignsMessagingUsage } from "@/components/dashboard/operator/Campaigns/CampaignsMessagingUsage"
import { CampaignsOverviewDateRangeControl } from "@/components/dashboard/operator/Campaigns/CampaignsOverviewDateRangeControl"
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
import type { OperatorCampaignsListViewId } from "@/types/operatorCampaigns"

type CampaignsBodyProps = {
  viewModel: OperatorCampaignsPageViewModel
  /** Visit-scoped store value — keeps the control in sync if refetch fails. */
  selectedDateRange: CampaignsOverviewDateRange
  onCommitDateRange: (range: CampaignsOverviewDateRange) => void
  onListViewChange: (viewId: OperatorCampaignsListViewId) => void
  onSearchQueryChange: (query: string) => void
  onViewAllCampaigns: () => void
  onClearAllFilters: () => void
  /** Inert until Create-campaign wizard tickets land. */
  onCreateCampaign?: () => void
  /** Inert until Use-a-template tickets land. */
  onUseTemplate?: () => void
}

/** Campaigns page body — header chrome, summary KPIs, messaging usage, list tabs + empty states. */
export function CampaignsBody({
  viewModel,
  selectedDateRange,
  onCommitDateRange,
  onListViewChange,
  onSearchQueryChange,
  onViewAllCampaigns,
  onClearAllFilters,
  onCreateCampaign,
  onUseTemplate,
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

      <CampaignsMessagingUsage messagingUsage={viewModel.messagingUsage} />

      <CampaignsListSection
        list={viewModel.list}
        onViewChange={onListViewChange}
        onSearchQueryChange={onSearchQueryChange}
        onCreateCampaign={onCreateCampaign}
        onUseTemplate={onUseTemplate}
        onViewAllCampaigns={onViewAllCampaigns}
        onClearAllFilters={onClearAllFilters}
      />
    </div>
  )
}
