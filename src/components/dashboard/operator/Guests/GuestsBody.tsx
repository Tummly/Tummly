import { Button } from "@/components/ui/button"
import { GuestsOverview } from "@/components/dashboard/operator/Guests/GuestsOverview"
import { GuestsSmartGroupsSection } from "@/components/dashboard/operator/Guests/GuestsSmartGroupsSection"
import type { OperatorTabContentStatus } from "@/components/dashboard/operator/OperatorTableTabPanel"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import type { GuestsOverviewDateRange } from "@/lib/operatorGuests/guestsOverviewDateRange"
import type {
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

type GuestsBodyProps = {
  tabContentStatus: OperatorTabContentStatus
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
  onAddTag?: () => void
  onManageGuestTags: (guestId: string) => void
  onViewGuest: (guestId: string) => void
  onManageMarketingPermissions: (guestId: string) => void
  onExportCsv: () => void
  onExportSelected?: () => void
  exportBusy?: boolean
  filterChips: readonly FilterChip[]
  filterChipCount: number
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  overviewDateRangeLabel: string
  overviewDateRange: GuestsOverviewDateRange
  onCommitOverviewDateRange: (range: GuestsOverviewDateRange) => void
}

/** Guests page body — overview KPIs and Smart Groups table from live API. */
export function GuestsBody({
  tabContentStatus,
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
  onAddTag,
  onManageGuestTags,
  onViewGuest,
  onManageMarketingPermissions,
  onExportCsv,
  onExportSelected,
  exportBusy = false,
  filterChips,
  filterChipCount,
  onOpenFilters,
  onRemoveFilterChip,
  overviewDateRangeLabel,
  overviewDateRange,
  onCommitOverviewDateRange,
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
          <Button variant="op-primary"
            type="button"
            disabled
            aria-disabled
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            Create campaign
          </Button>
          <Button variant="op-secondary"
            type="button"
            disabled={exportBusy}
            aria-disabled={exportBusy}
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onExportCsv}
          >
            Export CSV
          </Button>
        </div>
      </div>

      <GuestsOverview
        kpis={viewModel.overviewKpis}
        dateRangeLabel={overviewDateRangeLabel}
        selectedDateRange={overviewDateRange}
        onCommitDateRange={onCommitOverviewDateRange}
      />

      <GuestsSmartGroupsSection
        tabContentStatus={tabContentStatus}
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
        onAddTag={onAddTag}
        onManageGuestTags={onManageGuestTags}
        onViewGuest={onViewGuest}
        onManageMarketingPermissions={onManageMarketingPermissions}
        onExportSelected={onExportSelected}
        exportBusy={exportBusy}
        filterChips={filterChips}
        filterChipCount={filterChipCount}
        onOpenFilters={onOpenFilters}
        onRemoveFilterChip={onRemoveFilterChip}
      />
    </div>
  )
}
