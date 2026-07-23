import { ChevronDownIcon, SearchIcon, SlidersHorizontal } from "lucide-react"
import { useEffect } from "react"

import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { GuestProfileEmptyCopy } from "@/components/dashboard/operator/GuestProfile/GuestProfileEmptyCopy"
import { GuestsRemovableChip } from "@/components/dashboard/operator/Guests/GuestsRemovableChip"
import { useGuestFeedbacksTabModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestFeedbacksTabModule"
import { GuestProfileIssueTagsCell } from "@/components/dashboard/operator/GuestProfile/GuestProfileIssueTagsCell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
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
import type { FilterChip } from "@/lib/operatorFilterSheet"
import { guestFeedbacksFilterSheetSchema } from "@/lib/operatorGuestProfile/guestFeedbacksFilterSheetSchema"
import {
  GUEST_PROFILE_EMPTY_COPY,
  GUEST_PROFILE_FEEDBACKS_FILTERED_EMPTY,
  GUEST_PROFILE_OPEN_FEEDBACK_LABEL,
  OPERATOR_GUEST_FEEDBACKS_SORT_LABELS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS,
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
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
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorGuestFeedbacksSortId } from "@/types/operatorGuestProfile"
import { cn } from "@/lib/utils"

type GuestProfileFeedbacksPanelProps = {
  guestId: number
  locationId: number
  active: boolean
  onOpenFeedback: (feedbackId: number) => void
}

const SORT_OPTIONS = Object.entries(
  OPERATOR_GUEST_FEEDBACKS_SORT_LABELS
) as Array<[OperatorGuestFeedbacksSortId, string]>

function ClassificationCell({
  sentiment,
}: {
  sentiment: "positive" | "neutral" | "negative" | null
}) {
  if (sentiment == null) {
    return <span className={GUESTS_TABLE_LOCATION_CLASS}>—</span>
  }

  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
}

const FEEDBACKS_FILTER_SHEET_SCHEMA = guestFeedbacksFilterSheetSchema()

function FeedbacksFilterChips({
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

export function GuestProfileFeedbacksPanel({
  guestId,
  locationId,
  active,
  onOpenFeedback,
}: GuestProfileFeedbacksPanelProps) {
  const {
    snapshot,
    syncWorkspace,
    retryLoad,
    setSearchQuery,
    setSortId,
    goToPreviousPage,
    goToNextPage,
    openFilters,
    closeFilters,
    setFiltersSession,
    applyFilters,
    removeFilterChip,
    clearSearchAndFilters,
  } = useGuestFeedbacksTabModule()

  useEffect(() => {
    void syncWorkspace({
      guestId,
      selectedLocationId: locationId,
      active,
    })
  }, [guestId, locationId, active, syncWorkspace])

  const copy = GUEST_PROFILE_EMPTY_COPY.feedbacksTab
  const viewModel = snapshot.viewModel
  const toolbarEnabled = viewModel?.toolbarEnabled ?? false
  const sortLabel =
    viewModel?.sortLabel ??
    OPERATOR_GUEST_FEEDBACKS_SORT_LABELS["recent-activity"]

  if (!active) {
    return null
  }

  if (snapshot.loadStatus === "loading" && viewModel == null) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner aria-label="Loading feedback history" />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && viewModel == null) {
    return (
      <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load feedback history. Please try again.
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
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <SearchIcon
              className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-[#707070]"
              aria-hidden
            />
            <Input
              value={snapshot.searchQuery}
              onChange={(event) => {
                setSearchQuery(event.target.value)
              }}
              disabled={!toolbarEnabled}
              aria-disabled={!toolbarEnabled}
              aria-label={
                toolbarEnabled
                  ? copy.searchPlaceholder
                  : `${copy.searchPlaceholder} (unavailable)`
              }
              title={
                toolbarEnabled
                  ? undefined
                  : `${copy.searchPlaceholder} is unavailable`
              }
              placeholder={copy.searchPlaceholder}
              className={GUESTS_SEARCH_FIELD_CLASS}
            />
          </div>

          <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
            <Button
              type="button"
              variant="operator-secondary"
              disabled={!toolbarEnabled}
              aria-disabled={!toolbarEnabled}
              aria-label={
                !toolbarEnabled
                  ? "Filters (unavailable)"
                  : snapshot.filterChipCount > 0
                    ? `Filters, ${snapshot.filterChipCount} applied`
                    : "Filters"
              }
              title={toolbarEnabled ? undefined : "Filters is unavailable"}
              className="relative rounded-[2px]"
              onClick={() => {
                openFilters()
              }}
            >
              <SlidersHorizontal className="size-4" aria-hidden />
              Filters
              {toolbarEnabled && snapshot.filterChipCount > 0 ? (
                <Badge
                  variant="default"
                  className="absolute -top-1.5 -right-1.5 min-w-5 rounded-full px-1 py-0 text-[10px] leading-5"
                >
                  {snapshot.filterChipCount}
                </Badge>
              ) : null}
            </Button>

            {toolbarEnabled ? (
              <DropdownMenu modal={false}>
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
            ) : (
              <Button
                type="button"
                variant="operator-tertiary"
                disabled
                aria-disabled
                aria-label={`Sort: ${sortLabel} (unavailable)`}
                title={`Sort: ${sortLabel} is unavailable`}
                className={GUESTS_SORT_BUTTON_CLASS}
              >
                Sort: {sortLabel}
              </Button>
            )}
          </div>
        </div>

        {toolbarEnabled ? (
          <FeedbacksFilterChips
            chips={snapshot.filterChips}
            onRemoveChip={removeFilterChip}
          />
        ) : null}

        {viewModel.tableEmptyState === "virgin-empty" ? (
          <GuestProfileEmptyCopy
            title={copy.emptyTitle}
            helper={copy.emptyHelper}
          />
        ) : viewModel.tableEmptyState === "filtered-empty" ? (
          <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
            <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
              <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>
                {GUEST_PROFILE_FEEDBACKS_FILTERED_EMPTY.emptyTitle}
              </p>
              <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>
                {GUEST_PROFILE_FEEDBACKS_FILTERED_EMPTY.emptyHelper}
              </p>
            </div>
            <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
              <Button
                type="button"
                variant="operator-tertiary"
                onClick={() => {
                  clearSearchAndFilters()
                }}
                className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
              >
                {GUEST_PROFILE_FEEDBACKS_FILTERED_EMPTY.clearLabel}
              </Button>
            </div>
          </div>
        ) : (
          <div className={GUESTS_TABLE_FRAME_CLASS}>
            <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Date
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Feedback
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Classification
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Issue tags
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Recovery
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Location
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Action
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {viewModel.tableRows.map((row) => (
                  <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_LOCATION_CLASS}>
                        {row.dateDisplay}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                        “{row.feedbackDisplay}”
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <ClassificationCell
                        sentiment={row.classificationDisplay}
                      />
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <GuestProfileIssueTagsCell labels={row.issueTagLabels} />
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_LOCATION_CLASS}>
                        {row.recoveryDisplay}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_LOCATION_CLASS}>
                        {row.locationDisplay}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Button
                        type="button"
                        variant="link"
                        size="link-sm"
                        className={GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS}
                        onClick={() => {
                          onOpenFeedback(row.id)
                        }}
                      >
                        {GUEST_PROFILE_OPEN_FEEDBACK_LABEL}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
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
        title="Filter feedback"
        schema={FEEDBACKS_FILTER_SHEET_SCHEMA}
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
