import { CalendarIcon, ChevronDownIcon } from "lucide-react"
import { Fragment, useEffect } from "react"

import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { OperatorRemovableChip } from "@/components/dashboard/operator/FilterSheet/OperatorRemovableChip"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { useGuestActivityTabModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestActivityTabModule"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
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
  GUESTS_DETAIL_DIVIDER_CLASS,
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

function ActivityTimelineRow({ row }: { row: OperatorGuestProfileActivityRow }) {
  return (
    <article className="flex flex-col gap-3">
      <p className="text-sm font-semibold leading-[19px] text-foreground">
        {row.headline}
      </p>
      <div className="flex flex-col gap-2">
        <p className="text-sm font-medium leading-[19px] text-foreground">
          {row.body}
        </p>
        <p className="text-sm font-medium leading-[19px] text-muted-foreground dark:text-[#7c7c7c]">
          {row.metaDisplay}
        </p>
      </div>
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
  const filterChips = snapshot.filterChips
  const hasFilters = filterChips.length > 0
  const filtersLabel = hasFilters
    ? `Filters (${snapshot.filterChipCount})`
    : "Filters"

  return (
    <>
      <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        </div>

        <div className="flex flex-col gap-[22px]">
          <div className="flex flex-wrap items-center gap-3">
            <Button
              type="button"
              variant="operator-secondary"
              aria-label={
                hasFilters
                  ? `Filters, ${snapshot.filterChipCount} applied`
                  : "Filters"
              }
              className="rounded-[2px]"
              onClick={() => {
                openFilters()
              }}
            >
              {filtersLabel}
            </Button>

            {hasFilters ? (
              <div className="flex min-w-0 flex-1 flex-wrap items-center gap-3">
                <div
                  className="flex flex-wrap items-center gap-3"
                  aria-label="Applied filters"
                >
                  {filterChips.map((chip: FilterChip) => (
                    <OperatorRemovableChip
                      key={chip.id}
                      label={chip.label}
                      removeLabel={`Remove ${chip.label}`}
                      onRemove={() => {
                        removeFilterChip(chip)
                      }}
                    />
                  ))}
                </div>
                <Button
                  type="button"
                  variant="operator-tertiary"
                  className="rounded-[2px]"
                  onClick={() => {
                    clearFilters()
                  }}
                >
                  {GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY.clearLabel}
                </Button>
              </div>
            ) : (
              <div className="min-w-0 flex-1" />
            )}

            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="operator-tertiary"
                  aria-label={`Sort: ${sortLabel}`}
                  className={GUESTS_SORT_BUTTON_CLASS}
                >
                  <CalendarIcon className="size-3.5 shrink-0" aria-hidden />
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

          <Separator className={GUESTS_DETAIL_DIVIDER_CLASS} />

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
            <div className="flex flex-col gap-[22px]">
              {viewModel.timelineRows.map((row, index) => (
                <Fragment key={row.id}>
                  {index > 0 ? (
                    <Separator className={GUESTS_DETAIL_DIVIDER_CLASS} />
                  ) : null}
                  <ActivityTimelineRow row={row} />
                </Fragment>
              ))}
            </div>
          )}
        </div>

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
