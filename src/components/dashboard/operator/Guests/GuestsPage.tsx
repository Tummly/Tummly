import { GuestsBody } from "@/components/dashboard/operator/Guests/GuestsBody"
import { useGuestsPageModule } from "@/components/dashboard/operator/Guests/utils/useGuestsPageModule"

export function GuestsPage() {
  const guests = useGuestsPageModule()
  const { snapshot } = guests

  return (
    <GuestsBody
      viewModel={snapshot.viewModel}
      searchQuery={snapshot.searchQuery}
      sortId={snapshot.sortId}
      bulkSelectionLabel={snapshot.bulkSelectionLabel}
      isAllVisibleSelected={snapshot.isAllVisibleSelected}
      isSomeVisibleSelected={snapshot.isSomeVisibleSelected}
      isGuestSelected={snapshot.isGuestSelected}
      onSmartGroupChange={guests.setActiveSmartGroupId}
      onSearchQueryChange={guests.setSearchQuery}
      onSortChange={guests.setSortId}
      onPreviousPage={guests.goToPreviousPage}
      onNextPage={guests.goToNextPage}
      onToggleGuestSelection={guests.toggleGuestSelection}
      onToggleSelectAllVisibleRows={guests.toggleSelectAllVisibleRows}
      onClearSelection={guests.clearSelection}
      onClearSearchAndFilters={guests.clearSearchAndFilters}
    />
  )
}
