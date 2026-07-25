import {
  ChevronDownIcon,
  SearchIcon,
  SlidersHorizontal,
} from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { GuestsBulkBar } from "@/components/dashboard/operator/Guests/GuestsBulkBar"
import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { GuestsRowActionsMenu } from "@/components/dashboard/operator/Guests/GuestsRowActionsMenu"
import { GuestsTableEmptyState } from "@/components/dashboard/operator/Guests/GuestsTableEmptyState"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
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
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TAB_COUNT_ACTIVE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CHECKBOX_CELL_CLASS,
  GUESTS_TABLE_CHECKBOX_CELL_INNER_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FEEDBACK_COUNT_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CHECKBOX_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_ICON_CELL_INNER_CLASS,
  GUESTS_TABLE_INTERACTION_LABEL_CLASS,
  GUESTS_TABLE_INTERACTION_TIME_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
  OPERATOR_GUEST_CONTACT_LINK_CLASS,
  OPERATOR_GUEST_SORT_LABELS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import type {
  GuestFeedbackSentiment,
  OperatorGuestSmartGroupId,
  OperatorGuestSmartGroupTab,
  OperatorGuestSortId,
  OperatorGuestTableRow,
  OperatorGuestsTableEmptyStateKind,
} from "@/types/operatorGuests"
import type { FilterChip } from "@/lib/operatorFilterSheet"

const SORT_OPTIONS = Object.entries(OPERATOR_GUEST_SORT_LABELS) as Array<
  [OperatorGuestSortId, string]
>

type GuestsSmartGroupsSectionProps = {
  tabs: OperatorGuestSmartGroupTab[]
  activeTabId: OperatorGuestSmartGroupId
  onTabChange: (id: OperatorGuestSmartGroupId) => void
  rows: OperatorGuestTableRow[]
  searchQuery: string
  onSearchQueryChange: (query: string) => void
  sortId: OperatorGuestSortId
  sortLabel: string
  onSortChange: (id: OperatorGuestSortId) => void
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
  onPreviousPage: () => void
  onNextPage: () => void
  bulkSelectionLabel: string | null
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
  isGuestSelected: (guestId: string) => boolean
  onToggleGuestSelection: (guestId: string) => void
  onToggleSelectAllVisibleRows: () => void
  onClearSelection: () => void
  tableEmptyState: OperatorGuestsTableEmptyStateKind | null
  onClearSearchAndFilters: () => void
  onAddTag?: () => void
  onManageGuestTags: (guestId: string) => void
  onViewGuest: (guestId: string) => void
  onExportSelected?: () => void
  exportBusy?: boolean
  filterChips: readonly FilterChip[]
  filterChipCount: number
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  nowMs?: number
}

function feedbackSentimentLabel(
  sentiment: GuestFeedbackSentiment
): string | null {
  switch (sentiment) {
    case "positive":
      return "Positive"
    case "neutral":
      return "Neutral"
    case "negative":
      return "Negative"
    default:
      return null
  }
}

function feedbackSentimentVariant(
  sentiment: GuestFeedbackSentiment
): "positive" | "neutral" | "negative" | null {
  switch (sentiment) {
    case "positive":
      return "positive"
    case "neutral":
      return "neutral"
    case "negative":
      return "negative"
    default:
      return null
  }
}

function formatSubmissionCount(count: number): string {
  return count === 1 ? "1 submission" : `${count} submissions`
}

