import { GuestsBody } from "@/components/dashboard/operator/Guests/GuestsBody"
import { useGuestsPageModule } from "@/components/dashboard/operator/Guests/utils/useGuestsPageModule"
import { Button } from "@/components/ui/button"

export function GuestsPage() {
  const guests = useGuestsPageModule()
  const { snapshot } = guests

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex min-h-48 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading guests"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
          aria-hidden
        />
      </div>
    )
  }

  if (snapshot.viewModel == null && snapshot.loadStatus === "error") {
    return (
      <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load guests. Please try again.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            void guests.retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return (
    <>
      {snapshot.loadStatus === "error" ? (
        <div className="mb-3 flex flex-wrap items-center gap-3">
          <p className="text-sm text-destructive" role="alert">
            Could not refresh guests. Please try again.
          </p>
          <Button
            type="button"
            variant="link"
            size="link-sm"
            className="font-medium underline"
            onClick={() => {
              void guests.retryLoad()
            }}
          >
            Retry
          </Button>
        </div>
      ) : null}
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
    </>
  )
}
