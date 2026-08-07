import { ChevronDownIcon } from "lucide-react"

import { CampaignsListEmptyState } from "@/components/dashboard/operator/Campaigns/CampaignsListEmptyState"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_SEARCH_MISS_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import type { OperatorCampaignsListViewModel } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SMART_GROUPS_STACK_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TAB_COUNT_ACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorCampaignsListViewId } from "@/types/operatorCampaigns"
import { cn } from "@/lib/utils"

type CampaignsListSectionProps = {
  list: OperatorCampaignsListViewModel
  onViewChange: (viewId: OperatorCampaignsListViewId) => void
  onSearchQueryChange: (query: string) => void
  onCreateCampaign?: () => void
  onUseTemplate?: () => void
  onViewAllCampaigns?: () => void
  onClearAllFilters?: () => void
}

/** Campaigns list — tabs, toolbar, and empty states (Figma 4026:45202 / 4026:45443 / 4027:45669). */
export function CampaignsListSection({
  list,
  onViewChange,
  onSearchQueryChange,
  onCreateCampaign,
  onUseTemplate,
  onViewAllCampaigns,
  onClearAllFilters,
}: CampaignsListSectionProps) {
  const copy = CAMPAIGNS_PAGE_COPY
  const empty = list.empty

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={copy.listSectionTitle}>
      <div className={GUESTS_SMART_GROUPS_STACK_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.listSectionTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.listSectionSubtitle}
          </p>
        </div>

        {list.showListChrome ? (
          <>
            <div className={GUESTS_TABLIST_SCROLL_CLASS}>
              <div
                role="tablist"
                aria-label="Campaign views"
                className={GUESTS_TABLIST_CLASS}
              >
                {list.tabs.map((tab) => {
                  const selected = tab.id === list.activeViewId
                  return (
                    <Button
                      key={tab.id}
                      type="button"
                      variant="op-ghost"
                      role="tab"
                      aria-selected={selected}
                      className={cn(
                        GUESTS_TAB_BUTTON_CLASS,
                        selected
                          ? GUESTS_TAB_BUTTON_ACTIVE_CLASS
                          : GUESTS_TAB_BUTTON_INACTIVE_CLASS
                      )}
                      onClick={() => onViewChange(tab.id)}
                    >
                      <span>{tab.label}</span>
                      {tab.showCount ? (
                        <span
                          className={cn(
                            selected ? GUESTS_TAB_COUNT_ACTIVE_CLASS : ""
                          )}
                        >
                          {tab.count}
                        </span>
                      ) : null}
                    </Button>
                  )
                })}
              </div>
            </div>

            <div className={GUESTS_TOOLBAR_ROW_CLASS}>
              <div className={GUESTS_SEARCH_WRAP_CLASS}>
                <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
                <Input
                  value={list.searchQuery}
                  onChange={(event) =>
                    onSearchQueryChange(event.target.value)
                  }
                  aria-label={copy.searchPlaceholder}
                  placeholder={copy.searchPlaceholder}
                  className={GUESTS_SEARCH_FIELD_CLASS}
                />
              </div>

              <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
                <Button
                  type="button"
                  variant="op-secondary"
                  aria-label={copy.filtersLabel}
                  className="rounded-[2px]"
                  disabled
                >
                  {copy.filtersLabel}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  aria-label={copy.sortLabel}
                  className={GUESTS_SORT_BUTTON_CLASS}
                  disabled
                >
                  {copy.sortLabel}
                  <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
                </Button>
              </div>
            </div>

            {list.searchMissLabel != null ? (
              <p className={CAMPAIGNS_SEARCH_MISS_CLASS}>{list.searchMissLabel}</p>
            ) : null}
          </>
        ) : null}

        <div role="tabpanel">
          {empty != null ? (
            <CampaignsListEmptyState
              empty={empty}
              onCreateCampaign={onCreateCampaign}
              onUseTemplate={onUseTemplate}
              onViewAllCampaigns={onViewAllCampaigns}
              onClearAllFilters={onClearAllFilters}
            />
          ) : null}
        </div>
      </div>
    </section>
  )
}