/** Figma Smart groups — tabs, deferred toolbar chrome, and guest table. */
export function GuestsSmartGroupsSection({
  tabs,
  activeTabId,
  onTabChange,
  rows,
  searchQuery,
  onSearchQueryChange,
  sortId,
  sortLabel,
  onSortChange,
  pageRangeLabel,
  canGoPrevious,
  canGoNext,
  onPreviousPage,
  onNextPage,
  bulkSelectionLabel,
  isAllVisibleSelected,
  isSomeVisibleSelected,
  isGuestSelected,
  onToggleGuestSelection,
  onToggleSelectAllVisibleRows,
  onClearSelection,
  tableEmptyState,
  onClearSearchAndFilters,
  onAddTag,
  onManageGuestTags,
  onViewGuest,
  onExportSelected,
  exportBusy = false,
  filterChips,
  filterChipCount,
  onOpenFilters,
  onRemoveFilterChip,
  nowMs = Date.now(),
}: GuestsSmartGroupsSectionProps) {
  const headerCheckboxChecked = isAllVisibleSelected
    ? true
    : isSomeVisibleSelected
      ? "indeterminate"
      : false

  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SMART_GROUPS_STACK_CLASS}>
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Smart groups</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Quickly view guests based on permission, feedback and engagement
            activity.
          </p>
        </header>

        <div className={GUESTS_TABLIST_SCROLL_CLASS}>
          <div
            role="tablist"
            aria-label="Smart group filters"
            className={GUESTS_TABLIST_CLASS}
          >
            {tabs.map((tab) => {
              const selected = tab.id === activeTabId
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
                  onClick={() => onTabChange(tab.id)}
                >
                  <span>{tab.label}</span>
                  <span
                    className={cn(selected ? GUESTS_TAB_COUNT_ACTIVE_CLASS : "")}
                  >
                    {tab.count}
                  </span>
                </Button>
              )
            })}
          </div>
        </div>

        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <SearchIcon
              className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-[#707070]"
              aria-hidden
            />
            <Input
              value={searchQuery}
              onChange={(event) => onSearchQueryChange(event.target.value)}
              aria-label="Search guests"
              placeholder="Search guests by name, email or mobile number"
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
              className="relative rounded-[2px]"
              onClick={onOpenFilters}
            >
              <SlidersHorizontal className="size-4" aria-hidden />
              Filters
              {filterChipCount > 0 ? (
                <Badge
                  variant="default"
                  className="absolute -top-1.5 -right-1.5 min-w-5 rounded-full px-1 py-0 text-[10px] leading-5"
                >
                  {filterChipCount}
                </Badge>
              ) : null}
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
                  <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className={GUESTS_SORT_MENU_CLASS}>
                {SORT_OPTIONS.map(([id, label]) => (
                  <DropdownMenuItem
                    key={id}
                    className={cn(
                      GUESTS_TABLE_MENU_ITEM_CLASS,
                      id === sortId && GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS
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

        {bulkSelectionLabel ? (
          <GuestsBulkBar
            selectionLabel={bulkSelectionLabel}
            onClearSelection={onClearSelection}
            onAddTag={onAddTag}
            onExportSelected={onExportSelected}
            exportBusy={exportBusy}
          />
        ) : null}

        <div role="tabpanel">
          {tableEmptyState ? (
            <GuestsTableEmptyState
              kind={tableEmptyState}
              onClearSearchAndFilters={
                tableEmptyState === "no-guests-found"
                  ? onClearSearchAndFilters
                  : undefined
              }
            />
          ) : (
            <div className={GUESTS_TABLE_FRAME_CLASS}>
              <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={GUESTS_TABLE_HEAD_CHECKBOX_CELL_CLASS}>
                    <div className={GUESTS_TABLE_CHECKBOX_CELL_INNER_CLASS}>
                      <Checkbox
                        checked={headerCheckboxChecked}
                        onCheckedChange={onToggleSelectAllVisibleRows}
                        aria-label="Select all visible guests"
                      />
                    </div>
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Guest
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Contact
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Marketing status
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Location
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Latest feedback
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Last interaction
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                    <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                      Actions
                    </div>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row) => {
                  const sentimentLabel = feedbackSentimentLabel(
                    row.latestFeedbackSentiment
                  )
                  const sentimentVariant = feedbackSentimentVariant(
                    row.latestFeedbackSentiment
                  )

                  return (
                    <TableRow
                      key={row.id}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={GUESTS_TABLE_CHECKBOX_CELL_CLASS}>
                        <div className={GUESTS_TABLE_CHECKBOX_CELL_INNER_CLASS}>
                          <Checkbox
                            checked={isGuestSelected(row.id)}
                            onCheckedChange={() =>
                              onToggleGuestSelection(row.id)
                            }
                            aria-label={`Select ${row.name}`}
                          />
                        </div>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Button
                          type="button"
                          variant="link"
                          className={`${GUESTS_TABLE_GUEST_NAME_CLASS} h-auto min-h-0 p-0 font-semibold`}
                          onClick={() => {
                            onViewGuest(row.id)
                          }}
                        >
                          {row.name}
                        </Button>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <a
                          href={`mailto:${row.email}`}
                          className={OPERATOR_GUEST_CONTACT_LINK_CLASS}
                        >
                          {row.email}
                        </a>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge
                          variant="soft"
                          className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                        >
                          {row.marketingStatusLabel}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.locationName}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {sentimentLabel && sentimentVariant ? (
                          <div className="flex items-center gap-2.5">
                            <Badge variant={sentimentVariant}>
                              {sentimentLabel}
                            </Badge>
                            <span className={GUESTS_TABLE_FEEDBACK_COUNT_CLASS}>
                              {formatSubmissionCount(
                                row.feedbackSubmissionCount
                              )}
                            </span>
                          </div>
                        ) : (
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>—</span>
                        )}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.lastInteractionAt ? (
                          <p className="m-0 whitespace-pre-wrap">
                            <span className={GUESTS_TABLE_INTERACTION_LABEL_CLASS}>
                              {row.lastInteractionLabel}
                            </span>
                            {"\n"}
                            <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                              {formatRelativeTime(
                                row.lastInteractionAt,
                                nowMs
                              )}
                            </span>
                          </p>
                        ) : (
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>—</span>
                        )}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                        <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                          <GuestsRowActionsMenu
                            guestId={row.id}
                            guestName={row.name}
                            onManageTags={onManageGuestTags}
                            onViewGuest={onViewGuest}
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
          )}
        </div>

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
      </div>
    </section>
  )
}
