import { ChevronDownIcon, SlidersHorizontal } from "lucide-react"
import { useEffect } from "react"

import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { GuestsRemovableChip } from "@/components/dashboard/operator/Guests/GuestsRemovableChip"
import { useGuestActivityTabModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestActivityTabModule"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import { guestActivityFilterSheetSchema } from "@/lib/operatorGuestProfile/guestActivityFilterSheetSchema"
import {
  GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY,
  GUEST_PROFILE_EMPTY_COPY,
  OPERATOR_GUEST_ACTIVITY_SORT_LABELS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  OperatorGuestActivitySortId,
  OperatorGuestProfileActivityRow,
} from "@/types/operatorGuestProfile"
import { cn } from "@/lib/utils"

type GuestProfileActivityPanelProps = {
  guestId: number
  locationId: number
  active: boolean
}

const SORT_OPTIONS = Object.entries(
  OPERATOR_GUEST_ACTIVITY_SORT_LABELS
) as Array<[OperatorGuestActivitySortId, string]>

const ACTIVITY_FILTER_SHEET_SCHEMA = guestActivityFilterSheetSchema()

function ActivityFilterChips({
  chips,
  onRemoveChip,
}: {
  chips: readonly FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
}) {
  if (chips.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap gap-2" aria-label="Applied filters">
      {chips.map((chip) => (
        <GuestsRemovableChip
          key={chip.id}
          label={chip.label}
          removeLabel={`Remove ${chip.label}`}
          onRemove={() => onRemoveChip(chip)}
        />
      ))}
    </div>
  )
}

function ActivityTimelineRow({ row }: { row: OperatorGuestProfileActivityRow }) {
  return (
    <article className="flex flex-col gap-2 border-b border-[#e5e5e5] py-5 last:border-b-0 dark:border-[#262626]">
      <p className="text-sm font-semibold tracking-[-0.2px] text-foreground">
        {row.headline}
      </p>
      <p className={`text-sm font-medium ${GUESTS_TABLE_LOCATION_CLASS}`}>
        {row.body}
      </p>
      <p className={`text-xs font-medium ${GUESTS_TABLE_LOCATION_CLASS}`}>
        {row.metaDisplay}
      </p>
    </article>
  )
}

export function GuestProfileActivityPanel({
  guestId,
  locationId,
  active,
}: GuestProfileActivityPanelProps) {
  const {
    snapshot,
    syncWorkspace,
    retryLoad,
    setSortId,
    goToPreviousPage,
    goToNextPage,
    openFilters,
    closeFilters,
    setFiltersSession,
    applyFilters,
    removeFilterChip,
    clearFilters,
  } = useGuestActivityTabModule()

  useEffect(() => {
    void syncWorkspace({
      guestId,
      selectedLocationId: locationId,
      active,
    })
  }, [guestId, locationId, active, syncWorkspace])

  const copy = GUEST_PROFILE_EMPTY_COPY.activityTab
  const viewModel = snapshot.viewModel
  const sortLabel =
    viewModel?.sortLabel ??
    OPERATOR_GUEST_ACTIVITY_SORT_LABELS["recent-activity"]

  if (!active) {
    return null
  }

  if (snapshot.loadStatus === "loading" && viewModel == null) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner aria-label="Loading activity" />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && viewModel == null) {
    return (
      <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load activity. Please try again.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            void retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (viewModel == null) {
    return null
  }

  // Virgin empty: section card only — no Filters/Sort chrome (contract).
  if (viewModel.timelineEmptyState === "virgin-empty") {
    return (
      <GuestProfileSectionEmptyCard
        sectionTitle={copy.sectionTitle}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
      />
    )
  }

  const canGoPrevious = viewModel.currentPage > 1
  const maxPage = Math.max(
    1,
    Math.ceil(viewModel.totalCount / viewModel.pageSize)
  )
  const canGoNext = viewModel.currentPage < maxPage
  const showPagination = viewModel.totalCount > 0

  return (
    <>
      <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        </div>

        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={`${GUESTS_TOOLBAR_ACTIONS_CLASS} sm:ml-auto`}>
            <Button
              type="button"
              variant="operator-secondary"
              aria-label={
                snapshot.filterChipCount > 0
                  ? `Filters, ${snapshot.filterChipCount} applied`
                  : "Filters"
              }
              className="relative rounded-[2px]"
              onClick={() => {
                openFilters()
              }}
            >
              <SlidersHorizontal className="size-4" aria-hidden />
              Filters
              {snapshot.filterChipCount > 0 ? (
                <Badge
                  variant="default"
                  className="absolute -top-1.5 -right-1.5 min-w-5 rounded-full px-1 py-0 text-[10px] leading-5"
                >
                  {snapshot.filterChipCount}
                </Badge>
              ) : null}
            </Button>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="operator-tertiary"
                  aria-label={`Sort: ${sortLabel}`}
                  className={GUESTS_SORT_BUTTON_CLASS}
                >
                  Sort: {sortLabel}
                  <ChevronDownIcon
                    className="size-3.5 shrink-0"
                    aria-hidden
                  />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="min-w-[220px]">
                {SORT_OPTIONS.map(([id, label]) => (
                  <DropdownMenuItem
                    key={id}
                    className={cn(
                      "text-sm font-medium",
                      id === snapshot.sortId && "text-primary"
                    )}
                    onClick={() => {
                      setSortId(id)
                    }}
                  >
                    {label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        <ActivityFilterChips
          chips={snapshot.filterChips}
          onRemoveChip={removeFilterChip}
        />

        {viewModel.timelineEmptyState === "filtered-empty" ? (
          <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
            <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
              <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>
                {GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY.emptyTitle}
              </p>
              <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>
                {GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY.emptyHelper}
              </p>
            </div>
            <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
              <Button
                type="button"
                variant="operator-tertiary"
                onClick={() => {
                  clearFilters()
                }}
                className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
              >
                {GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY.clearLabel}
              </Button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col">
            {viewModel.timelineRows.map((row) => (
              <ActivityTimelineRow key={row.id} row={row} />
            ))}
          </div>
        )}

        {showPagination ? (
          <div className={GUESTS_PAGINATION_ROW_CLASS}>
            <p className={GUESTS_PAGINATION_LABEL_CLASS}>
              {viewModel.pageRangeLabel}
            </p>
            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant="operator-tertiary"
                disabled={!canGoPrevious}
                aria-disabled={!canGoPrevious}
                aria-label="Previous page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={() => {
                  goToPreviousPage()
                }}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="operator-tertiary"
                disabled={!canGoNext}
                aria-disabled={!canGoNext}
                aria-label="Next page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={() => {
                  goToNextPage()
                }}
              >
                Next
              </Button>
            </div>
          </div>
        ) : null}
      </section>

      <OperatorFilterSheetDialog
        open={snapshot.filtersSession != null}
        title="Filter activity"
        schema={ACTIVITY_FILTER_SHEET_SCHEMA}
        session={snapshot.filtersSession}
        onSessionChange={setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            closeFilters()
          }
        }}
        onApply={applyFilters}
      />
    </>
  )
}
