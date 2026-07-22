import { Button } from "@/components/ui/button"
import { GuestsOverview } from "@/components/dashboard/operator/Guests/GuestsOverview"
import { GuestsSmartGroupsSection } from "@/components/dashboard/operator/Guests/GuestsSmartGroupsSection"
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
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

type GuestsBodyProps = {
  viewModel: OperatorGuestsViewModel
  searchQuery: string
  sortId: OperatorGuestSortId
  bulkSelectionLabel: string | null
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
  isGuestSelected: (guestId: string) => boolean
  onSmartGroupChange: (id: OperatorGuestSmartGroupId) => void
  onSearchQueryChange: (query: string) => void
  onSortChange: (id: OperatorGuestSortId) => void
  onPreviousPage: () => void
  onNextPage: () => void
  onToggleGuestSelection: (guestId: string) => void
  onToggleSelectAllVisibleRows: () => void
  onClearSelection: () => void
  onClearSearchAndFilters: () => void
}

/** Guests page body — overview KPIs and Smart Groups table from live API. */
export function GuestsBody({
  viewModel,
  searchQuery,
  sortId,
  bulkSelectionLabel,
  isAllVisibleSelected,
  isSomeVisibleSelected,
  isGuestSelected,
  onSmartGroupChange,
  onSearchQueryChange,
  onSortChange,
  onPreviousPage,
  onNextPage,
  onToggleGuestSelection,
  onToggleSelectAllVisibleRows,
  onClearSelection,
  onClearSearchAndFilters,
}: GuestsBodyProps) {
  const canGoPrevious = viewModel.currentPage > 1
  const canGoNext =
    viewModel.currentPage * viewModel.pageSize < viewModel.totalFilteredCount

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>Guests</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Manage guest details, marketing permissions, feedback and engagement
            history.
          </p>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            disabled
            aria-disabled
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            Create campaign
          </Button>
          <Button
            type="button"
            disabled
            aria-disabled
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          >
            Export CSV
          </Button>
        </div>
      </div>

      <GuestsOverview kpis={viewModel.overviewKpis} />

      <GuestsSmartGroupsSection
        tabs={viewModel.smartGroupTabs}
        activeTabId={viewModel.activeSmartGroupId}
        onTabChange={onSmartGroupChange}
        rows={viewModel.tableRows}
        searchQuery={searchQuery}
        onSearchQueryChange={onSearchQueryChange}
        sortId={sortId}
        sortLabel={viewModel.sortLabel}
        onSortChange={onSortChange}
        pageRangeLabel={viewModel.pageRangeLabel}
        canGoPrevious={canGoPrevious}
        canGoNext={canGoNext}
        onPreviousPage={onPreviousPage}
        onNextPage={onNextPage}
        bulkSelectionLabel={bulkSelectionLabel}
        isAllVisibleSelected={isAllVisibleSelected}
        isSomeVisibleSelected={isSomeVisibleSelected}
        isGuestSelected={isGuestSelected}
        onToggleGuestSelection={onToggleGuestSelection}
        onToggleSelectAllVisibleRows={onToggleSelectAllVisibleRows}
        onClearSelection={onClearSelection}
        tableEmptyState={viewModel.tableEmptyState}
        onClearSearchAndFilters={onClearSearchAndFilters}
      />
    </div>
  )
}
