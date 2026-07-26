import { ChevronDownIcon, SlidersHorizontal } from "lucide-react"
import {
  Fragment,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type Ref,
} from "react"

import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { OperatorRemovableChip } from "@/components/dashboard/operator/FilterSheet/OperatorRemovableChip"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { useGuestActivityTabModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestActivityTabModule"
import { Badge } from "@/components/ui/badge"
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
  GUESTS_SORT_MENU_CLASS,
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
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

const FILTER_CHIP_ROW_GAP_PX = 12

function ActivityClearFiltersButton({
  buttonRef,
  onClick,
}: {
  buttonRef?: Ref<HTMLButtonElement>
  onClick: () => void
}) {
  return (
    <Button
      ref={buttonRef}
      type="button"
      variant="op-tertiary"
      className="rounded-[2px]"
      onClick={onClick}
    >
      {GUEST_PROFILE_ACTIVITY_FILTERED_EMPTY.clearLabel}
    </Button>
  )
}

function shouldPlaceClearUnderFilters(
  chipsRow: HTMLElement,
  clearWidthPx: number
): boolean {
  const chips = [
    ...chipsRow.querySelectorAll<HTMLElement>("[data-filter-chip]"),
  ]
  if (chips.length === 0) {
    return false
  }

  const chipsWidth = chips.reduce(
    (sum, chip, index) =>
      sum + chip.offsetWidth + (index > 0 ? FILTER_CHIP_ROW_GAP_PX : 0),
    0
  )
  const available = chipsRow.clientWidth
  if (chipsWidth > available) {
    return true
  }
  return chipsWidth + FILTER_CHIP_ROW_GAP_PX + clearWidthPx > available
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
  const filterChips = snapshot.filterChips
  const hasFilters = filterChips.length > 0
  const chipsRowRef = useRef<HTMLDivElement>(null)
  const clearButtonRef = useRef<HTMLButtonElement>(null)
  const [clearUnderFilters, setClearUnderFilters] = useState(false)

  useLayoutEffect(() => {
    if (!hasFilters) {
      setClearUnderFilters(false)
      return
    }

    const chipsRow = chipsRowRef.current
    if (chipsRow == null) {
      return
    }

    const update = () => {
      const clearWidth = clearButtonRef.current?.offsetWidth ?? 0
      setClearUnderFilters(shouldPlaceClearUnderFilters(chipsRow, clearWidth))
    }

    update()

    const observer = new ResizeObserver(update)
    observer.observe(chipsRow)
    return () => {
      observer.disconnect()
    }
  }, [hasFilters, filterChips])

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

        <div className="flex flex-col gap-[22px]">
          <div className="flex items-start gap-3">
            <div className="flex shrink-0 flex-col items-start gap-3">
              <Button
                type="button"
                variant="op-secondary"
                aria-label={
                  hasFilters
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
                {hasFilters ? (
                  <Badge
                    variant="default"
                    className="absolute -top-1.5 -right-1.5 min-w-5 rounded-full px-1 py-0 text-[10px] leading-5"
                  >
                    {snapshot.filterChipCount}
                  </Badge>
                ) : null}
              </Button>

              {hasFilters && clearUnderFilters ? (
                <ActivityClearFiltersButton
                  buttonRef={clearButtonRef}
                  onClick={() => {
                    clearFilters()
                  }}
                />
              ) : null}
            </div>

            {hasFilters ? (
              <div
                ref={chipsRowRef}
                className="flex min-w-0 flex-1 flex-wrap content-start gap-3"
                aria-label="Applied filters"
              >
                {filterChips.map((chip: FilterChip) => (
                  <span key={chip.id} data-filter-chip className="inline-flex">
                    <OperatorRemovableChip
                      label={chip.label}
                      removeLabel={`Remove ${chip.label}`}
                      onRemove={() => {
                        removeFilterChip(chip)
                      }}
                    />
                  </span>
                ))}
                {!clearUnderFilters ? (
                  <ActivityClearFiltersButton
                    buttonRef={clearButtonRef}
                    onClick={() => {
                      clearFilters()
                    }}
                  />
                ) : null}
              </div>
            ) : (
              <div className="min-w-0 flex-1" />
            )}

            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="op-tertiary"
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
              <DropdownMenuContent align="end" className={GUESTS_SORT_MENU_CLASS}>
                {SORT_OPTIONS.map(([id, label]) => (
                  <DropdownMenuItem
                    key={id}
                    className={cn(
                      GUESTS_TABLE_MENU_ITEM_CLASS,
                      id === snapshot.sortId &&
                        GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS
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
                  variant="op-tertiary"
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
                variant="op-tertiary"
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
                variant="op-tertiary"
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
