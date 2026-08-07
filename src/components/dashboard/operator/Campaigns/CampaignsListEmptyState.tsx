import { Button } from "@/components/ui/button"
import {
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS,
  CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import type { OperatorCampaignsListEmptyViewModel } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CampaignsListEmptyStateProps = {
  empty: OperatorCampaignsListEmptyViewModel
  onCreateCampaign?: () => void
  onUseTemplate?: () => void
  onViewAllCampaigns?: () => void
  onClearAllFilters?: () => void
}

/** Figma empty states — true-empty 4026:45443 / filter-search 4027:45669 / view-scoped Guests-style. */
export function CampaignsListEmptyState({
  empty,
  onCreateCampaign,
  onUseTemplate,
  onViewAllCampaigns,
  onClearAllFilters,
}: CampaignsListEmptyStateProps) {
  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{empty.title}</p>
        <p className={CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS}>{empty.helper}</p>
      </div>

      {empty.kind === "true-empty" ? (
        <div className={CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onCreateCampaign}
          >
            {empty.createCampaignLabel ?? CAMPAIGNS_PAGE_COPY.createCampaign}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onUseTemplate}
          >
            {empty.useTemplateLabel ?? CAMPAIGNS_PAGE_COPY.useTemplate}
          </Button>
        </div>
      ) : null}

      {empty.kind === "filter-search" ? (
        <div className={CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onViewAllCampaigns}
          >
            {empty.viewAllCampaignsLabel ?? CAMPAIGNS_PAGE_COPY.viewAllCampaigns}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onClearAllFilters}
          >
            {empty.clearAllFiltersLabel ?? CAMPAIGNS_PAGE_COPY.clearAllFilters}
          </Button>
        </div>
      ) : null}
    </div>
  )
}
