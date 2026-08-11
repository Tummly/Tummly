import { OffersListEmptyState } from "@/components/dashboard/operator/Offers/OffersListEmptyState"
import { OffersListTable } from "@/components/dashboard/operator/Offers/OffersListTable"
import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { ChevronDownIcon } from "lucide-react"
import type { OfferRowActionId } from "@/lib/operatorOffers/offerListPresentation"
import type { OperatorOffersListViewModel } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import {
  OFFERS_PAGE_COPY,
  OFFERS_SEARCH_MISS_CLASS,
  OPERATOR_OFFERS_SORT_OPTIONS,
} from "@/lib/operatorOffers/offersPresentation"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import {
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SMART_GROUPS_STACK_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_SORT_MENU_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TAB_COUNT_ACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  OperatorOffersListViewId,
  OperatorOffersSortId,
} from "@/types/operatorCampaigns"
import { cn } from "@/lib/utils"

type OffersListSectionProps = {
  list: OperatorOffersListViewModel
  onViewChange: (viewId: OperatorOffersListViewId) => void
  onSearchQueryChange: (query: string) => void
  onSortChange: (id: OperatorOffersSortId) => void
  onPreviousPage: () => void
  onNextPage: () => void
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onRowAction: (offerId: number, actionId: OfferRowActionId) => void
  onCreateOffer?: () => void
  onUseTemplate?: () => void
  onViewAllOffers?: () => void
  onClearAllFilters?: () => void
}

/** Offers list — tabs, Filters/Sort, chips, table, pagination (ticket 20). */
export function OffersListSection({
  list,
  onViewChange,
  onSearchQueryChange,
  onSortChange,
  onPreviousPage,
  onNextPage,
  onOpenFilters,
  onRemoveFilterChip,
  onRowAction,
  onCreateOffer,
  onUseTemplate,
  onViewAllOffers,
  onClearAllFilters,
}: OffersListSectionProps) {
  const copy = OFFERS_PAGE_COPY
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
                aria-label="Offer views"
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
                  aria-label={
                    list.filterChipCount > 0
                      ? `Filters, ${list.filterChipCount} applied`
                      : copy.filtersLabel
                  }
                  className="rounded-[2px]"
                  onClick={onOpenFilters}
                >
                  {copy.filtersLabel}
                  {list.filterChipCount > 0
                    ? ` (${list.filterChipCount})`
                    : null}
                </Button>
                <DropdownMenu modal={false}>
                  <DropdownMenuTrigger asChild>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      aria-label={`Sort: ${list.sortLabel}`}
                      className={GUESTS_SORT_BUTTON_CLASS}
                    >
                      Sort: {list.sortLabel}
                      <ChevronDownIcon
                        className="size-3.5 shrink-0"
                        aria-hidden
                      />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className={GUESTS_SORT_MENU_CLASS}
                  >
                    {OPERATOR_OFFERS_SORT_OPTIONS.map(([id, label]) => (
                      <DropdownMenuItem
                        key={id}
                        className={cn(
                          GUESTS_TABLE_MENU_ITEM_CLASS,
                          id === list.sortId
                            && GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS
                        )}
                        onClick={() => onSortChange(id)}
                      >
                        {label}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            <GuestsFilterChipRow
              chips={list.filterChips}
              onRemoveChip={onRemoveFilterChip}
            />

            {list.searchMissLabel != null ? (
              <p className={OFFERS_SEARCH_MISS_CLASS}>{list.searchMissLabel}</p>
            ) : null}
          </>
        ) : null}

        <div role="tabpanel">
          {empty != null ? (
            <OffersListEmptyState
              empty={empty}
              onCreateOffer={onCreateOffer}
              onUseTemplate={onUseTemplate}
              onViewAllOffers={onViewAllOffers}
              onClearAllFilters={onClearAllFilters}
            />
          ) : list.rows.length > 0 ? (
            <OffersListTable rows={list.rows} onRowAction={onRowAction} />
          ) : null}
        </div>

        {list.showListChrome ? (
          <div className={GUESTS_PAGINATION_ROW_CLASS}>
            <p className={GUESTS_PAGINATION_LABEL_CLASS}>
              {list.pageRangeLabel}
            </p>
            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant="op-secondary"
                disabled={!list.canGoPrevious}
                aria-disabled={!list.canGoPrevious}
                aria-label="Previous page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={onPreviousPage}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={!list.canGoNext}
                aria-disabled={!list.canGoNext}
                aria-label="Next page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={onNextPage}
              >
                Next
              </Button>
            </div>
          </div>
        ) : null}
      </div>
    </section>
  )
}
