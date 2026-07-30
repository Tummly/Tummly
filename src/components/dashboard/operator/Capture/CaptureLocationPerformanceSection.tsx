import { ChevronDownIcon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { CaptureLocationRowActionsMenu } from "@/components/dashboard/operator/Capture/CaptureLocationRowActionsMenu"
import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_MULTI_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import {
  OPERATOR_CAPTURE_LOCATION_SORT_LABELS,
} from "@/lib/operatorMultiCapture/buildCaptureLocationPerformance"
import type {
  OperatorCaptureLocationRowAction,
  OperatorMultiCaptureLocationPerformanceView,
} from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import {
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_SORT_MENU_CLASS,
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_ICON_CELL_INNER_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import type { CaptureLocationsSortId } from "@/types/dashboard"

const SORT_OPTIONS = Object.entries(
  OPERATOR_CAPTURE_LOCATION_SORT_LABELS
) as Array<[CaptureLocationsSortId, string]>

type CaptureLocationPerformanceSectionProps = {
  locationPerformance: OperatorMultiCaptureLocationPerformanceView
  searchQuery: string
  sortId: CaptureLocationsSortId
  filterChips: readonly FilterChip[]
  filterChipCount: number
  locationRowActions: readonly OperatorCaptureLocationRowAction[]
  onSearchQueryChange: (query: string) => void
  onSortChange: (id: CaptureLocationsSortId) => void
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onClearSearchAndFilters: () => void
  onPreviousPage: () => void
  onNextPage: () => void
  onNavigateToLocationCapture: (locationId: number) => void
}

/** Multi Capture Location performance — toolbar, table, empty/error chrome. */
export function CaptureLocationPerformanceSection({
  locationPerformance,
  searchQuery,
  sortId,
  filterChips,
  filterChipCount,
  locationRowActions,
  onSearchQueryChange,
  onSortChange,
  onOpenFilters,
  onRemoveFilterChip,
  onClearSearchAndFilters,
  onPreviousPage,
  onNextPage,
  onNavigateToLocationCapture,
}: CaptureLocationPerformanceSectionProps) {
  const copy = OPERATOR_CAPTURE_MULTI_SECTION_COPY.locationPerformance
  const {
    rows,
    emptyKind,
    showToolbar,
    showPagination,
    pageRangeLabel,
    sortLabel,
    currentPage,
    pageSize,
    totalCount,
  } = locationPerformance

  const canGoPrevious = currentPage > 1
  const canGoNext = currentPage * pageSize < totalCount

  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{copy.title}</h2>
        <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{copy.description}</p>
      </header>

      {emptyKind === "no-locations" || emptyKind === "load-error" ? (
        <div className={CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS}>
          {emptyKind === "no-locations" ? (
            <div className="flex flex-col items-center gap-2.5 text-center">
              <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
              <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[450px]`}>
                {copy.emptyHelper}
              </p>
            </div>
          ) : null}
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          {showToolbar ? (
            <>
              <div className={GUESTS_TOOLBAR_ROW_CLASS}>
                <div className={GUESTS_SEARCH_WRAP_CLASS}>
                  <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
                  <Input
                    value={searchQuery}
                    onChange={(event) =>
                      onSearchQueryChange(event.target.value)
                    }
                    aria-label="Search locations"
                    placeholder="Search locations"
                    className={GUESTS_SEARCH_FIELD_CLASS}
                  />
                </div>

                <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
                  <Button
                    type="button"
                    variant="op-secondary"
                    aria-label={
                      filterChipCount > 0
                        ? `Filters, ${filterChipCount} applied`
                        : "Filters"
                    }
                    className="rounded-[2px]"
                    onClick={onOpenFilters}
                  >
                    Filters
                    {filterChipCount > 0 ? ` (${filterChipCount})` : null}
                  </Button>
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
                    <DropdownMenuContent
                      align="end"
                      className={GUESTS_SORT_MENU_CLASS}
                    >
                      {SORT_OPTIONS.map(([id, label]) => (
                        <DropdownMenuItem
                          key={id}
                          className={cn(
                            GUESTS_TABLE_MENU_ITEM_CLASS,
                            id === sortId &&
                              GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS
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
                chips={filterChips}
                onRemoveChip={onRemoveFilterChip}
              />
            </>
          ) : null}

          {emptyKind === "no-results" ? (
            <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
              <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
                <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>
                  {copy.noResultsTitle}
                </p>
                <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>
                  {copy.noResultsHelper}
                </p>
              </div>
              <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
                <Button
                  type="button"
                  variant="op-tertiary"
                  onClick={onClearSearchAndFilters}
                  className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
                >
                  Clear search and filters
                </Button>
              </div>
            </div>
          ) : (
            <div className={GUESTS_TABLE_FRAME_CLASS}>
              <Table className={GUESTS_TABLE_CLASS}>
                <TableHeader className="[&_tr]:border-0">
                  <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Location
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Status
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Active placements
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Guest form opens
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Feedback submitted
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Submission rate
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Marketing opt-ins
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Offer claims
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                      Last activity
                    </TableHead>
                    <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                      Actions
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {rows.map((row) => (
                    <TableRow
                      key={row.locationId}
                      className={cn(
                        GUESTS_TABLE_BODY_ROW_CLASS,
                        "cursor-pointer"
                      )}
                      onClick={() => {
                        onNavigateToLocationCapture(row.locationId)
                      }}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Button
                          type="button"
                          variant="link"
                          className={`${GUESTS_TABLE_GUEST_NAME_CLASS} h-auto min-h-0 p-0 font-semibold`}
                          onClick={(event) => {
                            event.stopPropagation()
                            onNavigateToLocationCapture(row.locationId)
                          }}
                        >
                          {row.locationName}
                        </Button>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant="soft">{row.status}</Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.activePlacementsText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.qrScansText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.feedbackSubmittedText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.submissionRateText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.marketingOptInsText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.offerClaimsText}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.lastActivityText}
                        </span>
                      </TableCell>
                      <TableCell
                        className={GUESTS_TABLE_ACTIONS_CELL_CLASS}
                        onClick={(event) => {
                          event.stopPropagation()
                        }}
                      >
                        <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                          <CaptureLocationRowActionsMenu
                            locationName={row.locationName}
                            actions={locationRowActions}
                            onViewLocationCapture={() => {
                              onNavigateToLocationCapture(row.locationId)
                            }}
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}

          {showPagination ? (
            <div className={GUESTS_PAGINATION_ROW_CLASS}>
              <p className={GUESTS_PAGINATION_LABEL_CLASS}>{pageRangeLabel}</p>
              <div className="flex items-center gap-3">
                <Button
                  type="button"
                  variant="op-secondary"
                  disabled={!canGoPrevious}
                  aria-disabled={!canGoPrevious}
                  aria-label="Previous page"
                  className={GUESTS_PAGINATION_BUTTON_CLASS}
                  onClick={onPreviousPage}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  variant="op-secondary"
                  disabled={!canGoNext}
                  aria-disabled={!canGoNext}
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
      )}
    </section>
  )
}
